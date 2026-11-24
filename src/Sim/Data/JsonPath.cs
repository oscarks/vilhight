using System.Globalization;

namespace Rts.Sim.Data;

// Monta caminhos como "$.techs[1].cost.ore".
internal static class JsonPath
{
    public const string Root = "$";

    public static string Prop(string parent, string name) => parent + "." + name;

    public static string Index(string parent, int index) =>
        parent + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
}
