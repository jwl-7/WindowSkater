using System;

namespace WindowSkater;

internal enum Mode : byte { Ride, Grind, Air, Drag, Bail }

/// <summary>Position, physics and behaviour of the skater. Lengths are physical screen pixels.</summary>
internal sealed class Skater
{
    /// <summary>Fixed simulation step in seconds.</summary>
    public const float Step = 1f / 120f;

    private const float HopHeight = 75f, ClearHeight = 60f, MaxLaunchSpeed = 520f;
    private const float BailSeconds = 1.1f;

    public Character Character { get; private set; }
    public float X, Y, VX, VY, Time;
    public int Dir = 1;
    public Mode Mode = Mode.Air;
    public readonly Particle[] Particles = new Particle[64];

    private readonly Random rng = new();
    private float scale;

    // current ledge
    private Ledge cur;
    private RECT curRect;
    private int seenVersion = -1;
    private bool ledgeLost;

    // board attitude: resting, and the tween used while airborne
    private Grind style, plannedStyle;
    private Attitude rest, tweenFrom, tweenTo;
    private float tweenSeconds, airTime;

    // decisions
    private int wantDir = 1;
    private float thinkTimer = 1f, idleTimer, bailTimer, landSquash, celebrate, emitCarry;
    private Ledge goal;
    private bool hasGoal;
    private float launchX;
    private readonly Ledge[] recent = new Ledge[3];
    private int recentNext;

    // dragging
    private float grabDX, grabDY;
    private readonly (float T, float X, float Y)[] samples = new (float, float, float)[8];
    private int sampleCount;

    public Skater(Character character, float scale)
    {
        Character = character;
        this.scale = scale;
    }

    private float Gravity => 1800f * scale;
    private float RideSpeed => 170f * scale * Character.Speed;
    private float GrindSpeed => 130f * scale * Character.Speed;

    public void SetCharacter(Character character) => Character = character;
    public void SetScale(float value) => scale = value;

    public void Spawn()
    {
        RECT home = WindowScanner.Home;
        X = (home.Left + home.Right) / 2f;
        Y = home.Top - 40f * scale;
        VX = VY = 0f;
        Fall();
    }

    public void Update(float dt)
    {
        Time += dt;
        UpdateParticles(dt);
        celebrate = MathF.Max(0f, celebrate - dt);
        switch (Mode)
        {
            case Mode.Drag: UpdateDrag(); break;
            case Mode.Air: UpdateAir(dt); break;
            case Mode.Bail: UpdateBail(dt); break;
            default: UpdateGround(dt); break;
        }
    }

    private void UpdateGround(float dt)
    {
        if (!FollowLedge()) { Fall(); return; }

        idleTimer -= dt;
        float top = Mode == Mode.Grind ? GrindSpeed : RideSpeed;
        float target = idleTimer > 0f ? 0f : wantDir * top;
        float accel = 600f * scale * dt;
        VX = VX < target ? MathF.Min(VX + accel, target) : MathF.Max(VX - accel, target);
        if (wantDir != Dir && MathF.Abs(VX) < 30f * scale) Dir = wantDir;
        X += VX * dt;
        landSquash = MathF.Max(0f, landSquash - 3f * dt);

        if (Mode == Mode.Grind) EmitGrindSparks(dt);

        if ((VX > 0f && X > cur.Right) || (VX < 0f && X < cur.Left)) { LedgeEnd(); return; }
        Think(dt);
    }

    /// <summary>
    /// Once-per-frame work that needs system calls: rides along with a moving window and
    /// re-validates the goal. The 120 Hz steps then use the cached results.
    /// </summary>
    public void BeginFrame()
    {
        if (Mode is not (Mode.Ride or Mode.Grind or Mode.Bail)) return;

        if (cur.Hwnd != 0)
        {
            ledgeLost = Native.IsWindow(cur.Hwnd) == 0 || Native.IsWindowVisible(cur.Hwnd) == 0 || Native.IsIconic(cur.Hwnd) != 0;
            if (!ledgeLost && WindowScanner.TryGetRect(cur.Hwnd, out RECT r))
            {
                int dx = r.Left - curRect.Left, dy = r.Top - curRect.Top;
                if (dx != 0 || dy != 0)
                {
                    X += dx; Y += dy;
                    cur = cur.Shifted(dx, dy);
                    curRect = r;
                }
            }
        }

        if (hasGoal && !RefreshGoal())
        {
            hasGoal = false;
            thinkTimer = 0.4f;
        }
    }

