namespace WindowSkater;

internal sealed class Knight : Character
{
    public override string Name => "Knight";
    public override Quirks Quirks => Quirks.Dust;
    public override float Speed => 0.9f;
    public override float BailChance => 0.07f;
    public override uint SparkColor => 0xFFD8D8E0;

    public override Look Look { get; } = new()
    {
        Torso = 0xFFB8BCC8, Plate = 0xFFD0D4E0, Thigh = 0xFF9EA3B2, Shin = 0xFF8A8F9E, Boot = 0xFF6E7384,
        Arm = 0xFFA8ACBA, ArmBack = 0xFF8A8F9E, Glove = 0xFF6E7384,
        Belt = 0xFF6B4A2E, Buckle = 0xFFD8B848, Shoulder = 0xFFD0D4E0,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF7A5030, Grip = 0xFF1C1814, Base = 0xFFB07C48,
        Wheel = 0xFF8E94A4, Hub = 0xFFD8B848, Truck = 0xFF8A8A94,
        Graphic = new[]
        {
            new Shape(0xFFC8CCD8, new PointF[] { new(-8, -9), new(8, -9), new(8, 2), new(0, 9), new(-8, 2) }),
            new Shape(0xFFC02828, Shapes.Rect(-1.8f, -7, 1.8f, 6)),
            new Shape(0xFFC02828, Shapes.Rect(-6, -4, 6, -0.5f)),
        },
    };

    private static readonly PointF[] Shield = { new(48, 64), new(68, 64), new(68, 86), new(58, 98), new(48, 86) };
    private static readonly PointF[] ShieldCrossV = Shapes.Rect(56, 68, 60, 92);
    private static readonly PointF[] ShieldCrossH = Shapes.Rect(50, 76, 66, 80);
    private static readonly PointF[] Tabard = { new(72, 84), new(88, 84), new(90, 106), new(70, 106) };

    private static readonly PointF[] Helm =
        { new(63, 52), new(64, 34), new(70, 27), new(88, 26), new(98, 32), new(100, 44), new(100, 60), new(66, 62) };
    private static readonly PointF[] HelmShade = { new(63, 52), new(64, 34), new(70, 27), new(74, 44), new(72, 60) };
    private static readonly PointF[] HelmTop = { new(70, 27), new(88, 26), new(98, 32), new(90, 36), new(74, 36) };
    private static readonly PointF[] Ridge = Shapes.Rect(94, 32, 96, 60);
    private static readonly PointF[] Slit = { new(86, 40), new(100, 41), new(100, 45), new(86, 44) };
    private static readonly PointF[] HoleA = Shapes.Rect(92, 50, 94, 52);
    private static readonly PointF[] HoleB = Shapes.Rect(95, 52, 97, 54);
    private static readonly PointF[] HoleC = Shapes.Rect(92, 54, 94, 56);
    private static readonly PointF[] PlumeMount = { new(74, 24), new(86, 24), new(84, 29), new(76, 29) };

    public override void DrawBack(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFF8E94A4, Shield, body);
        g.Fill(0xFFC02828, ShieldCrossV, body);
        g.Fill(0xFFC02828, ShieldCrossH, body);
    }

    public override void DrawTorso(Gfx g, in Xf body)
    {
        Rig.Torso(g, Look, body);
        g.Fill(0xFFC02828, Tabard, body);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        Rig.Ribbon(g, p, body, 0xFFC02828, 78f, 24f, 34f, 9f);
        g.Fill(0xFFD8B848, PlumeMount, body);
        g.Fill(0xFFB4B9C6, Helm, body);
        g.Fill(0xFF8E94A4, HelmShade, body);
        g.Fill(0xFFDDE1EA, HelmTop, body);
        g.Fill(0xFF9AA0B0, Ridge, body);
        g.Fill(0xFF14161A, Slit, body);
        g.Fill(0xFF14161A, HoleA, body);
        g.Fill(0xFF14161A, HoleB, body);
        g.Fill(0xFF14161A, HoleC, body);
    }
}
