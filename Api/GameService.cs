using EFScaffold;
using EFScaffold.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Api;

public class GameService
{
    private readonly KahootContext _context;
    
    public GameService(KahootContext context)
    {
        _context = context;
    }
    
    public async Task<Game> CreateGame(string name)
    {
        var game = new Game
        {
            Id = Guid.NewGuid().ToString(),
            Name = name
        };
        
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        return game;
    }
    
    public async Task<Player> AddPlayerToGame(string gameId, string nickname)
    {
        var player = new Player
        {
            Id = Guid.NewGuid().ToString(),
            GameId = gameId,
            Nickname = nickname
        };
        
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }
    
    public async Task RemovePlayerFromGame(string playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player != null)
        {
            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<Question> AddQuestion(string gameId, string questionText, List<(string text, bool isCorrect)> options)
    {
        var question = new Question
        {
            Id = Guid.NewGuid().ToString(),
            GameId = gameId,
            QuestionText = questionText,
            Answered = false
        };
        
        _context.Questions.Add(question);
        
        foreach (var option in options)
        {
            _context.QuestionOptions.Add(new QuestionOption
            {
                Id = Guid.NewGuid().ToString(),
                QuestionId = question.Id,
                OptionText = option.text,
                IsCorrect = option.isCorrect
            });
        }
        
        await _context.SaveChangesAsync();
        return question;
    }
    
    public async Task SubmitAnswer(string playerId, string questionId, string optionId)
    {
        var playerAnswer = new PlayerAnswer
        {
            PlayerId = playerId,
            QuestionId = questionId,
            SelectedOptionId = optionId,
            AnswerTimestamp = DateTime.UtcNow
        };
        
        _context.PlayerAnswers.Add(playerAnswer);
        await _context.SaveChangesAsync();
    }
    
    public async Task<List<Game>> GetGames()
    {
        return await _context.Games.ToListAsync();
    }
    
    public async Task<Game> GetGameById(string gameId)
    {
        return await _context.Games
            .Include(g => g.Questions)
                .ThenInclude(q => q.QuestionOptions)
            .Include(g => g.Players)
            .FirstOrDefaultAsync(g => g.Id == gameId);
    }
    
    public async Task MarkQuestionAsAnswered(string questionId)
    {
        var question = await _context.Questions.FindAsync(questionId);
        if (question != null)
        {
            question.Answered = true;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<Dictionary<string, int>> GetScores(string gameId)
    {
        var scores = new Dictionary<string, int>();
        
        var players = await _context.Players
            .Where(p => p.GameId == gameId)
            .Include(p => p.PlayerAnswers)
                .ThenInclude(pa => pa.SelectedOption)
            .ToListAsync();
            
        foreach (var player in players)
        {
            int score = player.PlayerAnswers
                .Where(pa => pa.SelectedOption != null && pa.SelectedOption.IsCorrect)
                .Count();
                
            scores.Add(player.Nickname, score);
        }
        
        return scores;
    }
}