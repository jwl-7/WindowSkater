using System;

namespace WindowSkater;

/// <summary>The skeleton every character shares: torso, legs, arms and cloth, assembled around the board.</summary>
internal static class Rig
{
    private const float HipY = 100f, BackFootX = -20f, FrontFootX = 20f;

    private static readonly PointF[] TorsoOutline =
        { new(64, 62), new(96, 62), new(92, 80), new(89, 100), new(71, 100), new(68, 80) };
    private static readonly PointF[] Plate = { new(71, 66), new(89, 66), new(88, 86), new(72, 86) };
    private static readonly PointF[] BeltBar = { new(70, 92), new(90, 92), new(90, 98), new(70, 98) };
    private static readonly PointF[] BuckleBox = { new(78, 93), new(82, 93), new(82, 97), new(78, 97) };
    private static readonly PointF[] ShoulderBack = { new(62, 60), new(72, 58), new(73, 68), new(63, 70) };
    private static readonly PointF[] ShoulderFront = { new(88, 58), new(98, 60), new(97, 70), new(87, 68) };

    public static Xf BodyTransform(in Pose p) => new(80f, HipY, p.Lean, 0f, 16f * p.Crouch);

    public static void Draw(Gfx g, Character c, in Pose p)
    {
        g.Flip = p.Dir < 0;
        Xf body = BodyTransform(p);
        var board = new Board3D(p);
        Look look = c.Look;

        c.DrawBack(g, p, body);
        Arm(g, look, p, body, back: true);
        Leg(g, look, p, body, board, hipX: 73f, footX: BackFootX, width: 0.8f);
        board.Draw(g, c.Board);
        Leg(g, look, p, body, board, hipX: 87f, footX: FrontFootX, width: 1f);
        c.DrawTorso(g, body);
        c.DrawHead(g, p, body);
        c.DrawFront(g, p, body);
        Arm(g, look, p, body, back: false);
    }

    /// <summary>Base torso: body, chest plate, belt and shoulder pads in the character's colours.</summary>
    public static void Torso(Gfx g, in Look look, in Xf body)
    {
        g.Fill(look.Torso, TorsoOutline, body);
        g.Fill(look.Plate, Plate, body);
        g.Fill(look.Belt, BeltBar, body);
        g.Fill(look.Buckle, BuckleBox, body);
        g.Fill(look.Shoulder, ShoulderBack, body);
        g.Fill(look.Shoulder, ShoulderFront, body);
    }

    private static void Leg(Gfx g, in Look look, in Pose p, in Xf body, in Board3D board, float hipX, float footX, float width)
    {
        PointF hip = body.Map(hipX, HipY);
        PointF onBoard = board.FootPoint(footX);
        var tucked = new PointF(hip.X + (footX < 0f ? -4f : 6f), hip.Y + 16f);
        var foot = new PointF(onBoard.X + (tucked.X - onBoard.X) * p.Tuck, onBoard.Y + (tucked.Y - onBoard.Y) * p.Tuck);

        var knee = new PointF((hip.X + foot.X) / 2f + 5f + 9f * p.Crouch + 6f * p.Tuck, (hip.Y + foot.Y) / 2f);
        Limb(g, look.Thigh, hip, knee, 11f * width);
        Limb(g, look.Shin, knee, foot, 9f * width);
        g.Fill(look.Boot, stackalloc PointF[]
        {
            new(foot.X - 7f, foot.Y - 7f), new(foot.X + 9f, foot.Y - 7f),
            new(foot.X + 10f, foot.Y), new(foot.X - 7f, foot.Y),
        });
    }

    private static void Arm(Gfx g, in Look look, in Pose p, in Xf body, bool back)
    {
        float spread = p.ArmSpread;
        float up = back ? p.ArmUp : MathF.Max(p.ArmUp, p.FrontArmUp);
        float sway = MathF.Sin(p.Time * 5f + (back ? 0f : 1.5f)) * 3f;

        float lowX = back ? 50f - 10f * spread : 112f + 10f * spread;
        float lowY = (back ? 98f : 94f) - 8f * spread;
        float hx = lowX + ((back ? 50f : 112f) - lowX) * up;
        float hy = lowY + ((back ? 58f : 54f) - lowY) * up + sway;

        PointF shoulder = body.Map(back ? 70f : 90f, 66f);
        PointF hand = body.Map(hx, hy);
        var elbow = new PointF((shoulder.X + hand.X) / 2f, (shoulder.Y + hand.Y) / 2f + 9f * (1f - up));

        uint sleeve = back ? look.ArmBack : look.Arm;
        Limb(g, sleeve, shoulder, elbow, 8f);
        Limb(g, sleeve, elbow, hand, 7f);
        g.Fill(look.Glove, stackalloc PointF[]
        {
            new(hand.X - 4f, hand.Y - 4f), new(hand.X + 4f, hand.Y - 4f),
            new(hand.X + 4f, hand.Y + 4f), new(hand.X - 4f, hand.Y + 4f),
        });
    }

    /// <summary>A flat quad of the given width running from a to b.</summary>
    public static void Limb(Gfx g, uint color, PointF a, PointF b, float width)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 0.01f) return;
        float nx = -dy / length * width / 2f, ny = dx / length * width / 2f;
        g.Fill(color, stackalloc PointF[]
        {
            new(a.X + nx, a.Y + ny), new(a.X - nx, a.Y - ny),
            new(b.X - nx, b.Y - ny), new(b.X + nx, b.Y + ny),
        });
    }

    /// <summary>A cape hanging from the shoulders that streams behind the skater as speed rises.</summary>
    public static void Cape(Gfx g, in Pose p, in Xf body, uint color, uint facet, float length = 1f)
    {
        float f = p.CapeFlow, t = p.Time;
        float w1 = MathF.Sin(t * 9f), w2 = MathF.Sin(t * 7f + 1.3f), w3 = MathF.Sin(t * 11f + 2.1f);
        Span<PointF> q = stackalloc PointF[6];
        q[0] = body.Map(74f, 62f);
        q[1] = body.Map(92f, 64f);
        q[2] = body.Map(82f - 22f * f, 62f + (34f + w1 * 3f) * length);
        q[3] = body.Map(62f - 34f * f, 62f + (62f + w2 * 4f) * length);
        q[4] = body.Map(52f - 22f * f, 62f + (50f + w3 * 3f) * length);
        q[5] = body.Map(58f - 6f * f, 62f + 20f * length);
        g.Fill(color, q);
        g.Fill(facet, stackalloc PointF[] { q[0], q[2], q[3] });
    }

    /// <summary>A ribbon tied at the given body-space point that streams backwards.</summary>
    public static void Ribbon(Gfx g, in Pose p, in Xf body, uint color, float x, float y, float length, float width)
    {
        float f = 0.35f + 0.65f * p.CapeFlow;
        float wave = MathF.Sin(p.Time * 10f), wave2 = MathF.Sin(p.Time * 8f + 1.7f);
        float reach = length * f;
        g.Fill(color, stackalloc PointF[]
        {
            body.Map(x, y - width / 2f), body.Map(x - reach * 0.5f, y - width / 2f + wave * 3f),
            body.Map(x - reach, y + wave2 * 5f), body.Map(x - reach * 0.55f, y + width / 2f + wave * 3f),
            body.Map(x, y + width / 2f),
        });
    }
}
