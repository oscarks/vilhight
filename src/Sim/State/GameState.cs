using System;
using System.Collections.Generic;
using System.Globalization;
using Rts.Sim.Core;

namespace Rts.Sim.State;

public sealed class GameState
{
    private readonly Player[] _players;
    private readonly Country[] _countries;

    private GameState(
        ulong seed,
        int resourceCount,
        Player[] players,
        Universe universe,
        Country[] countries)
    {
        Seed = seed;
        Random = new SimRandom(seed);
        ResourceCount = resourceCount;
        _players = players;
        Players = Array.AsReadOnly(players);
        Universe = universe;
        _countries = countries;
        Countries = Array.AsReadOnly(countries);
    }

    public ulong Seed { get; }

    // 0 após Create; incrementado só por Step (Fase 2).
    public long Tick { get; internal set; }

    internal SimRandom Random { get; }

    public int ResourceCount { get; }

    public IReadOnlyList<Player> Players { get; }

    public Universe Universe { get; }

    public IReadOnlyList<Country> Countries { get; }

    public Player? FindPlayer(PlayerId id)
    {
        var lo = 0;
        var hi = _players.Length - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var v = _players[mid].Id.Value;
            if (v == id.Value)
            {
                return _players[mid];
            }

            if (v < id.Value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return null;
    }

    public Country? FindCountry(CountryId id)
    {
        var lo = 0;
        var hi = _countries.Length - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var v = _countries[mid].Id.Value;
            if (v == id.Value)
            {
                return _countries[mid];
            }

            if (v < id.Value)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return null;
    }

    // design: Dados; AI-SPEC Foundation Fase 3 RN-P3-62. O DataSet não é guardado no estado (DD-P3-14).
    public static GameState Create(GameSetup setup, Rts.Sim.Data.DataSet data)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(data);
        return Create(setup, data.Resources.Count);
    }

    // design: Planeta, países e espécies; AI-SPEC Foundation Fase 1 §5.6 (ordem de validação).
    public static GameState Create(GameSetup setup, int resourceCount)
    {
        ArgumentNullException.ThrowIfNull(setup);
        Validate(setup, resourceCount);

        var players = new Player[setup.Players.Count];
        for (var i = 0; i < players.Length; i++)
        {
            var p = setup.Players[i];
            players[i] = new Player(p.Id, p.Name);
        }

        Array.Sort(players, static (a, b) => a.Id.Value.CompareTo(b.Id.Value));

        var totalCountries = 0;
        var systems = new StarSystem[setup.StarSystems.Count];
        for (var i = 0; i < systems.Length; i++)
        {
            var ss = setup.StarSystems[i];
            var worlds = new World[ss.Worlds.Count];
            for (var j = 0; j < worlds.Length; j++)
            {
                var ws = ss.Worlds[j];
                var countries = new Country[ws.Countries.Count];
                for (var k = 0; k < countries.Length; k++)
                {
                    var cs = ws.Countries[k];
                    countries[k] = new Country(cs.Id, ws.Id, cs.Name, cs.Controller, resourceCount);
                }

                Array.Sort(countries, static (a, b) => a.Id.Value.CompareTo(b.Id.Value));
                totalCountries += countries.Length;
                worlds[j] = new World(ws.Id, ss.Id, countries);
            }

            Array.Sort(worlds, static (a, b) => a.Id.Value.CompareTo(b.Id.Value));
            systems[i] = new StarSystem(ss.Id, ss.Owner, worlds);
        }

        Array.Sort(systems, static (a, b) => a.Id.Value.CompareTo(b.Id.Value));

        var all = new Country[totalCountries];
        var n = 0;
        foreach (var s in systems)
        {
            foreach (var w in s.Worlds)
            {
                foreach (var c in w.Countries)
                {
                    all[n++] = c;
                }
            }
        }

        Array.Sort(all, static (a, b) => a.Id.Value.CompareTo(b.Id.Value));

        return new GameState(setup.Seed, resourceCount, players, new Universe(systems), all);
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static void Validate(GameSetup setup, int resourceCount)
    {
        if (resourceCount < 0)
        {
            throw new GameSetupException(
                GameSetupError.NegativeResourceCount,
                "resourceCount is " + Num(resourceCount));
        }

        var players = setup.Players ?? throw new ArgumentNullException(nameof(setup.Players));
        if (players.Count == 0)
        {
            throw new GameSetupException(GameSetupError.NoPlayers, "setup has no players");
        }

        var playerIds = new HashSet<int>();
        foreach (var p in players)
        {
            if (p is null)
            {
                throw new ArgumentNullException(nameof(setup.Players), "player element is null");
            }

            if (p.Id.Value < 0)
            {
                throw new GameSetupException(GameSetupError.NegativeId, "player " + Num(p.Id.Value));
            }

            CheckName(p.Name, "player", p.Id.Value);
            if (!playerIds.Add(p.Id.Value))
            {
                throw new GameSetupException(GameSetupError.DuplicatePlayerId, Num(p.Id.Value));
            }
        }

        var systems = setup.StarSystems ?? throw new ArgumentNullException(nameof(setup.StarSystems));
        if (systems.Count == 0)
        {
            throw new GameSetupException(GameSetupError.NoStarSystems, "setup has no star systems");
        }

        var systemIds = new HashSet<int>();
        var worldIds = new HashSet<int>();
        var countryIds = new HashSet<int>();
        foreach (var s in systems)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(setup.StarSystems), "star system element is null");
            }