    /// <summary>False once the window under us is gone or no longer holds our spot after a rescan.</summary>
    private bool FollowLedge()
    {
        if (ledgeLost) return false;
        if (seenVersion == WindowScanner.Version) return true;
        seenVersion = WindowScanner.Version;
        return Relocate();
    }

    /// <summary>After a rescan, finds the (possibly resized or partly covered) ledge we are standing on.</summary>
    private bool Relocate()
    {
        foreach (Ledge raw in WindowScanner.Ledges)
        {
            if (raw.Hwnd != cur.Hwnd) continue;
            Ledge l = raw.Live();
            if (Math.Abs(l.Y - cur.Y) <= 4 && X >= l.Left - 2 && X <= l.Right + 2)
            {
                cur = l;
                Y = l.Y;
                return true;
            }
        }
        return false;
    }

    private bool IsCurrent(in Ledge l) =>
        l.Hwnd == cur.Hwnd && Math.Abs(l.Y - cur.Y) <= 4 && l.Left < cur.Right && cur.Left < l.Right;

    private static bool IsSame(in Ledge a, in Ledge b) =>
        a.Hwnd == b.Hwnd && Math.Abs(a.Y - b.Y) <= 60 && a.Left < b.Right && b.Left < a.Right;

    private void Think(float dt)
    {
        thinkTimer -= dt;
        if (hasGoal) { PursueGoal(); return; }
        if (thinkTimer > 0f) return;

        thinkTimer = 1.2f + rng.NextSingle() * 2.3f;
        float roll = rng.NextSingle();
        if (roll < 0.28f) Hop();
        else if (roll < 0.38f && Mode == Mode.Ride) idleTimer = 0.6f + rng.NextSingle() * 0.9f;
        else ChooseGoal(0);
    }

    private void PursueGoal()
    {
        float distance = launchX - X;
        if (MathF.Abs(distance) > 8f * scale) { wantDir = distance > 0f ? 1 : -1; return; }
        hasGoal = false;
        if (!LaunchAt(goal)) thinkTimer = 0.4f;
    }

    /// <summary>Picks the most attractive reachable ledge. onlyDir restricts it to targets in that direction.</summary>
    private bool ChooseGoal(int onlyDir)
    {
        float best = float.NegativeInfinity;
        hasGoal = false;
        foreach (Ledge raw in WindowScanner.Ledges)
        {
            Ledge l = raw.Live();
            if (IsCurrent(l)) continue;

            float launch = LaunchPoint(l);
            float landing = LandingPoint(l, launch);
            if (onlyDir != 0 && landing != launch && (landing > launch ? 1 : -1) != onlyDir) continue;
            if (!Ballistic(launch, cur.Y, l, landing, out _, out _, out _)) continue;

            float higher = Math.Clamp((cur.Y - l.Y) / (300f * scale), -1f, 1f);
            float score = (l.Hwnd != 0 ? 2f : 0f) + higher
                        - MathF.Abs(landing - launch) / (700f * scale)
                        - RecentPenalty(l)
                        + rng.NextSingle() * 1.5f;
            if (score <= best) continue;
            best = score; goal = l; launchX = launch; hasGoal = true;
        }
        return hasGoal;
    }

    private bool RefreshGoal()
    {
        foreach (Ledge raw in WindowScanner.Ledges)
        {
            Ledge l = raw.Live();
            if (!IsSame(l, goal)) continue;
            float launch = LaunchPoint(l);
            if (!Ballistic(launch, cur.Y, l, LandingPoint(l, launch), out _, out _, out _)) return false;
            goal = l; launchX = launch;
            return true;
        }
        return false;
    }

    /// <summary>Where on the current ledge to take off to reach l.</summary>
    private float LaunchPoint(in Ledge l)
    {
        float margin = 12f * scale;
        float lo = MathF.Max(cur.Left, l.Left), hi = MathF.Min(cur.Right, l.Right);
        if (lo <= hi) return Math.Clamp(X, lo, hi);
        return l.Right < cur.Left ? cur.Left + margin : cur.Right - margin;
    }

    private float LandingPoint(in Ledge l, float fromX)
    {
        float margin = 14f * scale;
        if (l.Right - l.Left < 2f * margin) return (l.Left + l.Right) / 2f;
        return Math.Clamp(fromX, l.Left + margin, l.Right - margin);
    }

    private float RecentPenalty(in Ledge l)
    {
        float penalty = 0f;
        foreach (Ledge r in recent)
            if (r.Right > r.Left && IsSame(l, r)) penalty += 2.5f;
        return penalty;
    }

