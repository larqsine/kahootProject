namespace Api.Utilities;

public interface IGameTimeProvider
{
    public int MilliSeconds { get; set; }
}

public class GameTimeProvider : IGameTimeProvider
{
    public int MilliSeconds { get; set; } = 10_000;
}