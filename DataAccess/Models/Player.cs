namespace EFScaffold.EntityFramework;

public class Player
{
    public string Id { get; set; } = null!;

    public string? GameId { get; set; }

    public string Nickname { get; set; } = null!;

    public virtual Game? Game { get; set; }

    public virtual ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
}