    private void LedgeEnd()
    {
        int dir = VX > 0f ? 1 : -1;
        X = dir > 0 ? cur.Right : cur.Left;
        hasGoal = false;

        if (rng.NextSingle() < 0.55f && ChooseGoal(dir))
        {
            hasGoal = false;
            if (LaunchAt(goal)) return;
        }
        bool screenEdge = dir > 0 ? cur.Right >= WindowScanner.MaxX : cur.Left <= WindowScanner.MinX;
        if (cur.Hwnd != 0 && !screenEdge && rng.NextSingle() < 0.2f)
        {
            X += dir * 6f * scale;   // roll off the edge
            Fall();
            return;
        }
        wantDir = -dir;
        VX *= 0.3f;
    }

    /// <summary>Solves the jump from (fromX, fromY) to land at landingX on ledge t.</summary>
    private bool Ballistic(float fromX, float fromY, in Ledge t, float landingX, out float vx, out float vy, out float seconds)
    {
        vx = vy = seconds = 0f;
        float dy = t.Y - fromY;
        if (MathF.Abs(dy) > 0.95f * WindowScanner.MaxHeight) return false;

        vy = -MathF.Sqrt(2f * Gravity * (MathF.Max(-dy, 0f) + ClearHeight * scale));
        float discriminant = vy * vy + 2f * Gravity * dy;
        if (discriminant < 0f) return false;
        seconds = (-vy + MathF.Sqrt(discriminant)) / Gravity;
        vx = (landingX - fromX) / seconds;
        return MathF.Abs(vx) <= MaxLaunchSpeed * scale;
    }

    private bool LaunchAt(in Ledge t)
    {
        float landing = LandingPoint(t, X);
        float wander = Dir * rng.NextSingle() * 100f * scale;
        float margin = 14f * scale;
        if (t.Right - t.Left >= 2f * margin) landing = Math.Clamp(landing + wander, t.Left + margin, t.Right - margin);

        if (!Ballistic(X, Y, t, landing, out float vx, out float vy, out float seconds))
        {
            landing = LandingPoint(t, X);
            if (!Ballistic(X, Y, t, landing, out vx, out vy, out seconds)) return false;
        }

        VX = vx; VY = vy;
        if (MathF.Abs(vx) > 5f) Dir = wantDir = vx > 0f ? 1 : -1;
        recent[recentNext] = cur;
        recentNext = (recentNext + 1) % recent.Length;
        BeginAir(seconds, t.Hwnd == 0 ? Grind.None : PickStyle(), allowFlip: true);
        return true;
    }

    /// <summary>A small ollie in place, usually with a flip.</summary>
    private void Hop()
    {
        float height = HopHeight * scale;
        VY = -MathF.Sqrt(2f * Gravity * height);
        BeginAir(2f * MathF.Sqrt(2f * height / Gravity), Mode == Mode.Grind ? PickStyle() : Grind.None, allowFlip: true);
    }

    private Grind PickStyle() => rng.NextSingle() < 0.65f ? Character.Signature : Grind.FiftyFifty;

    private void BeginAir(float flightSeconds, Grind next, bool allowFlip)
    {
        Flip flip = allowFlip ? Tricks.PickFlip(rng, flightSeconds) : Flip.None;
        tweenFrom = rest;
        tweenTo = Tricks.WithFlip(Tricks.Rest(next), flip);
        plannedStyle = next;
        tweenSeconds = flightSeconds > 0f ? Math.Clamp(flightSeconds * 0.8f, 0.2f, 0.75f) : 0.3f;
        airTime = 0f;
        hasGoal = false;
        Mode = Mode.Air;

        if (flip != Flip.None && (Character.Quirks & Quirks.Smoke) != 0)
            for (int i = 0; i < 6; i++)
                Emit(X, Y, (rng.NextSingle() - 0.5f) * 120f, -rng.NextSingle() * 60f, 0.5f, 0xFF9A9AAA, 8f, -40f);
    }

    /// <summary>Drops off the current ledge with no planned trick.</summary>
    private void Fall()
    {
        tweenFrom = rest;
        tweenTo = default;
        plannedStyle = Grind.None;
        tweenSeconds = 0.3f;
        airTime = 0f;
        VY = 0f;
        hasGoal = false;
        Mode = Mode.Air;
    }

    private bool Tricking => tweenFrom.Roll != tweenTo.Roll || tweenFrom.Yaw != tweenTo.Yaw;

