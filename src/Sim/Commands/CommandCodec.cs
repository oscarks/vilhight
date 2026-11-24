using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Rts.Sim.Commands.Json;

namespace Rts.Sim.Commands;

// ADR-004: comandos serializáveis. Instância (não estática): o registro é estado de partida.
public sealed class CommandCodec
{
    private readonly CommandRegistry _registry;
    private readonly JsonSerializerOptions _options;

    public CommandCodec(CommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Freeze();
        _registry = registry;

        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type == typeof(Command))
            {
                var polymorphism = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "type",
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                };
                var entries = registry.Entries;
                for (var i = 0; i < entries.Count; i++)
                {
                    polymorphism.DerivedTypes.Add(new JsonDerivedType(entries[i].CommandType, entries[i].TypeName));
                }

                typeInfo.PolymorphismOptions = polymorphism;
            }

            if (typeInfo.Kind == JsonTypeInfoKind.Object)
            {
                foreach (var property in typeInfo.Properties)
                {
                    property.IsRequired = true;
                }
            }
        });

        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            NumberHandling = JsonNumberHandling.Strict,
            WriteIndented = false,
            TypeInfoResolver = resolver,
        };
        _options.Converters.Add(new FixedJsonConverter());
        _options.Converters.Add(new PlayerIdJsonConverter());
        _options.Converters.Add(new StarSystemIdJsonConverter());
        _options.Converters.Add(new WorldIdJsonConverter());
        _options.Converters.Add(new CountryIdJsonConverter());
        _options.Converters.Add(new ResourceIdJsonConverter());
        _options.Converters.Add(new TechIdJsonConverter());
    }

    public string Serialize(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_registry.TryGetTypeName(command.GetType(), out _))
        {
            throw new ArgumentException("command type not registered: " + command.GetType().Name);
        }

        return JsonSerializer.Serialize<Command>(command, _options);
    }

    public Command Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        Command? result;
        try
        {
            result = JsonSerializer.Deserialize<Command>(json, _options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new FormatException("invalid command json: " + ex.Message, ex);
        }

        return result ?? throw new FormatException("invalid command json: null");
    }
}
