using System;

namespace WindowSkater;

internal sealed class BobaFett : Character
{
    public override string Name => "Boba Fett";
    public override Quirks Quirks => Quirks.Jetpack;
    public override float Speed => 1.05f;

    public override Look Look { get; } = new()
    {
        Torso = 0xFF5E6B58, Plate = 0xFF7A8A6E, Thigh = 0xFF6E7B66, Shin = 0xFF5A664F, Boot = 0xFF8A6A3A,
        Arm = 0xFF6E7B66, ArmBack = 0xFF5A664F, Glove = 0xFF3A3A34,
        Belt = 0xFF7A5A2E, Buckle = 0xFFB09040, Shoulder = 0xFF9E2E26,
    };

    public override BoardSpec Board { get; } = new()
    {
        Edge = 0xFF3F4A38, Grip = 0xFF16181A, Base = 0xFF6B7A64,
        Wheel = 0xFF7A5A2E, Hub = 0xFFC8A030, Truck = 0xFF70706A,
        Graphic = new[]
        {
            new Shape(0xFFB03028, Shapes.Rect(-30, -1.5f, 30, 1.5f)),
            new Shape(0xFF14161A, Shapes.Rect(-10, -8, 10, -4.5f)),
            new Shape(0xFF14161A, Shapes.Rect(-2.5f, -4.5f, 2.5f, 8)),
        },
    };

    private static readonly PointF[] Pack = Shapes.Rect(47, 62, 65, 100);
    private static readonly PointF[] NozzleA = { new(50, 100), new(56, 100), new(55, 106), new(51, 106) };
    private static readonly PointF[] NozzleB = { new(57, 100), new(63, 100), new(62, 106), new(58, 106) };
    private static readonly PointF[] ChestYellow = Shapes.Rect(72, 68, 78, 80);
    private static readonly PointF[] ChestRed = Shapes.Rect(80, 68, 86, 80);

    private static readonly PointF[] Antenna = { new(66, 40), new(60, 22), new(63, 21), new(69, 39) };
    private static readonly PointF[] Shell =
        { new(63, 52), new(64, 38), new(71, 29), new(83, 26), new(94, 30), new(99, 40), new(99, 52), new(94, 60), new(68, 60) };
    private static readonly PointF[] Shade = { new(63, 52), new(64, 38), new(71, 29), new(73, 45), new(70, 58) };
    private static readonly PointF[] Crown = { new(71, 29), new(83, 26), new(94, 30), new(86, 36), new(74, 37) };
    private static readonly PointF[] Stripe = { new(66, 34), new(74, 29), new(78, 32), new(70, 38) };
    private static readonly PointF[] Dent = { new(78, 44), new(83, 41), new(84, 47) };
    private static readonly PointF[] VisorBand = { new(88, 40), new(99, 42), new(99, 47), new(88, 46) };
    private static readonly PointF[] VisorSlit = { new(93, 46), new(97, 46), new(96, 57), new(93, 56) };

    public override void DrawBack(Gfx g, in Pose p, in Xf body)
    {
        Rig.Cape(g, p, body, 0xFF6A5A3A, 0xFF5A4A2E, 0.6f);
        g.Fill(0xFF8A8A76, Pack, body);
        g.Fill(0xFF55554A, NozzleA, body);
        g.Fill(0xFF55554A, NozzleB, body);
        if (!p.Thrust) return;

        float flicker = MathF.Sin(p.Time * 40f) * 3f;
        g.Fill(0xFFFF8A20, stackalloc PointF[]
        {
            body.Map(50, 106), body.Map(63, 106), body.Map(57, 124 + flicker),
        });
        g.Fill(0xFFFFE070, stackalloc PointF[]
        {
            body.Map(53, 106), body.Map(60, 106), body.Map(57, 116 + flicker),
        });
    }

    public override void DrawTorso(Gfx g, in Xf body)
    {
        Rig.Torso(g, Look, body);
        g.Fill(0xFFC8A030, ChestYellow, body);
        g.Fill(0xFFB03028, ChestRed, body);
    }

    public override void DrawHead(Gfx g, in Pose p, in Xf body)
    {
        g.Fill(0xFF8A8A90, Antenna, body);
        g.Fill(0xFF6B7A64, Shell, body);
        g.Fill(0xFF55624F, Shade, body);
        g.Fill(0xFF8A9A80, Crown, body);
        g.Fill(0xFFB03028, Stripe, body);
        g.Fill(0xFF55624F, Dent, body);
        g.Fill(0xFF14161A, VisorBand, body);
        g.Fill(0xFF14161A, VisorSlit, body);
    }
}
