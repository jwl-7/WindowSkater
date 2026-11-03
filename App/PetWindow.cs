using System;
using System.Runtime.InteropServices;

namespace WindowSkater;

/// <summary>
/// The sprite window: layered (per-pixel alpha, clicks pass through empty pixels), always on top,
/// with no taskbar button and no focus stealing.
/// </summary>
internal sealed unsafe class PetWindow : IDisposable
{
    private const string ClassName = "WindowSkaterPet";
    private const float SpriteWidth = 192f, SpriteHeight = 176f;   // size in DIPs
    private const float AnchorX = 96f, AnchorY = 158f;             // ground contact point inside the sprite

    private int width, height;
    private nint memoryDC, dib;
    private Gfx gfx = null!;
    private float scale;

    public nint Hwnd { get; private set; }

    public PetWindow(float scale) => this.scale = scale;

    public void Create()
    {
        nint instance = Native.GetModuleHandle(0);
        var windowClass = new WNDCLASSEXW
        {
            cbSize = (uint)sizeof(WNDCLASSEXW),
            lpfnWndProc = (nint)(delegate* unmanaged<nint, uint, nuint, nint, nint>)&WindowProc,
            hInstance = instance,
            hCursor = Native.LoadCursor(0, Win32.IDC_ARROW),
            lpszClassName = Marshal.StringToHGlobalUni(ClassName),
        };
        Native.RegisterClassEx(ref windowClass);

        uint exStyle = Win32.WS_EX_LAYERED | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST | Win32.WS_EX_NOACTIVATE;
        Hwnd = Native.CreateWindowEx(exStyle, ClassName, "Window Skater", Win32.WS_POPUP, 0, 0, 1, 1, 0, 0, instance, 0);
        AllocateSurface();
    }

    /// <summary>Rebuilds the drawing surface for a monitor with a different DPI.</summary>
    public void Resize(float newScale)
    {
        scale = newScale;
        FreeSurface();
        AllocateSurface();
    }

    public void Show() => Native.ShowWindow(Hwnd, Win32.SW_SHOWNOACTIVATE);
    public void Hide() => Native.ShowWindow(Hwnd, Win32.SW_HIDE);

    public void KeepOnTop() =>
        Native.SetWindowPos(Hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | Win32.SWP_NOACTIVATE);

    public void Render(Skater skater)
    {
        int x = (int)MathF.Round(skater.X - AnchorX * scale);
        int y = (int)MathF.Round(skater.Y - AnchorY * scale);

        gfx.Clear();
        Rig.Draw(gfx, skater.Character, skater.GetPose());
        DrawParticles(skater.Particles, x, y);

        POINT destination = new() { X = x, Y = y };
        POINT source = default;
        SIZE size = new() { CX = width, CY = height };
        BLENDFUNCTION blend = new() { SourceConstantAlpha = 255, AlphaFormat = Win32.AC_SRC_ALPHA };
        Native.UpdateLayeredWindow(Hwnd, 0, &destination, &size, memoryDC, &source, 0, &blend, Win32.ULW_ALPHA);
    }

    public void Dispose()
    {
        FreeSurface();
        Native.DestroyWindow(Hwnd);
    }

    private void DrawParticles(Particle[] particles, int originX, int originY)
    {
        foreach (ref readonly Particle p in particles.AsSpan())
        {
            if (p.Life <= 0f) continue;
            uint alpha = (uint)(255f * MathF.Min(1f, p.Life / p.Max * 1.5f));
            uint color = (alpha << 24) | (p.Rgb & 0x00FFFFFF);
            float x = p.X - originX, y = p.Y - originY;
            gfx.Line(color, x, y, x - p.VX * 0.03f, y - p.VY * 0.03f, MathF.Max(1.5f, p.Width));
        }
    }

    private void AllocateSurface()
    {
        width = (int)MathF.Ceiling(SpriteWidth * scale);
        height = (int)MathF.Ceiling(SpriteHeight * scale);

        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER), biWidth = width, biHeight = -height,
            biPlanes = 1, biBitCount = 32,
        };
        nint screen = Native.GetDC(0);
        memoryDC = Native.CreateCompatibleDC(screen);
        dib = Native.CreateDIBSection(screen, ref header, 0, out nint bits, 0, 0);
        Native.SelectObject(memoryDC, dib);
        Native.ReleaseDC(0, screen);
        gfx = new Gfx(width, height, bits, scale);
    }

    private void FreeSurface()
    {
        gfx.Dispose();
        Native.DeleteObject(dib);
        Native.DeleteDC(memoryDC);
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint hwnd, uint message, nuint wParam, nint lParam)
    {
        if (message != 0 && message == Program.TaskbarCreated)
        {
            TrayIcon.Add(hwnd);
            return 0;
        }

        switch (message)
        {
            case Win32.WM_MOUSEACTIVATE:
                return Win32.MA_NOACTIVATE;

            case Win32.WM_LBUTTONDOWN:
                Native.SetCapture(hwnd);
                Program.Skater.BeginDrag();
                return 0;

            case Win32.WM_LBUTTONUP:
                Program.Skater.EndDrag();
                Native.ReleaseCapture();
                return 0;

            case Win32.WM_CAPTURECHANGED:
                Program.Skater.EndDrag();
                return 0;

            case Win32.WM_RBUTTONUP:
                TrayIcon.ShowMenu(hwnd);
                return 0;

            case Win32.WM_TRAY:
                uint mouse = (uint)lParam;
                if (mouse is Win32.WM_RBUTTONUP or Win32.WM_CONTEXTMENU) TrayIcon.ShowMenu(hwnd);
                else if (mouse == Win32.WM_LBUTTONDBLCLK) Program.TogglePause();
                return 0;

            case Win32.WM_DESTROY:
                Native.PostQuitMessage(0);
                return 0;
        }
        return Native.DefWindowProc(hwnd, message, wParam, lParam);
    }
}
