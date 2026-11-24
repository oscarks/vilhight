using System;
using System.Collections.Generic;
using Rts.Sim.Core;
using Rts.Sim.State;

namespace Rts.Sim.Commands;

public sealed class CommandRegistry
{
    public const int MaxTypeNameLength = 64;

    // Dictionaries só para busca (nunca iterados); _ordered é a única coleção iterada (pelo codec).
    private readonly Dictionary<Type, Entry> _byType = new();
    private readonly Dictionary<string, Entry> _byName = new(StringComparer.Ordinal);
    private readonly List<Entry> _ordered = new();

    public int Count => _ordered.Count;

    public bool IsFrozen { get; private set; }

    public void Register<T>(string typeName, CommandHandler<T> handler)
        where T : Command
    {
        ArgumentNullException.ThrowIfNull(typeName);
        ArgumentNullException.ThrowIfNull(handler);
        if (!IsValidName(typeName))
        {
            throw new ArgumentException("invalid command type name: " + typeName, nameof(typeName));
        }

        if (typeof(T).IsAbstract)
        {
            throw new ArgumentException("command type must be concrete: " + typeof(T).Name);
        }

        if (IsFrozen)
        {
            throw new InvalidOperationException("registry is frozen");
        }

        if (_byName.ContainsKey(typeName))
        {
            throw new InvalidOperationException("duplicate command type name: " + typeName);
        }

        if (_byType.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException("command type already registered: " + typeof(T).Name);
        }

        var entry = new Entry(typeName, typeof(T), (s, c, cmd) => handler(s, c, (T)cmd));
        _byType.Add(typeof(T), entry);
        _byName.Add(typeName, entry);
        _ordered.Add(entry);
    }

    public void Freeze() => IsFrozen = true;

    public bool TryGetTypeName(Type commandType, out string typeName)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        if (_byType.TryGetValue(commandType, out var entry))
        {
            typeName = entry.TypeName;
            return true;
        }

        typeName = string.Empty;
        return false;
    }

    public bool TryGetCommandType(string typeName, out Type commandType)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        if (_byName.TryGetValue(typeName, out var entry))
        {
            commandType = entry.CommandType;
            return true;
        }

        commandType = typeof(Command);
        return false;
    }

    internal bool TryGetEntry(Type commandType, out Entry entry) => _byType.TryGetValue(commandType, out entry);

    internal IReadOnlyList<Entry> Entries => _ordered;

    // INV-REG-02: ^[a-z][A-Za-z0-9]*(\.[a-z][A-Za-z0-9]*)*$ e Length <= 64.
    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || name.Length > MaxTypeNameLength)
        {
            return false;
        }

        var segmentStart = true;
        foreach (var ch in name)
        {
            if (segmentStart)
            {
                if (ch < 'a' || ch > 'z')
                {
                    return false;
                }

                segmentStart = false;
            }
            else if (ch == '.')
            {
                segmentStart = true;
            }
            else if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')))
            {
                return false;
            }
        }

        return !segmentStart;
    }

    internal readonly record struct Entry(
        string TypeName,
        Type CommandType,
        Func<GameState, SimContext, Command, CommandRejectReason> Invoke);
}
