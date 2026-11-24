namespace Rts.Sim.State;

public sealed class Player
{
    internal Player(PlayerId id, string name)
    {
        Id = id;
        Name = name;
    }

    public PlayerId Id { get; }
    public string Name { get; }
}
