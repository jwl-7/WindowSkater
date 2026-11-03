namespace WindowSkater;

internal sealed class Ninja : Character
{
    public override string Name => "Ninja";
    public override Grind Signature => Grind.Boardslide;
    public override Quirks Quirks => Quirks.Smoke;
    public override float Speed => 1.2f;

    public override Look Look { get; } = new()
    {
        Torso = 0xFF1A1C28, Plate = 0xFF2A2E40, Thigh = 0xFF1A1C28, Shin = 0xFF12141E, Boot = 0xFF2A2E40,
        Arm = 0xFF1A1C28, ArmBack = 0xFF12141E, Glove = 0xFF2A2E40,
        Belt = 0xFFB02828, Buckle = 0xFF888890, Shoulder = 0xFF2A2E40,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF14161E, Grip = 0xFF0E0F14, Base = 0xFF1E2230,
        Wheel = 0xFFB02828, Hub = 0xFF14161E, Truck = 0xFF5A5E70,
        Graphic = new[]
        {
            new Shape(0xFFB4BAC8, Shapes.Star(0, 0, 9, 3, 4, 0.39f)),
            new Shape(0xFF1E2230, Shapes.Ngon(0, 0, 1.6f, 8)),
        },
    };

    private static readonly PointF[] Blade = { new(58, 98), new(60, 100), new(94, 52), new(92, 50) };
    private static readonly PointF[] Handle = { new(94, 52), new(92, 50), new(98, 44), new(100, 46) };
    private static readonly PointF[] Guard = { new(91, 53), new(95, 49), new(96, 50), new(92, 54) };

    private static readonly PointF[] Hood =
        { new(63, 52), new(64, 38), new(71, 29), new(83, 26), new(94, 30), new(99, 40), new(99, 52), new(94, 62), new(68, 62) };
    private static readonly PointF[] HoodShade = { new(63, 52), new(64, 38), new(71, 29), new(73, 45), new(70, 60) };
    private static readonly PointF[] HoodTop = { new(71, 29), new(83, 26), new(94, 30), new(86, 36), new(74, 37) };
    private static readonly PointF[] Eyes = { new(86, 40), new(99, 41), new(99, 48), new(86, 47) };
    private static readonly PointF[] EyeWhite = { new(91, 42), new(98, 42), new(98, 46), new(91, 45) };
    private static readonly PointF[] Pupil = Shapes.Rect(95, 42, 98, 46);
    private static readonly PointF[] Brow = { new(88, 39), new(99, 40), new(99, 41.5f), new(88, 41) };
    private static readonly PointF[] Headband = { new(63, 35), new(99, 37), new(99, 40), new(63, 38) };
    private static readonly PointF[] HeadbandPlate = { new(84, 34), new(92, 35), new(92, 40), new(84, 39) };

    public override void DrawBack(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFFC8CCD8, Blade, body);
        g.Fill(0xFF3A2A20, Handle, body);
        g.Fill(0xFFD0B040, Guard, body);
        Rig.Ribbon(g, p, body, 0xFFC82828, 70f, 62f, 60f, 9f);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        Rig.Ribbon(g, p, body, 0xFFB02828, 64f, 38f, 34f, 5f);
        g.Fill(0xFF1A1C28, Hood, body);
        g.Fill(0xFF12141E, HoodShade, body);
        g.Fill(0xFF2A2E40, HoodTop, body);
        g.Fill(0xFFF0CFA8, Eyes, body);
        g.Fill(0xFFFFFFFF, EyeWhite, body);
        g.Fill(0xFF14161A, Pupil, body);
        g.Fill(0xFF14161A, Brow, body);
        g.Fill(0xFFB02828, Headband, body);
        g.Fill(0xFFA0A8B8, HeadbandPlate, body);
    }
}
