using System.Text.Json.Serialization;
using Rts.Sim.State;

namespace Rts.Sim.Commands;

// ADR-004: toda ação é um comando serializável com o país-alvo e o emissor.
public abstract record Command(
    [property: JsonPropertyOrder(-2)] CountryId Country,
    [property: JsonPropertyOrder(-1)] ControllerRef Issuer);