    private void UpdateAir(float dt)
    {
        airTime += dt;
        float oldY = Y;
        VY = MathF.Min(VY + Gravity * dt, 1600f * scale);
        X += VX * dt;
        Y += VY * dt;

        if (X < WindowScanner.MinX) { X = WindowScanner.MinX; VX = MathF.Abs(VX); }
        else if (X > WindowScanner.MaxX) { X = WindowScanner.MaxX; VX = -MathF.Abs(VX); }

        if ((Character.Quirks & Quirks.Jetpack) != 0 && VY < 0f) EmitFlame(dt);
        if (VY <= 0f) return;

        bool found = false;
        Ledge best = default;
        foreach (Ledge l in WindowScanner.Ledges)
        {
            if (oldY <= l.Y + 0.5f && Y >= l.Y && l.Contains(X) && (!found || l.Y < best.Y)) { best = l; found = true; }
        }
        if (found) { Land(best); return; }

        if (Y > WindowScanner.MaxBottom + 40f * scale) LandOnNearestFloor();
    }

    private void LandOnNearestFloor()
    {
        Ledge pick = default;
        float bestGap = float.MaxValue;
        foreach (Ledge l in WindowScanner.Ledges)
        {
            if (l.Hwnd != 0) continue;
            float gap = l.Contains(X) ? 0f : MathF.Min(MathF.Abs(X - l.Left), MathF.Abs(X - l.Right));
            if (gap < bestGap) { bestGap = gap; pick = l; }
        }
        if (pick.Right <= pick.Left) return;
        X = Math.Clamp(X, pick.Left, pick.Right);
        Land(pick);
    }

    private void Land(Ledge l)
    {
        bool trick = Tricking;
        cur = l;
        Y = l.Y;
        VY = 0f;

        style = l.Hwnd == 0 ? Grind.None : plannedStyle != Grind.None ? plannedStyle : Grind.FiftyFifty;
        rest = Tricks.Rest(style);
        Mode = style == Grind.None ? Mode.Ride : Mode.Grind;
        if (l.Hwnd != 0 && WindowScanner.TryGetRect(l.Hwnd, out RECT r)) curRect = r;
        seenVersion = WindowScanner.Version;
        ledgeLost = false;

        landSquash = 1f;
        if (MathF.Abs(VX) > 15f) Dir = VX > 0f ? 1 : -1;
        wantDir = Dir;
        thinkTimer = 1.2f + rng.NextSingle() * 2.3f;
        hasGoal = false;

        if ((Character.Quirks & Quirks.Dust) != 0)
            for (int i = 0; i < 8; i++)
                Emit(X, Y, (rng.NextSingle() - 0.5f) * 240f, -rng.NextSingle() * 90f, 0.35f, 0xFFD8D8E0, 3f, 600f);
        if (trick && (Character.Quirks & Quirks.PeaceSign) != 0) celebrate = 0.8f;
        if (trick && rng.NextSingle() < Character.BailChance) StartBail();
    }

    private void StartBail()
    {
        Mode = Mode.Bail;
        bailTimer = BailSeconds;
        style = cur.Hwnd == 0 ? Grind.None : Grind.FiftyFifty;
        rest = default;
    }

    private void UpdateBail(float dt)
    {
        if (!FollowLedge()) { Fall(); return; }
        bailTimer -= dt;
        VX = VX < 0f ? MathF.Min(0f, VX + 400f * scale * dt) : MathF.Max(0f, VX - 400f * scale * dt);
        X = Math.Clamp(X + VX * dt, cur.Left, cur.Right);
        if (bailTimer > 0f) return;
        Mode = style == Grind.None ? Mode.Ride : Mode.Grind;
        thinkTimer = 0.5f;
    }

    public void BeginDrag()
    {
        Native.GetCursorPos(out POINT cursor);
        grabDX = X - cursor.X;
        grabDY = Y - cursor.Y;
        sampleCount = 0;
        VX = VY = 0f;
        hasGoal = false;
        Mode = Mode.Drag;
    }

    private void UpdateDrag()
    {
        Native.GetCursorPos(out POINT cursor);
        X = cursor.X + grabDX;
        Y = cursor.Y + grabDY;
        if (sampleCount == samples.Length)
        {
            Array.Copy(samples, 1, samples, 0, samples.Length - 1);
            sampleCount--;
        }
        samples[sampleCount++] = (Time, X, Y);
    }

    public void EndDrag()
    {
        if (Mode != Mode.Drag) return;

        float vx = 0f, vy = 0f;
        if (sampleCount >= 2)
        {
            var last = samples[sampleCount - 1];
            int first = sampleCount - 1;
            while (first > 0 && last.T - samples[first - 1].T <= 0.12f) first--;
            float span = last.T - samples[first].T;
            if (span > 0.001f)
            {
                vx = (last.X - samples[first].X) / span;
                vy = (last.Y - samples[first].Y) / span;
            }
        }

        float max = 2200f * scale;
        Fall();
        VX = Math.Clamp(vx, -max, max);
        VY = Math.Clamp(vy, -max, max);
        if (MathF.Abs(VX) > 60f) Dir = wantDir = VX > 0f ? 1 : -1;
    }

