using EFScaffold;
using EFScaffold.EntityFramework;

namespace Api.Utilities;

public class Seeder(KahootContext context)
{
    public async Task<string> SeedDefaultGameReturnId()
    {
        await context.Database.EnsureCreatedAsync();
        var gameId = Guid.NewGuid().ToString();
        var game = new Game
        {
            Id = gameId,
            Name = "Test Quiz",
            Questions = new List<Question>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    QuestionText = "What is the capital of France?",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "Paris",
                            IsCorrect = true
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "London",
                            IsCorrect = false
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "Berlin",
                            IsCorrect = false
                        }
                    }
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    QuestionText = "What is 2 + 2?",
                    QuestionOptions = new List<QuestionOption>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "3",
                            IsCorrect = false
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "4",
                            IsCorrect = true
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            OptionText = "5",
                            IsCorrect = false
                        }
                    }
                }
            }
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();
        return gameId;
    }
}