using System.Globalization;
using Godot;

namespace Rts.Game;

// Nó de diagnóstico: inerte sem --smoke-seconds=N. Mede a taxa, o hash no tick 100, o HUD e a câmera (CA-11..13).
public partial class SmokeRun : Node
{
    public const string SecondsArgPrefix = "--smoke-seconds=";
    public const int MinSeconds = 10;
    public const int MaxSeconds = 600;
    public const long HashCheckTick = 100;
    public const ulong ExpectedHashAtCheckTick = 8913928992633490408UL;
    public const int ExitPass = 0;
    public const int ExitFail = 1;
    public const int ExitDataError = 2;
    public const int ExitBadArgs = 3;

    private SimHost _host = null!;
    private Camera2D _cam = null!;
    private Label _hud = null!;
    private int _seconds;
    private bool _started;
    private bool _done;
    private ulong _startUsec;
    private long _startTick;
    private ulong _hash100;
    private int _step;
    private Vector2 _p0;
    private Vector2 _p1;
    private float _z0;
    private float _z1;
    private float _z2;

    public override void _Ready()
    {
        string? arg = null;
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            if (a.StartsWith(SecondsArgPrefix, System.StringComparison.Ordinal))
            {
                arg = a;
            }
        }

        if (arg is null)
        {
            SetProcess(false);
            return;
        }

        var text = arg.Substring(SecondsArgPrefix.Length);
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || seconds < MinSeconds || seconds > MaxSeconds)
        {
            Finish("SMOKE result=BAD_ARGS arg=" + arg, ExitBadArgs);
            return;
        }

        _seconds = seconds;
        _host = GetNode<SimHost>("%SimHost");
        _cam = GetNode<Camera2D>("%CameraRig");
        _hud = GetNode<Label>("%HudTick");
        if (_host.LoadError is not null)
        {
            var nl = _host.LoadError.IndexOf('\n', System.StringComparison.Ordinal);
            var first = nl < 0 ? _host.LoadError : _host.LoadError.Substring(0, nl);
            Finish("SMOKE result=DATA_ERROR message=" + first.TrimEnd('\r'), ExitDataError);
            return;
        }

        _host.Stepped += OnStepped;
        _started = true;
    }

    public override void _Process(double delta)
    {
        if (!_started || _done)
        {
            return;
        }

        var sim = _host.Simulation!;
        var now = Time.GetTicksUsec();
        if (_step == 0)
        {
            _startUsec = now;
            _startTick = sim.State.Tick;
            _step = 1;
        }

        var t = (long)(now - _startUsec);
        if (_step == 1 && t >= 1_000_000)
        {
            _p0 = _cam.Position;
            _z0 = _cam.Zoom.X;
            SendKey(Key.D, true);
            SendKey(Key.S, true);
            _step = 2;
        }
        else if (_step == 2 && t >= 2_000_000)
        {
            SendKey(Key.D, false);
            SendKey(Key.S, false);
            _p1 = _cam.Position;
            _step = 3;
        }
        else if (_step == 3 && t >= 2_500_000)
        {
            SendWheel(MouseButton.WheelUp);
            _step = 4;
        }
        else if (_step == 4 && t >= 3_000_000)
        {
            _z1 = _cam.Zoom.X;
            SendWheel(MouseButton.WheelDown);
            _step = 5;
        }
        else if (_step == 5 && t >= 3_500_000)
        {
            _z2 = _cam.Zoom.X;
            _step = 6;
        }

        if (t >= (long)_seconds * 1_000_000)
        {
            Verdict(sim, t);
        }
    }

    private void OnStepped(long tick)
    {
        if (tick == HashCheckTick)
        {
            _hash100 = _host.Simulation!.ComputeHash();
        }
    }

    private void Verdict(Rts.Sim.Core.Simulation sim, long windowMicros)
    {
        var ticks = sim.State.Tick - _startTick;
        var rateMilli = ticks * 1_000_000_000L / windowMicros;
        var data = _host.Data!;
        var resources = data.Resources.Count;
        var techs = data.Techs.Count;
        var cameraMoved = _step >= 3 && _p1.X > _p0.X && _p1.Y > _p0.Y;
        var zoomIn = _step >= 5 && _z1 > _z0;
        var zoomOut = _step >= 6 && _z2 < _z1;

        var lines = _hud.Text.Split('\n');
        var hudOk = lines.Length >= 4
            && lines[0] == "Tick: " + sim.State.Tick.ToString(CultureInfo.InvariantCulture)
            && lines[2] == "Hash: " + (sim.ComputeHash() >> 32).ToString("x8", CultureInfo.InvariantCulture)
            && lines[3] == "Recursos: 5 · Techs: 4";

        var pass = rateMilli >= 19_000 && rateMilli <= 21_000
            && _hash100 == ExpectedHashAtCheckTick
            && resources == 5 && techs == 4
            && cameraMoved && zoomIn && zoomOut && hudOk;

        var line = string.Concat(
            "SMOKE result=", pass ? "PASS" : "FAIL",
            " seconds=", _seconds.ToString(CultureInfo.InvariantCulture),
            " window_us=", windowMicros.ToString(CultureInfo.InvariantCulture),
            " ticks=", ticks.ToString(CultureInfo.InvariantCulture),
            " rate_milli=", rateMilli.ToString(CultureInfo.InvariantCulture),
            " hash100=", _hash100.ToString(CultureInfo.InvariantCulture),
            " resources=", resources.ToString(CultureInfo.InvariantCulture),
            " techs=", techs.ToString(CultureInfo.InvariantCulture),
            " camera_moved=", cameraMoved ? "1" : "0",
            " zoom_in=", zoomIn ? "1" : "0",
            " zoom_out=", zoomOut ? "1" : "0",
            " hud_ok=", hudOk ? "1" : "0",
            " dropped_us=", _host.DroppedMicros.ToString(CultureInfo.InvariantCulture),
            " source=", _host.DataSource == SimHost.DataResPath ? "res" : "dir",
            " exported=", OS.HasFeature("template") ? "1" : "0");
        Finish(line, pass ? ExitPass : ExitFail);
    }

    private void Finish(string line, int code)
    {
        _done = true;
        SetProcess(false);
        GD.Print(line);
        GetTree().Quit(code);
    }

    private static void SendKey(Key key, bool pressed) =>
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = pressed });

    private static void SendWheel(MouseButton button) =>
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = button, Pressed = true });
}
