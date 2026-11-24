using Godot;

namespace Rts.Game;

// Câmera mínima (DD-06): WASD e roda do mouse. Não lê nem altera a Sim (CA-13). E1-4 a substitui.
public partial class CameraRig : Camera2D
{
    [Export]
    public float PanSpeed { get; set; } = 800f;

    [Export]
    public float ZoomStep { get; set; } = 1.1f;

    [Export]
    public float MinZoom { get; set; } = 0.25f;

    [Export]
    public float MaxZoom { get; set; } = 4f;

    public override void _Process(double delta)
    {
        var dir = Input.GetVector("camera_left", "camera_right", "camera_up", "camera_down");
        Position += dir * PanSpeed * (float)delta / Zoom.X;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            var z = Zoom.X;
            if (mouse.ButtonIndex == MouseButton.WheelUp)
            {
                z = System.Math.Min(z * ZoomStep, MaxZoom);
            }
            else if (mouse.ButtonIndex == MouseButton.WheelDown)
            {
                z = System.Math.Max(z / ZoomStep, MinZoom);
            }
            else
            {
                return;
            }

            Zoom = new Vector2(z, z);
        }
    }
}
