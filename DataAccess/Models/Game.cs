namespace EFScaffold.EntityFramework;

public class Game
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Player> Players { get; set; } = new List<Player>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}