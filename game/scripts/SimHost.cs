using Godot;
using Rts.Sim.Commands;
using Rts.Sim.Control;
using Rts.Sim.Core;
using Rts.Sim.Data;
using Rts.Sim.Hosting;
using Rts.Sim.State;
using Rts.Sim.Systems;

namespace Rts.Game;

// Adaptador fino entre o Godot e a Sim: carrega dados, monta o estado e chama Step a 20 ticks/s (ADR-003, ADR-009).
public partial class SimHost : Node
{
    public const string DataResPath = "res://data";
    public const string DataDirArgPrefix = "--data-dir=";

    private readonly TickClock _clock = new();
    private SimLoop? _loop;

    public DataSet? Data { get; private set; }

    public string DataSource { get; private set; } = "";

    public Simulation? Simulation { get; private set; }

    public HumanController? LocalHuman { get; private set; }

    public string? LoadError { get; private set; }

    public bool IsRunning => Simulation is not null;

    public float InterpolationAlpha => IsRunning ? FixedConvert.ToFloat(_clock.Alpha) : 0f;

    public long DroppedMicros => IsRunning ? _clock.DroppedMicros : 0L;

    // Disparado depois de cada Step, com State.Tick já incrementado (1, 2, 3, ...).
    public event System.Action<long>? Stepped;

    public static long DeltaToMicros(double delta)
    {
        if (!(delta > 0))
        {
            return 0;
        }

        var micros = System.Math.Round(delta * 1_000_000.0, System.MidpointRounding.ToEven);
        var limit = long.MaxValue - TickClock.TickMicros;
        return micros >= limit ? limit : (long)micros;
    }

    public override void _Ready()
    {
        string? dataDir = null;
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith(DataDirArgPrefix, System.StringComparison.Ordinal))
            {
                dataDir = arg.Substring(DataDirArgPrefix.Length);
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(dataDir))
            {
                DataSource = dataDir;
                Data = DataLoader.Load(dataDir);
            }
            else
            {
                DataSource = DataResPath;
                Data = DataLoader.Parse(ResDataReader.Read(DataResPath));
            }
        }
        catch (System.Exception ex) when (ex is DataValidationException or System.IO.IOException or System.UnauthorizedAccessException)
        {
            LoadError = ex.Message;
            GD.PushError(ex.Message);
            SetProcess(false);
            return;
        }

        var state = GameState.Create(DemoSetup.Create(), Data);
        var simulation = new Simulation(state, System.Array.Empty<ISimSystem>(), new CommandRegistry());
        var controllers = new ControllerSet(state);
        var humans = new System.Collections.Generic.List<HumanController>();
        foreach (var country in state.Countries)
        {
            var player = country.Controller.Player;
            if (country.Controller.Kind == ControllerKind.Human)
            {
                HumanController? human = null;
                foreach (var h in humans)
                {
                    if (h.Identity.Player == player)
                    {
                        human = h;
                    }
                }

                if (human is null)
                {
                    human = new HumanController(player);
                    humans.Add(human);
                }

                controllers.Assign(country.Id, human);
            }
            else
            {
                controllers.Assign(country.Id, new AiController(player));
            }
        }

        HumanController? local = null;
        foreach (var h in humans)
        {
            if (h.Identity.Player == DemoSetup.LocalPlayer)
            {
                local = h;
            }
        }

        Simulation = simulation;
        LocalHuman = local;
        _loop = new SimLoop(simulation, controllers, new CommandQueue());
    }

    public override void _Process(double delta)
    {
        if (_loop is null || Simulation is null)
        {
            return;
        }

        var n = _clock.Advance(DeltaToMicros(delta));
        for (var i = 0; i < n; i++)
        {
            _loop.Tick();
            Stepped?.Invoke(Simulation.State.Tick);
        }
    }
}
