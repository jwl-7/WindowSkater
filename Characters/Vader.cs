namespace WindowSkater;

internal sealed class Vader : Character
{
    public override string Name => "Darth Vader";
    public override Grind Signature => Grind.Darkslide;

    public override Look Look { get; } = new()
    {
        Torso = 0xFF1C1C22, Plate = 0xFF2E2E38, Thigh = 0xFF1C1C22, Shin = 0xFF131318, Boot = 0xFF2E2E38,
        Arm = 0xFF1C1C22, ArmBack = 0xFF131318, Glove = 0xFF2E2E38,
        Belt = 0xFF6C6C78, Buckle = 0xFFE8B030, Shoulder = 0xFF45454F,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF2A2A30, Grip = 0xFF151518, Base = 0xFF16161A,
        Wheel = 0xFFB02A2A, Hub = 0xFF200808, Truck = 0xFF70707C,
        Graphic = new[]
        {
            new Shape(0xFFE0302A, Shapes.Rect(-30, -2.2f, 20, 2.2f)),
            new Shape(0xFFFFD0C8, Shapes.Rect(-27, -0.8f, 17, 0.8f)),
            new Shape(0xFF9A9AA6, Shapes.Rect(20, -3, 30, 3)),
        },
    };

    private const uint Cape = 0xFF101014, CapeFacet = 0xFF1E1E26;
    private const uint Helmet = 0xFF18181E, HelmetLight = 0xFF2C2C36, HelmetFacet = 0xFF22222B;
    private const uint Mask = 0xFF4E4E5A, MaskDark = 0xFF2A2A32, Lens = 0xFF08080A;

    private static readonly PointF[] ButtonRed = Shapes.Rect(74, 70, 79, 75);
    private static readonly PointF[] ButtonBlue = Shapes.Rect(81, 70, 86, 75);
    private static readonly PointF[] ButtonGreen = Shapes.Rect(74, 78, 86, 81);

    private static readonly PointF[] Dome =
        { new(64, 50), new(66, 36), new(74, 28), new(86, 26), new(96, 32), new(99, 42), new(97, 52) };
    private static readonly PointF[] DomeLight = { new(74, 28), new(86, 26), new(96, 32), new(88, 37), new(76, 38) };
    private static readonly PointF[] DomeFacet = { new(66, 36), new(74, 28), new(76, 38), new(68, 46) };
    private static readonly PointF[] Skirt = { new(62, 50), new(99, 50), new(95, 60), new(66, 60) };
    private static readonly PointF[] Face = { new(87, 39), new(99, 43), new(100, 53), new(94, 60), new(86, 58), new(84, 46) };
    private static readonly PointF[] Grille = { new(90, 52), new(98, 52), new(95, 58), new(91, 57) };
    private static readonly PointF[] Eye = { new(89, 42), new(97, 44), new(96, 48), new(89, 47) };

    public override void DrawBack(Gfx g, in Pose p, in Xf body) => Rig.Cape(g, p, body, Cape, CapeFacet);

    public override void DrawTorso(Gfx g, in Xf body)
    {
        Rig.Torso(g, Look, body);
        g.Fill(0xFFC8322C, ButtonRed, body);
        g.Fill(0xFF3A70D8, ButtonBlue, body);
        g.Fill(0xFF3AB06A, ButtonGreen, body);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(Helmet, Skirt, body);
        g.Fill(Helmet, Dome, body);
        g.Fill(HelmetLight, DomeLight, body);
        g.Fill(HelmetFacet, DomeFacet, body);
        g.Fill(Mask, Face, body);
        g.Fill(MaskDark, Grille, body);
        g.Fill(Lens, Eye, body);
    }
}
