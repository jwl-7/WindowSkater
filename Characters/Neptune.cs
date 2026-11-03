namespace WindowSkater;

internal sealed class Neptune : Character
{
    public override string Name => "Neptune";
    public override Grind Signature => Grind.Boardslide;
    public override Quirks Quirks => Quirks.PeaceSign;
    public override uint SparkColor => 0xFFFFA0D8;

    public override Look Look { get; } = new()
    {
        Torso = 0xFFF2F2F8, Plate = 0xFF7A5CC8, Thigh = 0xFFF4D6C0, Shin = 0xFFF4D6C0, Boot = 0xFF6A4FB0,
        Arm = 0xFFF2F2F8, ArmBack = 0xFFE0E0EC, Glove = 0xFFF4D6C0,
        Belt = 0xFF7A5CC8, Buckle = 0xFFF0C0E0, Shoulder = 0xFF7A5CC8,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF5A3F9A, Grip = 0xFF1E1A2A, Base = 0xFF8A66D8,
        Wheel = 0xFFF2F2F8, Hub = 0xFFC9A8F2, Truck = 0xFF9A9AB0,
        Graphic = new[]
        {
            new Shape(0xFFFFFFFF, new PointF[]
            {
                new(-3, -9), new(3, -9), new(3, -3), new(9, -3), new(9, 3), new(3, 3),
                new(3, 9), new(-3, 9), new(-3, 3), new(-9, 3), new(-9, -3), new(-3, -3),
            }),
            new Shape(0xFFF6A0C8, Shapes.Ngon(0, 0, 2.2f, 8)),
        },
    };

    private static readonly PointF[] Hood = { new(62, 60), new(78, 58), new(76, 74), new(60, 76) };
    private static readonly PointF[] HoodLining = { new(64, 62), new(74, 61), new(73, 70), new(63, 72) };
    private static readonly PointF[] Collar = { new(70, 64), new(90, 64), new(88, 72), new(72, 72) };
    private static readonly PointF[] Skirt = { new(66, 94), new(94, 94), new(98, 106), new(62, 106) };
    private static readonly PointF[] SkirtHem = Shapes.Rect(62, 104, 98, 107);

    private static readonly PointF[] HairBack =
    {
        new(58, 48), new(60, 30), new(70, 21), new(84, 19), new(95, 24), new(100, 36),
        new(92, 34), new(84, 30), new(76, 33), new(70, 42), new(72, 66), new(60, 68),
    };
    private static readonly PointF[] Face =
        { new(68, 38), new(94, 36), new(99, 46), new(97, 57), new(86, 62), new(73, 59), new(67, 49) };
    private static readonly PointF[] Bangs =
        { new(70, 34), new(84, 28), new(98, 36), new(99, 41), new(92, 38), new(88, 44), new(84, 38), new(78, 45), new(74, 38) };
    private static readonly PointF[] Ahoge = { new(84, 28), new(86, 18), new(90, 24) };
    private static readonly PointF[] Eye = { new(88, 44), new(95, 43), new(96, 51), new(89, 52) };
    private static readonly PointF[] EyeShine = Shapes.Rect(90, 45, 93, 48);
    private static readonly PointF[] Blush = { new(86, 54), new(92, 54), new(91, 57), new(86, 57) };
    private static readonly PointF[] Mouth = { new(94, 57), new(97, 56), new(96, 59) };
    private static readonly PointF[] ClipHorizontal = Shapes.Rect(72, 34, 80, 38);
    private static readonly PointF[] ClipVertical = Shapes.Rect(74, 32, 78, 40);

    public override void DrawBack(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFFEDEDF6, Hood, body);
        g.Fill(0xFF7A5CC8, HoodLining, body);
    }

    public override void DrawTorso(Gfx g, in Xf body)
    {
        Rig.Torso(g, Look, body);
        g.Fill(0xFF7A5CC8, Collar, body);
        g.Fill(0xFFF2F2F8, Skirt, body);
        g.Fill(0xFF7A5CC8, SkirtHem, body);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFFB794E8, HairBack, body);
        g.Fill(0xFFF8DCC8, Face, body);
        g.Fill(0xFFC9A8F2, Bangs, body);
        g.Fill(0xFFC9A8F2, Ahoge, body);
        g.Fill(0xFF3C3CA8, Eye, body);
        g.Fill(0xFFFFFFFF, EyeShine, body);
        g.Fill(0xFFF6A0B8, Blush, body);
        g.Fill(0xFFC05070, Mouth, body);
        g.Fill(0xFFFFFFFF, ClipHorizontal, body);
        g.Fill(0xFFFFFFFF, ClipVertical, body);
    }
}