    private void Emit(float x, float y, float vx, float vy, float life, uint rgb, float width, float gravity)
    {
        for (int i = 0; i < Particles.Length; i++)
        {
            if (Particles[i].Life > 0f) continue;
            Particles[i] = new Particle
            {
                X = x, Y = y, VX = vx * scale, VY = vy * scale, Life = life, Max = life,
                Rgb = rgb, Width = width * scale, Gravity = gravity * scale,
            };
            return;
        }
    }

    private void EmitGrindSparks(float dt)
    {
        emitCarry += dt * (style == Grind.Darkslide ? 110f : 70f);
        for (; emitCarry >= 1f; emitCarry -= 1f)
        {
            float life = 0.15f + rng.NextSingle() * 0.25f;
            Emit(X - Dir * 26f * scale, Y - scale,
                -Dir * (60f + rng.NextSingle() * 160f), -(80f + rng.NextSingle() * 180f),
                life, Character.SparkColor & 0x00FFFFFF, 2f, 900f);
        }
    }

    private void EmitFlame(float dt)
    {
        emitCarry += dt * 90f;
        for (; emitCarry >= 1f; emitCarry -= 1f)
            Emit(X - Dir * 20f * scale + (rng.NextSingle() - 0.5f) * 6f * scale, Y - 46f * scale,
                (rng.NextSingle() - 0.5f) * 30f, 120f + rng.NextSingle() * 100f, 0.2f, 0xFF9A30, 4f, 0f);
    }

    private void UpdateParticles(float dt)
    {
        foreach (ref Particle p in Particles.AsSpan())
        {
            if (p.Life <= 0f) continue;
            p.Life -= dt;
            p.VY += p.Gravity * dt;
            p.X += p.VX * dt;
            p.Y += p.VY * dt;
        }
    }

    public Pose GetPose()
    {
        var p = new Pose { Dir = Dir, Time = Time, FrontArmUp = celebrate > 0f ? 1f : 0f };
        float speed01 = Math.Clamp(MathF.Abs(VX) / RideSpeed, 0f, 1f);
        Attitude a = rest;

        switch (Mode)
        {
            case Mode.Ride:
                p.Crouch = idleTimer > 0f ? 0.05f : 0.12f + landSquash * 0.45f;
                p.Lean = VX * Dir / RideSpeed * 0.06f;
                p.ArmSpread = 0.3f;
                p.CapeFlow = speed01;
                break;

            case Mode.Grind:
                p.Crouch = 0.5f + landSquash * 0.3f;
                p.Lean = style == Grind.Darkslide ? 0.2f : 0.14f;
                p.ArmSpread = 1f;
                p.CapeFlow = speed01;
                break;

            case Mode.Air:
                float t = Math.Clamp(airTime / tweenSeconds, 0f, 1f);
                a = Attitude.Lerp(tweenFrom, tweenTo, Tricks.Ease(t));
                p.Tuck = Tricking ? MathF.Sin(MathF.PI * t) : 0f;
                float tilt = VY < 0f
                    ? -0.35f * Math.Clamp(-VY / (500f * scale), 0f, 1f)
                    : MathF.Max(0f, 0.15f - airTime * 0.05f);
                p.Pitch = tilt * (1f - 0.6f * p.Tuck);
                p.Crouch = VY < 0f ? 0.75f : 0.45f;
                p.ArmUp = 0.4f;
                p.ArmSpread = 0.6f;
                p.CapeFlow = 0.8f;
                p.Thrust = (Character.Quirks & Quirks.Jetpack) != 0 && VY < 0f;
                break;

            case Mode.Drag:
                p.Crouch = 0.25f;
                p.Pitch = MathF.Sin(Time * 7f) * 0.3f;
                p.Lean = MathF.Sin(Time * 5f + 1f) * 0.15f;
                p.ArmUp = 1f;
                p.ArmSpread = 0.5f;
                break;

            case Mode.Bail:
                p.Crouch = 1f;
                p.Lean = -1.1f * Math.Clamp(bailTimer / 0.3f, 0f, 1f);
                p.Pitch = 0.3f;
                p.ArmUp = 1f;
                p.ArmSpread = 1f;
                break;
        }

        p.Roll = a.Roll; p.Yaw = a.Yaw; p.Drop = a.Drop;
        return p;
    }
}
