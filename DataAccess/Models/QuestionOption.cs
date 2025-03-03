namespace EFScaffold.EntityFramework;

public class QuestionOption
{
    public string Id { get; set; } = null!;

    public string? QuestionId { get; set; }

    public string OptionText { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();

    public virtual Question? Question { get; set; }
}