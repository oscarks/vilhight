using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Data;

// Etapa 5: ciclos.
internal sealed partial class DataCompiler
{
    // ---------- etapa 5: ciclos ----------

    private void Stage5()
    {
        _file = DataFiles.Techs;
        var color = new byte[_techs.Count]; // 0 branco, 1 cinza, 2 preto
        var stack = new List<int>();
        for (var i = 0; i < _techs.Count; i++)
        {
            if (_techs[i] != null && color[i] == 0)
            {
                Visit(i, color, stack);
            }
        }
    }

    private void Visit(int u, byte[] color, List<int> stack)
    {
        color[u] = 1;
        stack.Add(u);
        var tu = _techs[u]!;
        foreach (var q in tu.Requires)
        {
            if (!_techMap.TryGetValue(q.Key, out var v))
            {
                continue;
            }

            if (color[v] == 1)
            {
                var sb = new StringBuilder("cycle: ");
                var start = stack.IndexOf(v);
                for (var k = start; k < stack.Count; k++)
                {
                    sb.Append(_techs[stack[k]]!.Id).Append(" → ");
                }

                sb.Append(_techs[v]!.Id);
                Err(DataErrorCode.TechCycle, JsonPath.Index(JsonPath.Prop(JsonPath.Index("$.techs", u), "requires"), q.Index), tu.Id, sb.ToString());
            }
            else if (color[v] == 0)
            {
                Visit(v, color, stack);
            }
        }

        stack.RemoveAt(stack.Count - 1);
        color[u] = 2;
    }
}
