namespace Rts.Sim.State;

public sealed class GameSetupException : System.ArgumentException
{
    public GameSetupException(GameSetupError error, string detail)
        : base($"{error}: {detail}")
    {
        Error = error;
    }

    public GameSetupError Error { get; }
}
