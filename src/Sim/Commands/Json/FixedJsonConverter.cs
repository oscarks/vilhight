using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rts.Sim.Core;

namespace Rts.Sim.Commands.Json;

// Fixed trafega como o Raw inteiro de 64 bits; nunca como número fracionário.
internal sealed class FixedJsonConverter : JsonConverter<Fixed>
{
    public override Fixed Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Fixed.FromRaw(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, Fixed value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Raw);
}
