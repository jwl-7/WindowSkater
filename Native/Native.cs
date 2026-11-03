using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

namespace WindowSkater;

[StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X, Y; }
[StructLayout(LayoutKind.Sequential)] internal struct SIZE { public int CX, CY; }
[StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left, Top, Right, Bottom; }

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public nint hwnd; public uint message; public nuint wParam; public nint lParam;
    public uint time; public POINT pt; public uint lPrivate;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WNDCLASSEXW
{
    public uint cbSize, style; public nint lpfnWndProc; public int cbClsExtra, cbWndExtra;
    public nint hInstance, hIcon, hCursor, hbrBackground, lpszMenuName, lpszClassName, hIconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
{
    public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount;
    public uint biCompression, biSizeImage; public int biXPelsPerMeter, biYPelsPerMeter;
    public uint biClrUsed, biClrImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO { public uint cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

[StructLayout(LayoutKind.Sequential)]
internal struct GdiplusStartupInput
{
    public uint GdiplusVersion; public nint DebugEventCallback;
    public int SuppressBackgroundThread, SuppressExternalCodecs;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NOTIFYICONDATAW
{
    public uint cbSize; public nint hWnd; public uint uID, uFlags, uCallbackMessage; public nint hIcon;
    public fixed char szTip[128];
    public uint dwState, dwStateMask;
    public fixed char szInfo[256];
    public uint uTimeoutOrVersion;
    public fixed char szInfoTitle[64];
    public uint dwInfoFlags;
    public Guid guidItem;
    public nint hBalloonIcon;
}

internal static unsafe partial class Native
{
    // ---- user32 ----
    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW")] public static partial ushort RegisterClassEx(ref WNDCLASSEXW wc);
    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(uint exStyle, string cls, string? title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint inst, nint param);
    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")] public static partial nint DefWindowProc(nint h, uint msg, nuint wp, nint lp);
    [LibraryImport("user32.dll", EntryPoint = "PeekMessageW")] public static partial int PeekMessage(out MSG msg, nint h, uint min, uint max, uint remove);
    [LibraryImport("user32.dll")] public static partial int TranslateMessage(ref MSG msg);
    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")] public static partial nint DispatchMessage(ref MSG msg);
    [LibraryImport("user32.dll")] public static partial void PostQuitMessage(int code);
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")] public static partial int PostMessage(nint h, uint msg, nuint wp, nint lp);
    [LibraryImport("user32.dll")] public static partial int DestroyWindow(nint h);
    [LibraryImport("user32.dll")] public static partial int ShowWindow(nint h, int cmd);
    [LibraryImport("user32.dll")] public static partial int SetWindowPos(nint h, nint after, int x, int y, int cx, int cy, uint flags);
    [LibraryImport("user32.dll")] public static partial int UpdateLayeredWindow(nint h, nint hdcDst, POINT* pptDst, SIZE* psize, nint hdcSrc, POINT* pptSrc, uint crKey, BLENDFUNCTION* blend, uint flags);
    [LibraryImport("user32.dll")] public static partial nint GetDC(nint h);
    [LibraryImport("user32.dll")] public static partial int ReleaseDC(nint h, nint dc);
    [LibraryImport("user32.dll")] public static partial int EnumWindows(delegate* unmanaged<nint, nint, int> proc, nint lp);
    [LibraryImport("user32.dll")] public static partial int IsWindowVisible(nint h);
    [LibraryImport("user32.dll")] public static partial int IsIconic(nint h);
    [LibraryImport("user32.dll")] public static partial int IsWindow(nint h);
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static partial nint GetWindowLongPtr(nint h, int index);
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW")] public static partial int GetWindowTextLength(nint h);
    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW")] public static partial int GetClassName(nint h, char* buf, int max);
    [LibraryImport("user32.dll")] public static partial int GetCursorPos(out POINT p);
    [LibraryImport("user32.dll")] public static partial nint SetCapture(nint h);
    [LibraryImport("user32.dll")] public static partial int ReleaseCapture();
    [LibraryImport("user32.dll")] public static partial uint GetDpiForSystem();
    [LibraryImport("user32.dll")] public static partial nint MonitorFromPoint(POINT pt, int flags);
    [LibraryImport("user32.dll")] public static partial int EnumDisplayMonitors(nint hdc, RECT* clip, delegate* unmanaged<nint, nint, RECT*, nint, int> proc, nint lp);
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")] public static partial int GetMonitorInfo(nint mon, ref MONITORINFO mi);
    [LibraryImport("user32.dll")] public static partial nint CreatePopupMenu();
    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16)] public static partial int AppendMenu(nint menu, uint flags, nuint id, string? text);
    [LibraryImport("user32.dll")] public static partial int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);
    [LibraryImport("user32.dll")] public static partial int DestroyMenu(nint menu);
    [LibraryImport("user32.dll")] public static partial int SetForegroundWindow(nint h);
    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW")] public static partial nint LoadCursor(nint inst, nint name);
    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)] public static partial uint RegisterWindowMessage(string s);
    [LibraryImport("user32.dll")] public static partial uint MsgWaitForMultipleObjects(uint count, nint* handles, int waitAll, uint ms, uint mask);
    [LibraryImport("user32.dll")] public static partial nint SetWinEventHook(uint min, uint max, nint mod, delegate* unmanaged<nint, uint, nint, int, int, uint, uint, void> proc, uint pid, uint tid, uint flags);
    [LibraryImport("user32.dll")] public static partial int UnhookWinEvent(nint hook);

    // ---- kernel32 / dwmapi / shell32 / shcore ----
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")] public static partial nint GetModuleHandle(nint name);
    [LibraryImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW")] public static partial nint CreateWaitableTimerEx(nint attrs, nint name, uint flags, uint access);
    [LibraryImport("kernel32.dll")] public static partial int SetWaitableTimer(nint timer, long* dueTime, int period, nint completion, nint arg, int resume);
    [LibraryImport("kernel32.dll")] public static partial int CancelWaitableTimer(nint timer);
    [LibraryImport("dwmapi.dll")] public static partial int DwmGetWindowAttribute(nint h, uint attr, void* pv, uint cb);
    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")] public static partial int Shell_NotifyIcon(uint msg, NOTIFYICONDATAW* data);
    [LibraryImport("shell32.dll")] public static partial int SHQueryUserNotificationState(out int state);
    [LibraryImport("shell32.dll", EntryPoint = "ExtractIconW", StringMarshalling = StringMarshalling.Utf16)] public static partial nint ExtractIcon(nint inst, string file, uint index);
    [LibraryImport("shcore.dll")] public static partial int GetDpiForMonitor(nint mon, int type, out uint dpiX, out uint dpiY);

    // ---- gdi32 ----
    [LibraryImport("gdi32.dll")] public static partial nint CreateCompatibleDC(nint dc);
    [LibraryImport("gdi32.dll")] public static partial nint CreateDIBSection(nint dc, ref BITMAPINFOHEADER bmi, uint usage, out nint bits, nint section, uint offset);
    [LibraryImport("gdi32.dll")] public static partial nint SelectObject(nint dc, nint obj);
    [LibraryImport("gdi32.dll")] public static partial int DeleteObject(nint obj);
    [LibraryImport("gdi32.dll")] public static partial int DeleteDC(nint dc);

    // ---- gdiplus (flat API) ----
    [LibraryImport("gdiplus.dll")] public static partial int GdiplusStartup(out nuint token, ref GdiplusStartupInput input, nint output);
    [LibraryImport("gdiplus.dll")] public static partial void GdiplusShutdown(nuint token);
    [LibraryImport("gdiplus.dll")] public static partial int GdipCreateBitmapFromScan0(int w, int h, int stride, int format, nint scan0, out nint bmp);
    [LibraryImport("gdiplus.dll")] public static partial int GdipGetImageGraphicsContext(nint img, out nint g);
    [LibraryImport("gdiplus.dll")] public static partial int GdipSetSmoothingMode(nint g, int mode);
    [LibraryImport("gdiplus.dll")] public static partial int GdipGraphicsClear(nint g, uint argb);
    [LibraryImport("gdiplus.dll")] public static partial int GdipCreateSolidFill(uint argb, out nint brush);
    [LibraryImport("gdiplus.dll")] public static partial int GdipSetSolidFillColor(nint brush, uint argb);
    [LibraryImport("gdiplus.dll")] public static partial int GdipCreatePen1(uint argb, float width, int unit, out nint pen);
    [LibraryImport("gdiplus.dll")] public static partial int GdipSetPenColor(nint pen, uint argb);
    [LibraryImport("gdiplus.dll")] public static partial int GdipSetPenWidth(nint pen, float width);
    [LibraryImport("gdiplus.dll")] public static partial int GdipFillPolygon(nint g, nint brush, PointF* pts, int count, int fillMode);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDrawPolygon(nint g, nint pen, PointF* pts, int count);
    [LibraryImport("gdiplus.dll")] public static partial int GdipFillEllipse(nint g, nint brush, float x, float y, float w, float h);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDrawLine(nint g, nint pen, float x1, float y1, float x2, float y2);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDeleteGraphics(nint g);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDisposeImage(nint img);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDeleteBrush(nint brush);
    [LibraryImport("gdiplus.dll")] public static partial int GdipDeletePen(nint pen);
}
