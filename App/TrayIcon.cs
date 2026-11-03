using System;

namespace WindowSkater;

/// <summary>Notification-area icon (the exe's own icon) and its menu.</summary>
internal static unsafe class TrayIcon
{
    private const uint CmdPause = 1, CmdExit = 2, CmdStartup = 3, CmdRandom = 100, CmdCharacterBase = 101;
    private const string Tip = "Window Skater";

    private static nint icon;

    public static void Add(nint hwnd)
    {
        if (icon == 0) icon = Native.ExtractIcon(0, Environment.ProcessPath ?? "", 0);

        NOTIFYICONDATAW data = default;
        data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
        data.hWnd = hwnd;
        data.uID = 1;
        data.uFlags = Win32.NIF_MESSAGE | Win32.NIF_ICON | Win32.NIF_TIP;
        data.uCallbackMessage = Win32.WM_TRAY;
        data.hIcon = icon;
        for (int i = 0; i < Tip.Length; i++) data.szTip[i] = Tip[i];
        Native.Shell_NotifyIcon(Win32.NIM_ADD, &data);
    }

    public static void Remove(nint hwnd)
    {
        NOTIFYICONDATAW data = default;
        data.cbSize = (uint)sizeof(NOTIFYICONDATAW);
        data.hWnd = hwnd;
        data.uID = 1;
        Native.Shell_NotifyIcon(Win32.NIM_DELETE, &data);
    }

    public static void ShowMenu(nint hwnd)
    {
        string current = Settings.Character;
        nint characters = Native.CreatePopupMenu();
        Native.AppendMenu(characters, Checked(current == Settings.RandomCharacter), CmdRandom, Settings.RandomCharacter);
        for (int i = 0; i < Character.All.Length; i++)
        {
            string name = Character.All[i].Name;
            Native.AppendMenu(characters, Checked(name == current), (nuint)(CmdCharacterBase + i), name);
        }

        nint menu = Native.CreatePopupMenu();
        Native.AppendMenu(menu, Win32.MF_POPUP, (nuint)characters, "Character");
        Native.AppendMenu(menu, Win32.MF_STRING, CmdPause, Program.Paused ? "Resume" : "Pause");
        Native.AppendMenu(menu, Checked(Settings.RunAtStartup), CmdStartup, "Run at startup");
        Native.AppendMenu(menu, Win32.MF_SEPARATOR, 0, null);
        Native.AppendMenu(menu, Win32.MF_STRING, CmdExit, "Exit");

        Native.GetCursorPos(out POINT cursor);
        Native.SetForegroundWindow(hwnd);
        uint command = (uint)Native.TrackPopupMenu(menu, Win32.TPM_RETURNCMD | Win32.TPM_RIGHTBUTTON, cursor.X, cursor.Y, 0, hwnd, 0);
        Native.DestroyMenu(menu);
        Native.PostMessage(hwnd, Win32.WM_NULL, 0, 0);

        switch (command)
        {
            case CmdPause: Program.TogglePause(); break;
            case CmdExit: Program.Quit(); break;
            case CmdStartup: Settings.RunAtStartup = !Settings.RunAtStartup; break;
            case CmdRandom: Program.ChooseCharacter(Settings.RandomCharacter); break;
            case >= CmdCharacterBase when command < CmdCharacterBase + Character.All.Length:
                Program.ChooseCharacter(Character.All[command - CmdCharacterBase].Name);
                break;
        }
    }

    private static uint Checked(bool on) => on ? Win32.MF_STRING | Win32.MF_CHECKED : Win32.MF_STRING;
}
