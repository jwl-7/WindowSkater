namespace WindowSkater;

internal sealed class Stormtrooper : Character
{
    public override string Name => "Stormtrooper";
    public override Grind Signature => Grind.Boardslide;
    public override float BailChance => 0.18f;
    public override uint SparkColor => 0xFFB0E0FF;

    public override Look Look { get; } = new()
    {
        Torso = 0xFFE6E6EA, Plate = 0xFFF4F4F8, Thigh = 0xFFE2E2E8, Shin = 0xFFD2D2DA, Boot = 0xFF24242C,
        Arm = 0xFFE6E6EA, ArmBack = 0xFFC8C8D0, Glove = 0xFF24242C,
        Belt = 0xFF2A2A32, Buckle = 0xFF9A9AA4, Shoulder = 0xFFF4F4F8,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFFB8B8C0, Grip = 0xFF202024, Base = 0xFFEDEDF1,
        Wheel = 0xFF2A2A30, Hub = 0xFFA0A0A8, Truck = 0xFF8A8A94,
        Graphic = new[]
        {
            new Shape(0xFF1C1C22, Shapes.Ngon(0, 0, 8.5f, 8, 0.39f)),
            new Shape(0xFFEDEDF1, Shapes.Ngon(0, 0, 5f, 8, 0.39f)),
            new Shape(0xFF1C1C22, Shapes.Rect(-26, -1.5f, -12, 1.5f)),
            new Shape(0xFF1C1C22, Shapes.Rect(12, -1.5f, 26, 1.5f)),
        },
    };

    private static readonly PointF[] Shell =
        { new(63, 52), new(64, 38), new(71, 29), new(83, 26), new(94, 30), new(99, 40), new(99, 50), new(95, 60), new(68, 60) };
    private static readonly PointF[] Shade = { new(63, 52), new(64, 38), new(71, 29), new(73, 45), new(70, 58) };
    private static readonly PointF[] Crown = { new(71, 29), new(83, 26), new(94, 30), new(86, 36), new(74, 37) };
    private static readonly PointF[] Lens = { new(88, 39), new(98, 41), new(98, 47), new(92, 48), new(88, 45) };
    private static readonly PointF[] Jaw = { new(84, 50), new(92, 52), new(92, 60), new(84, 60) };
    private static readonly PointF[] Mouth = { new(92, 53), new(98, 52), new(96, 60), new(92, 59) };
    private static readonly PointF[] Tear = Shapes.Rect(90, 48, 92, 53);

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFFEDEDF1, Shell, body);
        g.Fill(0xFFD2D2DA, Shade, body);
        g.Fill(0xFFFFFFFF, Crown, body);
        g.Fill(0xFFC4C4CC, Jaw, body);
        g.Fill(0xFF16161A, Lens, body);
        g.Fill(0xFF16161A, Tear, body);
        g.Fill(0xFF2A2A30, Mouth, body);
    }
}
