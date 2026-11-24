using System.Collections.Generic;
using System.Globalization;
using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Systems;

namespace Rts.Sim.Tests.Fixtures;

public sealed class RecordingSystem : ISimSystem
{
    private readonly string _name;
    private readonly List<string> _log;

    public RecordingSystem(string name, List<string> log)
    {
        _name = name;
        _log = log;
    }

    public void Tick(GameState state, SimContext context)
    {
        _log.Add(_name + "@" + context.Tick.ToString(CultureInfo.InvariantCulture));
    }
}
