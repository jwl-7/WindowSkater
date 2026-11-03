namespace WindowSkater;

internal sealed class DoomGuy : Character
{
    public override string Name => "Doom Guy";
    public override float Speed => 1.1f;
    public override uint SparkColor => 0xFFFF4A20;

    public override Look Look { get; } = new()
    {
        Torso = 0xFF3E7A38, Plate = 0xFF55A04A, Thigh = 0xFF3E7A38, Shin = 0xFF2F5E2B, Boot = 0xFF6B4A2E,
        Arm = 0xFF3E7A38, ArmBack = 0xFF2F5E2B, Glove = 0xFF6B4A2E,
        Belt = 0xFF4A3A28, Buckle = 0xFFB0B0B8, Shoulder = 0xFF5FB055,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF4A2A18, Grip = 0xFF1A1210, Base = 0xFF6A2A18,
        Wheel = 0xFF2A2420, Hub = 0xFFFF8A20, Truck = 0xFF6A6460,
        Graphic = new[]
        {
            new Shape(0xFFE04A1C, new PointF[]
            {
                new(-26, 8), new(-22, -2), new(-16, 3), new(-10, -8), new(-4, 2),
                new(2, -10), new(8, 3), new(14, -6), new(18, 2), new(26, 8),
            }),
            new Shape(0xFFFFC030, new PointF[]
            {
                new(-16, 8), new(-12, 0), new(-6, 4), new(0, -5), new(6, 4), new(12, -2), new(16, 8),
            }),
        },
    };

    private static readonly PointF[] Shell =
        { new(63, 52), new(64, 38), new(71, 29), new(83, 26), new(94, 30), new(99, 40), new(99, 52), new(94, 60), new(68, 60) };
    private static readonly PointF[] Shade = { new(63, 52), new(64, 38), new(71, 29), new(73, 45), new(70, 58) };
    private static readonly PointF[] Crown = { new(71, 29), new(83, 26), new(94, 30), new(86, 36), new(74, 37) };
    private static readonly PointF[] Visor = { new(88, 38), new(99, 40), new(99, 48), new(90, 49), new(87, 44) };
    private static readonly PointF[] Glint = { new(91, 40), new(97, 41), new(96, 43), new(91, 42) };
    private static readonly PointF[] JawGuard = { new(85, 49), new(99, 49), new(94, 60), new(85, 58) };
    private static readonly PointF[] Vents = Shapes.Rect(90, 52, 97, 54);
    private static readonly PointF[] ChestHighlight = { new(74, 68), new(86, 68), new(85, 82), new(75, 82) };

    public override void DrawTorso(Gfx g, in Xf body)
    {
        Rig.Torso(g, Look, body);
        g.Fill(0xFF5BAE50, ChestHighlight, body);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFF3C7A36, Shell, body);
        g.Fill(0xFF2C5A28, Shade, body);
        g.Fill(0xFF5BA84D, Crown, body);
        g.Fill(0xFF2C5A28, JawGuard, body);
        g.Fill(0xFF16301A, Vents, body);
        g.Fill(0xFFFF8E24, Visor, body);
        g.Fill(0xFFFFD27A, Glint, body);
    }
}
