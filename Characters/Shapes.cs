using System;

namespace WindowSkater;

/// <summary>Small polygon builders for deck graphics.</summary>
internal static class Shapes
{
    public static PointF[] Rect(float x0, float y0, float x1, float y1) =>
        new PointF[] { new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1) };

    public static PointF[] Ngon(float cx, float cy, float radius, int sides, float rotation = 0f)
    {
        var pts = new PointF[sides];
        for (int i = 0; i < sides; i++)
        {
            float a = rotation + MathF.Tau * i / sides;
            pts[i] = new PointF(cx + MathF.Cos(a) * radius, cy + MathF.Sin(a) * radius);
        }
        return pts;
    }

    public static PointF[] Star(float cx, float cy, float outer, float inner, int points, float rotation = 0f)
    {
        var pts = new PointF[points * 2];
        for (int i = 0; i < pts.Length; i++)
        {
            float a = rotation + MathF.PI * i / points;
            float r = (i & 1) == 0 ? outer : inner;
            pts[i] = new PointF(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
        }
        return pts;
    }
}
