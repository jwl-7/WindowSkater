using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WindowSkater;

/// <summary>A horizontal surface to stand on. Hwnd 0 is the screen floor.</summary>
internal struct Ledge
{
    public nint Hwnd;
    public int Left, Right, Y;

    /// <summary>Top-left of the owning window when scanned, so the ledge can be moved to the window's live position.</summary>
    public int WinLeft, WinTop;

    public readonly bool Contains(float x) => x >= Left && x <= Right;

    public readonly Ledge Shifted(int dx, int dy) =>
        new() { Hwnd = Hwnd, Left = Left + dx, Right = Right + dx, Y = Y + dy, WinLeft = WinLeft + dx, WinTop = WinTop + dy };

    /// <summary>The same ledge at its window's current position (floors never move).</summary>
    public readonly Ledge Live()
    {
        if (Hwnd == 0 || !WindowScanner.TryGetRect(Hwnd, out RECT r)) return this;
        return Shifted(r.Left - WinLeft, r.Top - WinTop);
    }
}

/// <summary>
/// Finds the visible top edges of open windows (minus the parts hidden behind windows above them)
/// plus one floor ledge per monitor. Rescans are driven by window events, with a slow safety net.
/// </summary>
internal static unsafe class WindowScanner
{
    private const double DebounceSeconds = 0.1, SafetySeconds = 1.0;

    public static List<Ledge> Ledges { get; private set; } = new();
    public static int Version { get; private set; }
    public static int MinX { get; private set; }
    public static int MaxX { get; private set; }
    public static int MaxBottom { get; private set; }
    public static int MaxHeight { get; private set; }
    public static RECT Home { get; private set; }

    public static nint Self { get; set; }
    public static float Scale { get; set; } = 1f;

    private struct Candidate { public nint Hwnd; public RECT Rect; }

    private static readonly List<Candidate> candidates = new();
    private static readonly List<RECT> monitors = new();
    private static readonly List<(int Left, int Right)> segments = new();
    private static List<Ledge> next = new();

    private static volatile bool dirty = true;
    private static double lastScan = double.NegativeInfinity;
    private static nint hookSystem, hookObject;

    public static void StartWatching()
    {
        hookSystem = Native.SetWinEventHook(Win32.EVENT_SYSTEM_FOREGROUND, Win32.EVENT_SYSTEM_MINIMIZEEND,
            0, &OnWindowEvent, 0, 0, Win32.WINEVENT_SKIPOWNPROCESS);
        hookObject = Native.SetWinEventHook(Win32.EVENT_OBJECT_DESTROY, Win32.EVENT_OBJECT_LOCATIONCHANGE,
            0, &OnWindowEvent, 0, 0, Win32.WINEVENT_SKIPOWNPROCESS);
    }

    public static void StopWatching()
    {
        if (hookSystem != 0) Native.UnhookWinEvent(hookSystem);
        if (hookObject != 0) Native.UnhookWinEvent(hookObject);
    }

    /// <summary>Rescans if something changed recently or the safety interval elapsed. Returns true if it scanned.</summary>
    public static bool Refresh(double now)
    {
        double since = now - lastScan;
        if (!(dirty && since >= DebounceSeconds) && since < SafetySeconds) return false;
        dirty = false;
        lastScan = now;
        Scan();
        return true;
    }

