namespace WindowSkater;

internal sealed class MasterChief : Character
{
    public override string Name => "Master Chief";
    public override Grind Signature => Grind.Boardslide;
    public override uint SparkColor => 0xFFE9D060;

    public override Look Look { get; } = new()
    {
        Torso = 0xFF5F6F3E, Plate = 0xFF74854C, Thigh = 0xFF5F6F3E, Shin = 0xFF4A5830, Boot = 0xFF3A4228,
        Arm = 0xFF5F6F3E, ArmBack = 0xFF4A5830, Glove = 0xFF3A3A3A,
        Belt = 0xFF2A2A2A, Buckle = 0xFF8A8A8A, Shoulder = 0xFF7A8C50,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF3A4428, Grip = 0xFF181A12, Base = 0xFF5A6A3A,
        Wheel = 0xFF2A2E22, Hub = 0xFFE9B947, Truck = 0xFF70746A,
        Graphic = new[]
        {
            new Shape(0xFFE9B947, new PointF[] { new(-20, -8), new(-8, 0), new(-20, 8), new(-14, 8), new(-2, 0), new(-14, -8) }),
            new Shape(0xFFE9B947, new PointF[] { new(0, -8), new(12, 0), new(0, 8), new(6, 8), new(18, 0), new(6, -8) }),
        },
    };

    private static readonly PointF[] Dome =
        { new(62, 52), new(63, 36), new(70, 27), new(83, 24), new(95, 28), new(100, 38), new(100, 52), new(96, 61), new(67, 61) };
    private static readonly PointF[] Shade = { new(62, 52), new(63, 36), new(70, 27), new(72, 44), new(69, 58) };
    private static readonly PointF[] Crown = { new(70, 27), new(83, 24), new(95, 28), new(87, 34), new(73, 35) };
    private static readonly PointF[] Visor = { new(86, 36), new(99, 38), new(100, 49), new(94, 53), new(86, 50) };
    private static readonly PointF[] Glare = { new(88, 38), new(98, 40), new(97, 43), new(88, 42) };
    private static readonly PointF[] Chin = { new(84, 50), new(96, 54), new(94, 61), new(84, 60) };
    private static readonly PointF[] ChinVents = Shapes.Rect(89, 56, 94, 58);

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFF5E6E3C, Dome, body);
        g.Fill(0xFF46542C, Shade, body);
        g.Fill(0xFF7C8E52, Crown, body);
        g.Fill(0xFF46542C, Chin, body);
        g.Fill(0xFF2A3320, ChinVents, body);
        g.Fill(0xFFE9B947, Visor, body);
        g.Fill(0xFFFFE99A, Glare, body);
    }
}