            if (s.Id.Value < 0)
            {
                throw new GameSetupException(GameSetupError.NegativeId, "starSystem " + Num(s.Id.Value));
            }

            if (!systemIds.Add(s.Id.Value))
            {
                throw new GameSetupException(GameSetupError.DuplicateStarSystemId, Num(s.Id.Value));
            }

            if (!playerIds.Contains(s.Owner.Value))
            {
                throw new GameSetupException(
                    GameSetupError.UnknownStarSystemOwner,
                    "starSystem " + Num(s.Id.Value) + " owner " + Num(s.Owner.Value));
            }

            var worlds = s.Worlds ?? throw new ArgumentNullException(nameof(s.Worlds));
            foreach (var w in worlds)
            {
                if (w is null)
                {
                    throw new ArgumentNullException(nameof(s.Worlds), "world element is null");
                }

                if (w.Id.Value < 0)
                {
                    throw new GameSetupException(GameSetupError.NegativeId, "world " + Num(w.Id.Value));
                }

                if (!worldIds.Add(w.Id.Value))
                {
                    throw new GameSetupException(GameSetupError.DuplicateWorldId, Num(w.Id.Value));
                }

                var countries = w.Countries ?? throw new ArgumentNullException(nameof(w.Countries));
                if (countries.Count > GameStateLimits.MaxCountriesPerWorld)
                {
                    throw new GameSetupException(
                        GameSetupError.TooManyCountries,
                        "world " + Num(w.Id.Value) + " has " + Num(countries.Count) + " countries (max "
                        + Num(GameStateLimits.MaxCountriesPerWorld) + ")");
                }

                foreach (var c in countries)
                {
                    if (c is null)
                    {
                        throw new ArgumentNullException(nameof(w.Countries), "country element is null");
                    }

                    if (c.Id.Value < 0)
                    {
                        throw new GameSetupException(GameSetupError.NegativeId, "country " + Num(c.Id.Value));
                    }

                    if (!countryIds.Add(c.Id.Value))
                    {
                        throw new GameSetupException(GameSetupError.DuplicateCountryId, Num(c.Id.Value));
                    }

                    CheckName(c.Name, "country", c.Id.Value);
                    var kind = c.Controller.Kind;
                    if (kind != ControllerKind.Human && kind != ControllerKind.Ai)
                    {
                        throw new GameSetupException(
                            GameSetupError.InvalidControllerKind,
                            "country " + Num(c.Id.Value) + " kind " + Num((byte)kind));
                    }

                    if (!playerIds.Contains(c.Controller.Player.Value))
                    {
                        throw new GameSetupException(
                            GameSetupError.UnknownControllerPlayer,
                            "country " + Num(c.Id.Value) + " player " + Num(c.Controller.Player.Value));
                    }
                }
            }
        }
    }

    private static void CheckName(string? name, string type, int id)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name), type + " " + Num(id) + " name is null");
        }

        if (name.Trim().Length < 1 || name.Length > GameStateLimits.MaxNameLength)
        {
            throw new GameSetupException(GameSetupError.InvalidName, type + " " + Num(id));
        }
    }
}
