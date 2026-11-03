using System;

namespace WindowSkater;

/// <summary>Colours and deck art for one skateboard. Graphic shapes live on the underside of the deck.</summary>
internal sealed class BoardSpec
{
    public uint Edge, Grip, Base, Wheel, Hub, Truck;
    public Shape[] Graphic = Array.Empty<Shape>();
}

/// <summary>
/// A tiny 3D skateboard: roll flips it about its length, yaw spins it, pitch tips it in the side view.
/// Visible faces are picked by back-face culling and projected orthographically into art space.
/// </summary>
internal readonly struct Board3D
{
    private const float CenterX = 80f, CenterY = 133f;
    private const float Thickness = 5f, WheelX = 26f, WheelY = 9f, WheelZ = -12f, WheelRadius = 5f;
    public const float FlippedFootZ = -17f;

    private static readonly float[] Xs = { -46f, -40f, -34f, 34f, 40f, 46f };
    private static readonly float[] HalfWidth = { 7f, 11f, 11f, 11f, 11f, 7f };
    private static readonly float[] Kick = { 7.2f, 3.6f, 0f, 0f, 3.6f, 7.2f };

    private readonly float cosRoll, sinRoll, cosYaw, sinYaw, cosPitch, sinPitch, drop;

    public Board3D(in Pose p)
    {
        cosRoll = MathF.Cos(p.Roll); sinRoll = MathF.Sin(p.Roll);
        cosYaw = MathF.Cos(p.Yaw); sinYaw = MathF.Sin(p.Yaw);
        cosPitch = MathF.Cos(p.Pitch); sinPitch = MathF.Sin(p.Pitch);
        drop = p.Drop;
    }

    /// <summary>True once the board is upside down, so feet rest on the underside.</summary>
    public bool Inverted => cosRoll < 0f;

    /// <summary>Board space (x along the length, y across, z up from the deck surface) to art space.</summary>
    public PointF Project(float x, float y, float z)
    {
        float y1 = y * cosRoll - z * sinRoll;
        float z1 = y * sinRoll + z * cosRoll;
        float x2 = x * cosYaw - y1 * sinYaw;
        float sy = -z1;
        return new PointF(
            CenterX + x2 * cosPitch - sy * sinPitch,
            CenterY + drop + x2 * sinPitch + sy * cosPitch);
    }

    public PointF FootPoint(float x) => Project(x, 0f, Inverted ? FlippedFootZ : 0f);

    public void Draw(Gfx g, BoardSpec spec)
    {
        // corner k at station i: 0 = top +y, 1 = top -y, 2 = bottom +y, 3 = bottom -y
        Span<PointF> c = stackalloc PointF[Xs.Length * 4];
        for (int i = 0; i < Xs.Length; i++)
        {
            float x = Xs[i], hw = HalfWidth[i], top = Kick[i], bottom = top - Thickness;
            c[i * 4] = Project(x, hw, top);
            c[i * 4 + 1] = Project(x, -hw, top);
            c[i * 4 + 2] = Project(x, hw, bottom);
            c[i * 4 + 3] = Project(x, -hw, bottom);
        }

        // Camera looks along +y, so a face is visible when its rotated normal has y < 0.
        float topFacing = sinRoll * cosYaw;
        float sideFacing = cosRoll * cosYaw;
        bool topVisible = topFacing > 0.001f, bottomVisible = topFacing < -0.001f;

        if (!bottomVisible) DrawWheels(g, spec);

        if (bottomVisible)
        {
            Strip(g, c, 2, 3, spec.Base);
            DrawGraphic(g, spec);
        }
        if (topVisible) Strip(g, c, 0, 1, spec.Grip);
        if (sideFacing < -0.001f) Strip(g, c, 0, 2, spec.Edge);   // +y side
        if (sideFacing > 0.001f) Strip(g, c, 1, 3, spec.Edge);    // -y side
        if (sinYaw < -0.001f) Cap(g, c, Xs.Length - 1, spec.Edge); // nose
        if (sinYaw > 0.001f) Cap(g, c, 0, spec.Edge);              // tail

        if (bottomVisible) DrawWheels(g, spec);
    }

    /// <summary>Fills the quad strip running along the deck between corner rows a and b.</summary>
    private static void Strip(Gfx g, ReadOnlySpan<PointF> c, int a, int b, uint color)
    {
        Span<PointF> poly = stackalloc PointF[Xs.Length * 2];
        for (int i = 0; i < Xs.Length; i++)
        {
            poly[i] = c[i * 4 + a];
            poly[poly.Length - 1 - i] = c[i * 4 + b];
        }
        g.Fill(color, poly);
    }

    private static void Cap(Gfx g, ReadOnlySpan<PointF> c, int station, uint color)
    {
        int i = station * 4;
        g.Fill(color, stackalloc PointF[] { c[i], c[i + 1], c[i + 3], c[i + 2] });
    }

    private void DrawGraphic(Gfx g, BoardSpec spec)
    {
        Span<PointF> poly = stackalloc PointF[16];
        foreach (Shape s in spec.Graphic)
        {
            for (int i = 0; i < s.Points.Length; i++)
                poly[i] = Project(s.Points[i].X, s.Points[i].Y, -Thickness - 0.1f);
            g.Fill(s.Color, poly[..s.Points.Length], rim: false);
        }
    }

    private void DrawWheels(Gfx g, BoardSpec spec)
    {
        Span<PointF> truck = stackalloc PointF[4];
        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * WheelX;
            truck[0] = Project(x - 3f, 0f, -Thickness);
            truck[1] = Project(x + 3f, 0f, -Thickness);
            truck[2] = Project(x + 2f, 0f, WheelZ);
            truck[3] = Project(x - 2f, 0f, WheelZ);
            g.Fill(spec.Truck, truck);

            PointF near = Project(x, -WheelY, WheelZ), far = Project(x, WheelY, WheelZ);
            if (MathF.Abs(near.X - far.X) + MathF.Abs(near.Y - far.Y) > 0.5f) DrawWheel(g, spec, far);
            DrawWheel(g, spec, near);
        }
    }

    private static void DrawWheel(Gfx g, BoardSpec spec, PointF center)
    {
        g.Circle(spec.Wheel, center, WheelRadius);
        g.Circle(spec.Hub, center, 2f);
    }
}
