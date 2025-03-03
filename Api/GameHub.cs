

using EFScaffold;
using Microsoft.AspNetCore.SignalR;
using EFScaffold.EntityFramework;

namespace Api;

public class GameHub : Hub
{
    private readonly KahootContext _context;
    private readonly GameService _gameService;

    public GameHub(KahootContext context, GameService gameService)
    {
        _context = context;
        _gameService = gameService;
    }

    // Player methods
    public async Task JoinGame(string gameId, string nickname)
    {
        var player = await _gameService.AddPlayerToGame(gameId, nickname);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        Context.Items["PlayerId"] = player.Id;
        Context.Items["GameId"] = gameId;
        Context.Items["Role"] = "player";
        
        await Clients.Group(gameId).SendAsync("PlayerJoined", player.Nickname);
    }
    
    public async Task SubmitAnswer(string questionId, string optionId)
    {
        var playerId = Context.Items["PlayerId"] as string;
        if (playerId != null)
        {
            await _gameService.SubmitAnswer(playerId, questionId, optionId);
            
            var gameId = Context.Items["GameId"] as string;
            await Clients.Group(gameId).SendAsync("AnswerSubmitted", playerId, questionId);
        }
    }
    
    // Host methods - moved from controller
    public async Task RegisterAsHost()
    {
        Context.Items["Role"] = "host";
        await Clients.Caller.SendAsync("HostRegistered");
    }
    
    public async Task<string> CreateGame(string name)
    {
        var game = await _gameService.CreateGame(name);
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
        Context.Items["GameId"] = game.Id;
        
        return game.Id;
    }
    
    public async Task<List<Game>> GetGames()
    {
        return await _gameService.GetGames();
    }
    
    public async Task<Game> GetGameById(string gameId)
    {
        return await _gameService.GetGameById(gameId);
    }
    
    public async Task<object> AddQuestion(string gameId, string questionText, List<QuestionOptionDto> options)
{
    if (!IsHostOrAdmin())
        throw new HubException("Only hosts can add questions");
    
    try
    {
        // Debug received data
        Console.WriteLine($"Received question: {questionText}");
        Console.WriteLine($"Options count: {options?.Count}");
    
        // Create question
        var question = new Question
        {
            Id = Guid.NewGuid().ToString(),
            GameId = gameId,
            QuestionText = questionText,
            Answered = false
        };
    
        _context.Questions.Add(question);
    
        // Filter out any null or empty options
        var validOptions = options?.Where(o => !string.IsNullOrWhiteSpace(o.Text)).ToList();
    
        if (validOptions == null || validOptions.Count == 0)
            throw new HubException("At least one valid option is required");
        
        // Create list to store option IDs
        var createdOptions = new List<object>();
        
        foreach (var option in validOptions)
        {
            Console.WriteLine($"Adding option: {option.Text}, IsCorrect: {option.IsCorrect}");
            
            var questionOption = new QuestionOption
            {
                Id = Guid.NewGuid().ToString(),
                QuestionId = question.Id,
                OptionText = option.Text,
                IsCorrect = option.IsCorrect
            };
            
            _context.QuestionOptions.Add(questionOption);
            
            // Add to simplified list
            createdOptions.Add(new { 
                id = questionOption.Id,
                text = questionOption.OptionText,
                isCorrect = questionOption.IsCorrect
            });
        }
    
        await _context.SaveChangesAsync();
        
        // Return a simple DTO object instead of the EF entity
        return new {
            id = question.Id,
            text = question.QuestionText,
            options = createdOptions
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR in AddQuestion: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        throw;
    }
}
    
    public async Task StartGame(string gameId)
    {
        if (!IsHostOrAdmin())
            throw new HubException("Only hosts can start games");
            
        var game = await _gameService.GetGameById(gameId);
        if (game == null)
            throw new HubException("Game not found");
            
        await Clients.Group(gameId).SendAsync("GameStarted");
    }
    
    public async Task StartQuestion(string gameId, string questionId)
    {
        if (!IsHostOrAdmin())
            throw new HubException("Only hosts can start questions");
            
        var game = await _gameService.GetGameById(gameId);
        if (game == null)
            throw new HubException("Game not found");

        var question = game.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            throw new HubException("Question not found");
            
        await Clients.Group(gameId).SendAsync("QuestionStarted", new {
            id = question.Id,
            text = question.QuestionText,
            options = question.QuestionOptions.Select(o => new { id = o.Id, text = o.OptionText })
        });
    }
    
    public async Task EndQuestion(string gameId, string questionId)
    {
        if (!IsHostOrAdmin())
            throw new HubException("Only hosts can end questions");
            
        await _gameService.MarkQuestionAsAnswered(questionId);
        await Clients.Group(gameId).SendAsync("QuestionEnded", questionId);
    }
    
    public async Task<Dictionary<string, int>> GetScores(string gameId)
    {
        var scores = await _gameService.GetScores(gameId);
        await Clients.Group(gameId).SendAsync("ScoresUpdated", scores);
        return scores;
    }
    
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var gameId = Context.Items["GameId"] as string;
        var playerId = Context.Items["PlayerId"] as string;
        
        if (gameId != null && playerId != null)
        {
            await _gameService.RemovePlayerFromGame(playerId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", playerId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    private bool IsHostOrAdmin()
    {
        return Context.Items["Role"]?.ToString() == "host";
    }
    
    public class QuestionOptionDto
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}