// Program.cs
using System.Text.Json;
using EFScaffold;
using EFScaffold.EntityFramework;
using Fleck;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services
builder.Services.AddDbContext<KahootContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Game state tracking
var gameConnections = new Dictionary<string, List<IWebSocketConnection>>();
var playerConnections = new Dictionary<IWebSocketConnection, string>();

var server = new WebSocketServer("ws://0.0.0.0:8181");
server.RestartAfterListenError = true;
server.ListenerSocket.NoDelay = true;
server.Start(socket =>
{
    socket.OnOpen = () =>
    {
        Console.WriteLine($"Connection opened: {socket.ConnectionInfo.Id}");
    };

    socket.OnClose = () =>
    {
        if (playerConnections.TryGetValue(socket, out var gameId))
        {
            if (gameConnections.ContainsKey(gameId))
            {
                gameConnections[gameId].Remove(socket);
                BroadcastToGame(gameId, new { type = "player-disconnected", playerId = socket.ConnectionInfo.Id });
            }
            playerConnections.Remove(socket);
        }
    };

    socket.OnMessage = async message =>
    {
        try
        {
            var msg = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            if (msg == null) return;

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KahootContext>();

            switch (msg["type"].ToString())
            {
                case "host-create-game":
                    await HandleCreateGame(socket, msg, dbContext);
                    break;

                case "player-join":
                    await HandlePlayerJoin(socket, msg, dbContext);
                    break;

                case "start-game":
                    await HandleStartGame(msg, dbContext);
                    break;

                case "submit-answer":
                    await HandleSubmitAnswer(socket, msg, dbContext);
                    break;

                case "next-question":
                    await HandleNextQuestion(msg, dbContext);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling message: {ex.Message}");
            socket.Send(JsonSerializer.Serialize(new { type = "error", message = "Internal server error" }));
        }
    };
});

async Task HandleCreateGame(IWebSocketConnection socket, Dictionary<string, object> msg, KahootContext dbContext)
{
    var game = new Game
    {
        Id = Guid.NewGuid().ToString()[..6].ToUpper(),
        Name = msg["name"].ToString()!
    };

    dbContext.Games.Add(game);
    await dbContext.SaveChangesAsync();

    gameConnections[game.Id] = new List<IWebSocketConnection> { socket };
    playerConnections[socket] = game.Id;

    socket.Send(JsonSerializer.Serialize(new
    {
        type = "game-created",
        gameId = game.Id,
        name = game.Name
    }));
}

async Task HandlePlayerJoin(IWebSocketConnection socket, Dictionary<string, object> msg, KahootContext dbContext)
{
    var gameId = msg["gameId"].ToString()!;
    var nickname = msg["nickname"].ToString()!;

    var game = await dbContext.Games.FindAsync(gameId);
    if (game == null)
    {
        socket.Send(JsonSerializer.Serialize(new { type = "error", message = "Game not found" }));
        return;
    }

    var player = new Player
    {
        Id = socket.ConnectionInfo.Id.ToString(),
        GameId = gameId,
        Nickname = nickname
    };

    dbContext.Players.Add(player);
    await dbContext.SaveChangesAsync();

    if (!gameConnections.ContainsKey(gameId))
    {
        gameConnections[gameId] = new List<IWebSocketConnection>();
    }
    gameConnections[gameId].Add(socket);
    playerConnections[socket] = gameId;

    BroadcastToGame(gameId, new
    {
        type = "player-joined",
        player = new { id = player.Id, nickname = player.Nickname }
    });
}

async Task HandleStartGame(Dictionary<string, object> msg, KahootContext dbContext)
{
    var gameId = msg["gameId"].ToString()!;
    var questions = await dbContext.Questions
        .Where(q => q.GameId == gameId)
        .Include(q => q.QuestionOptions)
        .OrderBy(q => q.Id)
        .ToListAsync();

    if (!questions.Any()) return;

    var currentQuestion = questions.First();
    BroadcastToGame(gameId, new
    {
        type = "game-started",
        question = new
        {
            text = currentQuestion.QuestionText,
            options = currentQuestion.QuestionOptions.Select(o => o.OptionText).ToList(),
            timeLimit = 20
        }
    });
}

async Task HandleSubmitAnswer(IWebSocketConnection socket, Dictionary<string, object> msg, KahootContext dbContext)
{
    var gameId = msg["gameId"].ToString()!;
    var questionId = msg["questionId"].ToString()!;
    var optionIndex = Convert.ToInt32(msg["optionIndex"]);

    var question = await dbContext.Questions
        .Include(q => q.QuestionOptions)
        .FirstOrDefaultAsync(q => q.Id == questionId);

    if (question == null) return;

    var selectedOption = question.QuestionOptions.ElementAtOrDefault(optionIndex);
    if (selectedOption == null) return;

    var playerAnswer = new PlayerAnswer
    {
        PlayerId = socket.ConnectionInfo.Id.ToString(),
        QuestionId = questionId,
        SelectedOptionId = selectedOption.Id,
        AnswerTimestamp = DateTime.UtcNow
    };

    dbContext.PlayerAnswers.Add(playerAnswer);
    await dbContext.SaveChangesAsync();

    var allPlayersAnswered = await dbContext.Players
        .Where(p => p.GameId == gameId)
        .AllAsync(p => p.PlayerAnswers.Any(pa => pa.QuestionId == questionId));

    if (allPlayersAnswered)
    {
        await SendQuestionResults(gameId, questionId, dbContext);
    }
}

async Task HandleNextQuestion(Dictionary<string, object> msg, KahootContext dbContext)
{
    var gameId = msg["gameId"].ToString()!;
    var nextQuestion = await dbContext.Questions
        .Include(q => q.QuestionOptions)
        .Where(q => q.GameId == gameId && !q.Answered)
        .OrderBy(q => q.Id)
        .FirstOrDefaultAsync();

    if (nextQuestion == null)
    {
        await EndGame(gameId, dbContext);
        return;
    }

    BroadcastToGame(gameId, new
    {
        type = "new-question",
        question = new
        {
            id = nextQuestion.Id,
            text = nextQuestion.QuestionText,
            options = nextQuestion.QuestionOptions.Select(o => o.OptionText).ToList(),
            timeLimit = 20
        }
    });
}

async Task SendQuestionResults(string gameId, string questionId, KahootContext dbContext)
{
    var question = await dbContext.Questions
        .Include(q => q.QuestionOptions)
        .Include(q => q.PlayerAnswers)
        .ThenInclude(pa => pa.Player)
        .FirstOrDefaultAsync(q => q.Id == questionId);

    if (question == null) return;

    var correctOption = question.QuestionOptions.First(o => o.IsCorrect);
    var results = question.PlayerAnswers.Select(pa => new
    {
        playerId = pa.PlayerId,
        correct = pa.SelectedOptionId == correctOption.Id,
        // Fix: Calculate time difference correctly
        time = pa.AnswerTimestamp.HasValue 
            ? (DateTime.UtcNow - pa.AnswerTimestamp.Value).TotalSeconds 
            : 0
    }).ToList(); // Add ToList() to materialize the query

    question.Answered = true;
    await dbContext.SaveChangesAsync();

    BroadcastToGame(gameId, new
    {
        type = "question-results",
        correctOptionIndex = question.QuestionOptions.ToList().IndexOf(correctOption),
        results
    });
}

async Task EndGame(string gameId, KahootContext dbContext)
{
    var leaderboard = await dbContext.Players
        .Where(p => p.GameId == gameId)
        .Select(p => new
        {
            nickname = p.Nickname,
            score = p.PlayerAnswers.Count(pa => pa.SelectedOption!.IsCorrect) * 100
        })
        .OrderByDescending(p => p.score)
        .ToListAsync();

    BroadcastToGame(gameId, new
    {
        type = "game-over",
        leaderboard
    });
}

void BroadcastToGame(string gameId, object message)
{
    if (!gameConnections.ContainsKey(gameId)) return;
    
    var json = JsonSerializer.Serialize(message);
    foreach (var connection in gameConnections[gameId])
    {
        connection.Send(json);
    }
}

/*app.UseRouting();
app.UseCors();*/
    
// Keep the application running
await app.RunAsync();