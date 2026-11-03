using System;
using System.Diagnostics;
using System.Threading;

namespace WindowSkater;

internal static unsafe class Program
{
    private const int FrameMilliseconds = 16, HiddenPollMilliseconds = 1000;
    private const double EnvironmentCheckSeconds = 1.0, KeepOnTopSeconds = 2.0;
    private const int MaxStepsPerFrame = 8;

    private static PetWindow pet = null!;
    private static nint timer;
    private static bool running = true, fullscreenHidden;
    private static float scale;

    public static Skater Skater { get; private set; } = null!;
    public static bool Paused { get; private set; }
    public static uint TaskbarCreated { get; private set; }

    public static void Quit() => running = false;

    public static void TogglePause()
    {
        Paused = !Paused;
        if (Paused) pet.Hide();
        else if (!fullscreenHidden) pet.Show();
        ArmTimer();
    }

    public static void ChooseCharacter(string name)
    {
        Settings.Character = name;
        Skater.SetCharacter(PickCharacter(name));
    }

    private static Character PickCharacter(string name) =>
        name == Settings.RandomCharacter ? Character.All[Random.Shared.Next(Character.All.Length)] : Character.Find(name);

    private static int Main()
    {
        using var singleInstance = new Mutex(true, "Local\\WindowSkater.SingleInstance", out bool first);
        if (!first) return 0;

        var gdiInput = new GdiplusStartupInput { GdiplusVersion = 1 };
        Native.GdiplusStartup(out nuint gdiToken, ref gdiInput, 0);

        scale = Native.GetDpiForSystem() / 96f;
        TaskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");

        pet = new PetWindow(scale);
        pet.Create();
        WindowScanner.Self = pet.Hwnd;
        WindowScanner.Scale = scale;
        WindowScanner.StartWatching();
        WindowScanner.Refresh(0);

        Skater = new Skater(PickCharacter(Settings.Character), scale);
        Skater.Spawn();
        TrayIcon.Add(pet.Hwnd);
        pet.Render(Skater);
        pet.Show();

        timer = Native.CreateWaitableTimerEx(0, 0, Win32.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, Win32.TIMER_ALL_ACCESS);
        if (timer == 0) timer = Native.CreateWaitableTimerEx(0, 0, 0, Win32.TIMER_ALL_ACCESS);
        ArmTimer();

        RunLoop();

        WindowScanner.StopWatching();
        TrayIcon.Remove(pet.Hwnd);
        pet.Dispose();
        Native.GdiplusShutdown(gdiToken);
        return 0;
    }

    private static void RunLoop()
    {
        var clock = Stopwatch.StartNew();
        nint handle = timer;
        double last = 0, lastCheck = 0, lastOnTop = 0;
        float accumulator = 0f;

        while (running)
        {
            uint woke = Native.MsgWaitForMultipleObjects(1, &handle, 0, Win32.INFINITE, Win32.QS_ALLINPUT);
            PumpMessages();
            if (woke != 0 || !running) continue;   // only the frame timer drives a tick

            double now = clock.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(now - last, 0.05);
            last = now;

            if (fullscreenHidden || now - lastCheck >= EnvironmentCheckSeconds)
            {
                lastCheck = now;
                CheckEnvironment();
            }
            if (fullscreenHidden) continue;

            WindowScanner.Refresh(now);
            Skater.BeginFrame();
            accumulator += dt;
            for (int steps = 0; accumulator >= Skater.Step; steps++, accumulator -= Skater.Step)
            {
                if (steps == MaxStepsPerFrame) { accumulator = 0f; break; }
                Skater.Update(Skater.Step);
            }
            pet.Render(Skater);

            if (now - lastOnTop >= KeepOnTopSeconds)
            {
                lastOnTop = now;
                pet.KeepOnTop();
            }
        }
    }

    private static void PumpMessages()
    {
        while (Native.PeekMessage(out MSG message, 0, 0, 0, Win32.PM_REMOVE) != 0)
        {
            if (message.message == Win32.WM_QUIT) running = false;
            Native.TranslateMessage(ref message);
            Native.DispatchMessage(ref message);
        }
    }

    /// <summary>Hides during fullscreen apps and follows the skater across monitors with different DPI.</summary>
    private static void CheckEnvironment()
    {
        bool fullscreen = WindowScanner.IsFullscreenAppRunning();
        if (fullscreen != fullscreenHidden)
        {
            fullscreenHidden = fullscreen;
            if (fullscreen) pet.Hide();
            else if (!Paused) pet.Show();
            ArmTimer();
        }
        if (fullscreenHidden) return;

        float monitorScale = WindowScanner.ScaleAt(Skater.X, Skater.Y);
        if (MathF.Abs(monitorScale - scale) < 0.01f) return;
        scale = monitorScale;
        WindowScanner.Scale = scale;
        Skater.SetScale(scale);
        pet.Resize(scale);
    }

    /// <summary>Frame rate while active, a slow poll while a fullscreen app hides us, off while paused.</summary>
    private static void ArmTimer()
    {
        if (Paused)
        {
            Native.CancelWaitableTimer(timer);
            return;
        }
        int period = fullscreenHidden ? HiddenPollMilliseconds : FrameMilliseconds;
        long due = -10_000L * period;
        Native.SetWaitableTimer(timer, &due, period, 0, 0, 0);
    }
}
