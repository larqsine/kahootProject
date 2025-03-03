using Microsoft.AspNetCore.Mvc;
using EFScaffold.EntityFramework;
using Microsoft.AspNetCore.SignalR;

namespace Api;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameController(GameService gameService, IHubContext<GameHub> hubContext)
    {
        _gameService = gameService;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGame(CreateGameDto dto)
    {
        var game = await _gameService.CreateGame(dto.Name);
        return Ok(new { gameId = game.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetGames()
    {
        var games = await _gameService.GetGames();
        return Ok(games);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(string id)
    {
        var game = await _gameService.GetGameById(id);
        if (game == null)
            return NotFound();
        return Ok(game);
    }

    [HttpPost("{id}/questions")]
    public async Task<IActionResult> AddQuestion(string id, AddQuestionDto dto)
    {
        var question = await _gameService.AddQuestion(
            id, 
            dto.QuestionText, 
            dto.Options.Select(o => (o.Text, o.IsCorrect)).ToList()
        );
        return Ok(question);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartGame(string id)
    {
        var game = await _gameService.GetGameById(id);
        if (game == null)
            return NotFound();
            
        // Notify all players the game is starting
        await _hubContext.Clients.Group(id).SendAsync("GameStarted");
        return Ok();
    }

    [HttpPost("{gameId}/questions/{questionId}/start")]
    public async Task<IActionResult> StartQuestion(string gameId, string questionId)
    {
        var game = await _gameService.GetGameById(gameId);
        if (game == null)
            return NotFound();

        var question = game.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return NotFound();
            
        // Send the question to all players
        await _hubContext.Clients.Group(gameId).SendAsync("QuestionStarted", new {
            id = question.Id,
            text = question.QuestionText,
            options = question.QuestionOptions.Select(o => new { 
                id = o.Id, 
                text = o.OptionText 
            })
        });
        
        return Ok();
    }

    [HttpPost("{gameId}/questions/{questionId}/end")]
    public async Task<IActionResult> EndQuestion(string gameId, string questionId)
    {
        await _gameService.MarkQuestionAsAnswered(questionId);
        
        // Notify all clients that the question has ended
        await _hubContext.Clients.Group(gameId).SendAsync("QuestionEnded", questionId);
        return Ok();
    }

    [HttpGet("{id}/scores")]
    public async Task<IActionResult> GetScores(string id)
    {
        var scores = await _gameService.GetScores(id);
        
        // Send scores to all clients
        await _hubContext.Clients.Group(id).SendAsync("ScoresUpdated", scores);
        return Ok(scores);
    }
}

// DTOs
public class CreateGameDto
{
    public string Name { get; set; }
}

public class AddQuestionDto
{
    public string QuestionText { get; set; }
    public List<OptionDto> Options { get; set; }
}

public class OptionDto
{
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
}