    public static bool TryGetRect(nint hwnd, out RECT rect)
    {
        RECT r;
        int hr = Native.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, &r, (uint)sizeof(RECT));
        rect = r;
        return hr == 0;
    }

    /// <summary>Scale of the monitor under a point, from its effective DPI.</summary>
    public static float ScaleAt(float x, float y)
    {
        nint monitor = Native.MonitorFromPoint(new POINT { X = (int)x, Y = (int)y }, Win32.MONITOR_DEFAULTTONEAREST);
        return Native.GetDpiForMonitor(monitor, 0, out uint dpi, out _) == 0 ? dpi / 96f : Scale;
    }

    public static bool IsFullscreenAppRunning()
    {
        if (Native.SHQueryUserNotificationState(out int state) != 0) return false;
        return state is Win32.QUNS_BUSY or Win32.QUNS_RUNNING_D3D_FULL_SCREEN or Win32.QUNS_PRESENTATION_MODE;
    }

    private static void Scan()
    {
        monitors.Clear();
        Native.EnumDisplayMonitors(0, null, &CollectMonitor, 0);
        candidates.Clear();
        Native.EnumWindows(&CollectWindow, 0);

        if (monitors.Count == 0) return;
        Home = monitors[0];
        monitors.Sort(static (a, b) => a.Left.CompareTo(b.Left));

        next.Clear();
        int minX = int.MaxValue, maxX = int.MinValue, maxBottom = int.MinValue, maxHeight = 0;
        foreach (RECT m in monitors)
        {
            minX = Math.Min(minX, m.Left);
            maxX = Math.Max(maxX, m.Right);
            maxBottom = Math.Max(maxBottom, m.Bottom);
            maxHeight = Math.Max(maxHeight, m.Bottom - m.Top);
            AddFloor(m);
        }
        MinX = minX; MaxX = maxX; MaxBottom = maxBottom; MaxHeight = maxHeight;

        int headroom = (int)(135 * Scale), minLength = (int)(90 * Scale), floorGap = (int)(30 * Scale);
        for (int i = 0; i < candidates.Count; i++)
        {
            RECT r = candidates[i].Rect;
            int y = r.Top;
            if (!TryFindMonitor(r, y, headroom, floorGap, out RECT mon)) continue;

            segments.Clear();
            segments.Add((Math.Max(r.Left, mon.Left), Math.Min(r.Right, mon.Right)));
            for (int j = 0; j < i && segments.Count > 0; j++) Subtract(candidates[j].Rect, y);

            foreach (var (left, right) in segments)
                if (right - left >= minLength)
                    next.Add(new Ledge { Hwnd = candidates[i].Hwnd, Left = left, Right = right, Y = y, WinLeft = r.Left, WinTop = r.Top });
        }

        (Ledges, next) = (next, Ledges);
        Version++;
    }

    /// <summary>Adds a monitor's floor, joining it to the previous one when they meet at the same height.</summary>
    private static void AddFloor(RECT m)
    {
        int last = next.Count - 1;
        if (last >= 0 && next[last].Y == m.Bottom && next[last].Right >= m.Left)
        {
            Ledge joined = next[last];
            joined.Right = Math.Max(joined.Right, m.Right);
            next[last] = joined;
            return;
        }
        next.Add(new Ledge { Hwnd = 0, Left = m.Left, Right = m.Right, Y = m.Bottom });
    }

    private static bool TryFindMonitor(RECT r, int y, int headroom, int floorGap, out RECT found)
    {
        foreach (RECT m in monitors)
        {
            if (r.Right > m.Left && r.Left < m.Right && y >= m.Top + headroom && y <= m.Bottom - floorGap)
            {
                found = m;
                return true;
            }
        }
        found = default;
        return false;
    }

    /// <summary>Removes the span covered by a higher window from every segment of the top edge at y.</summary>
    private static void Subtract(RECT above, int y)
    {
        if (!(above.Top <= y && above.Bottom > y)) return;
        for (int k = segments.Count - 1; k >= 0; k--)
        {
            var (left, right) = segments[k];
            if (above.Right <= left || above.Left >= right) continue;
            segments.RemoveAt(k);
            if (left < above.Left) segments.Add((left, above.Left));
            if (above.Right < right) segments.Add((above.Right, right));
        }
    }

    [UnmanagedCallersOnly]
    private static void OnWindowEvent(nint hook, uint ev, nint hwnd, int idObject, int idChild, uint thread, uint time)
    {
        if (idObject == 0 && hwnd != Self) dirty = true;
    }

    [UnmanagedCallersOnly]
    private static int CollectMonitor(nint monitor, nint dc, RECT* rect, nint lParam)
    {
        MONITORINFO info = default;
        info.cbSize = (uint)sizeof(MONITORINFO);
        if (Native.GetMonitorInfo(monitor, ref info) != 0) monitors.Add(info.rcWork);
        return 1;
    }

    [UnmanagedCallersOnly]
    private static int CollectWindow(nint hwnd, nint lParam)
    {
        if (hwnd == Self || Native.IsWindowVisible(hwnd) == 0 || Native.IsIconic(hwnd) != 0) return 1;

        long exStyle = Native.GetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE);
        bool toolWindow = (exStyle & Win32.WS_EX_TOOLWINDOW) != 0 && (exStyle & Win32.WS_EX_APPWINDOW) == 0;
        if (toolWindow || (exStyle & Win32.WS_EX_NOACTIVATE) != 0) return 1;

        int cloaked = 0;
        Native.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, &cloaked, sizeof(int));
        if (cloaked != 0 || Native.GetWindowTextLength(hwnd) == 0) return 1;

        char* buffer = stackalloc char[64];
        var name = new ReadOnlySpan<char>(buffer, Native.GetClassName(hwnd, buffer, 64));
        if (name.SequenceEqual("Progman") || name.SequenceEqual("WorkerW") ||
            name.SequenceEqual("Shell_TrayWnd") || name.SequenceEqual("Shell_SecondaryTrayWnd"))
            return 1;

        if (!TryGetRect(hwnd, out RECT rect) || rect.Right - rect.Left < 120 || rect.Bottom - rect.Top < 60) return 1;
        candidates.Add(new Candidate { Hwnd = hwnd, Rect = rect });
        return 1;
    }
}
