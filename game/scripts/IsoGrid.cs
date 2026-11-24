using Godot;

namespace Rts.Game;

// Grade isométrica 2:1 de placeholder (E0-9). A projeção é privada e será substituída no E1-2.
public partial class IsoGrid : Node2D
{
    private static readonly Color ColorA = new(0.30f, 0.55f, 0.30f);
    private static readonly Color ColorB = new(0.26f, 0.48f, 0.26f);
    private static readonly Color Outline = new(0.15f, 0.30f, 0.15f);

    [Export]
    public int GridWidth { get; set; } = 32;

    [Export]
    public int GridHeight { get; set; } = 32;

    [Export]
    public int TileWidth { get; set; } = 64;

    [Export]
    public int TileHeight { get; set; } = 32;

    public override void _Draw()
    {
        var hw = TileWidth / 2f;
        var hh = TileHeight / 2f;
        for (var y = 0; y < GridHeight; y++)
        {
            for (var x = 0; x < GridWidth; x++)
            {
                var cx = (x - y) * TileWidth / 2f;
                var cy = (x + y) * TileHeight / 2f;
                var top = new Vector2(cx, cy - hh);
                var right = new Vector2(cx + hw, cy);
                var bottom = new Vector2(cx, cy + hh);
                var left = new Vector2(cx - hw, cy);
                DrawColoredPolygon(new[] { top, right, bottom, left }, (x + y) % 2 == 0 ? ColorA : ColorB);
                DrawPolyline(new[] { top, right, bottom, left, top }, Outline, 1f);
            }
        }
    }
}
