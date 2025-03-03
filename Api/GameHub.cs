

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
    
    public async Task<Question> AddQuestion(string gameId, string questionText, List<(string text, bool isCorrect)> options)
    {
        if (!IsHostOrAdmin())
            throw new HubException("Only hosts can add questions");
            
        return await _gameService.AddQuestion(gameId, questionText, options);
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
}