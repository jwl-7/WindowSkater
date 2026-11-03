using System;
using System.Runtime.InteropServices;

namespace WindowSkater;

[StructLayout(LayoutKind.Sequential)]
internal struct PointF
{
    public float X, Y;
    public PointF(float x, float y) { X = x; Y = y; }
}

/// <summary>A filled polygon in the deck's own (u along the length, v across the width) plane.</summary>
internal readonly struct Shape
{
    public readonly uint Color;
    public readonly PointF[] Points;
    public Shape(uint color, PointF[] points) { Color = color; Points = points; }
}

/// <summary>Rotate about a pivot, then translate. Positive angles tip +x downwards (screen y points down).</summary>
internal readonly struct Xf
{
    private readonly float px, py, cos, sin, tx, ty;

    public Xf(float px, float py, float angle, float tx, float ty)
    {
        this.px = px; this.py = py; this.tx = tx; this.ty = ty;
        cos = MathF.Cos(angle); sin = MathF.Sin(angle);
    }

    public PointF Map(float x, float y)
    {
        float dx = x - px, dy = y - py;
        return new PointF(dx * cos - dy * sin + px + tx, dx * sin + dy * cos + py + ty);
    }
}

/// <summary>
/// GDI+ drawing onto a 32bpp premultiplied bitmap that shares memory with our DIB.
/// Art is authored in a 160x160 space facing right and placed inside a larger sprite.
/// </summary>
internal sealed unsafe class Gfx : IDisposable
{
    public const float OffsetX = 16f, OffsetY = 8f;
    private const uint RimColor = 0x40FFFFFF;
    private const int MaxPoints = 32;

    private readonly nint bitmap, graphics, brush, rimPen, linePen;
    private readonly PointF[] work = new PointF[MaxPoints];
    private readonly float scale;

    /// <summary>Mirror the art horizontally (skater facing left).</summary>
    public bool Flip;

    public Gfx(int width, int height, nint bits, float scale)
    {
        this.scale = scale;
        Native.GdipCreateBitmapFromScan0(width, height, width * 4, Win32.PixelFormat32bppPARGB, bits, out bitmap);
        Native.GdipGetImageGraphicsContext(bitmap, out graphics);
        Native.GdipSetSmoothingMode(graphics, Win32.SmoothingModeAntiAlias);
        Native.GdipCreateSolidFill(0, out brush);
        Native.GdipCreatePen1(RimColor, MathF.Max(1f, 0.9f * scale), Win32.UnitPixel, out rimPen);
        Native.GdipCreatePen1(0, 1f, Win32.UnitPixel, out linePen);
    }

    public void Clear() => Native.GdipGraphicsClear(graphics, 0);

    /// <summary>Fills a polygon given in art space (applies mirroring, offset and scale only).</summary>
    public void Fill(uint color, ReadOnlySpan<PointF> points, bool rim = true)
    {
        for (int i = 0; i < points.Length; i++)
        {
            float x = Flip ? 160f - points[i].X : points[i].X;
            work[i] = new PointF((x + OffsetX) * scale, (points[i].Y + OffsetY) * scale);
        }
        Native.GdipSetSolidFillColor(brush, color);
        fixed (PointF* p = work)
        {
            Native.GdipFillPolygon(graphics, brush, p, points.Length, 0);
            if (rim && IsDark(color)) Native.GdipDrawPolygon(graphics, rimPen, p, points.Length);
        }
    }

    /// <summary>The rim is a translucent highlight, so it only shows (and is only worth drawing) on darker fills.</summary>
    private static bool IsDark(uint color) => ((color >> 16) & 0xFF) + ((color >> 8) & 0xFF) + (color & 0xFF) < 510;

    /// <summary>Fills a static polygon after mapping it through a body-group transform.</summary>
    public void Fill(uint color, PointF[] points, in Xf xf)
    {
        Span<PointF> mapped = stackalloc PointF[points.Length];
        for (int i = 0; i < points.Length; i++) mapped[i] = xf.Map(points[i].X, points[i].Y);
        Fill(color, mapped);
    }

    public void Circle(uint color, PointF center, float radius)
    {
        float x = Flip ? 160f - center.X : center.X;
        Native.GdipSetSolidFillColor(brush, color);
        Native.GdipFillEllipse(graphics, brush,
            (x - radius + OffsetX) * scale, (center.Y - radius + OffsetY) * scale,
            2f * radius * scale, 2f * radius * scale);
    }

    /// <summary>Line in sprite pixels (particles).</summary>
    public void Line(uint color, float x1, float y1, float x2, float y2, float width)
    {
        Native.GdipSetPenColor(linePen, color);
        Native.GdipSetPenWidth(linePen, width);
        Native.GdipDrawLine(graphics, linePen, x1, y1, x2, y2);
    }

    public void Dispose()
    {
        Native.GdipDeletePen(linePen);
        Native.GdipDeletePen(rimPen);
        Native.GdipDeleteBrush(brush);
        Native.GdipDeleteGraphics(graphics);
        Native.GdipDisposeImage(bitmap);
    }
}
