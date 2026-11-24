using Rts.Sim.Core;
using Rts.Sim.State;
using Rts.Sim.Systems;

namespace Rts.Sim.Tests.Fixtures;

public sealed class TestIncomeSystem : ISimSystem
{
    private static readonly Fixed Income = Fixed.FromRatio(1, 20);

    public void Tick(GameState state, SimContext context)
    {
        if (state.ResourceCount <= 0)
        {
            return;
        }

        var r = new ResourceId(0);
        for (var i = 0; i < state.Countries.Count; i++)
        {
            var c = state.Countries[i];
            c.SetStock(r, c.GetStock(r) + Income);
        }
    }
}
