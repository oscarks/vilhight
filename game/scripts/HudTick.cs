using System.Globalization;
using Godot;

namespace Rts.Game;

// HUD do M0 (CA-12): tick, taxa medida, hash curto e contagens de conteúdo. Só lê o SimHost.
public partial class HudTick : Label
{
    private const long RateWindowMicros = 1_000_000;

    private SimHost _host = null!;
    private long _lastTick = -1;
    private bool _errorShown;
    private string _hash = "";
    private string _rate = "--";
    private bool _windowStarted;
    private ulong _windowStartUsec;
    private long _windowStartTick;

    public override void _Ready()
    {
        _host = GetNode<SimHost>("%SimHost");
    }

    public override void _Process(double delta)
    {
        if (_host.LoadError is not null)
        {
            if (!_errorShown)
            {
                Text = "Erro ao carregar dados:\n" + _host.LoadError;
                _errorShown = true;
            }

            return;
        }

        var sim = _host.Simulation;
        var data = _host.Data;
        if (sim is null || data is null)
        {
            return;
        }

        var tick = sim.State.Tick;
        var now = Time.GetTicksUsec();
        var changed = tick != _lastTick;
        if (!_windowStarted)
        {
            _windowStarted = true;
            _windowStartUsec = now;
            _windowStartTick = tick;
        }
        else if (now - _windowStartUsec >= (ulong)RateWindowMicros)
        {
            var rate = (tick - _windowStartTick) * 1e6 / (now - _windowStartUsec);
            _rate = rate.ToString("0.0", CultureInfo.InvariantCulture);
            _windowStartUsec = now;
            _windowStartTick = tick;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        if (tick != _lastTick)
        {
            _hash = (sim.ComputeHash() >> 32).ToString("x8", CultureInfo.InvariantCulture);
            _lastTick = tick;
        }

        Text = string.Concat(
            "Tick: ", tick.ToString(CultureInfo.InvariantCulture),
            "\nTaxa: ", _rate, " ticks/s",
            "\nHash: ", _hash,
            "\nRecursos: ", data.Resources.Count.ToString(CultureInfo.InvariantCulture),
            " · Techs: ", data.Techs.Count.ToString(CultureInfo.InvariantCulture));
    }
}
