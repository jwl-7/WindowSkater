using System;

namespace WindowSkater;

internal enum Grind : byte { None, FiftyFifty, Boardslide, Darkslide }

internal enum Flip : byte { None, Kickflip, Heelflip, TreFlip, ShoveIt }

/// <summary>Board orientation: roll and yaw in radians, plus extra drop (art units) so the contact point stays on the ledge.</summary>
internal readonly struct Attitude
{
    public readonly float Roll, Yaw, Drop;
    public Attitude(float roll, float yaw, float drop) { Roll = roll; Yaw = yaw; Drop = drop; }

    public static Attitude Lerp(in Attitude a, in Attitude b, float t) =>
        new(a.Roll + (b.Roll - a.Roll) * t, a.Yaw + (b.Yaw - a.Yaw) * t, a.Drop + (b.Drop - a.Drop) * t);
}

internal static class Tricks
{
    private const float BoardslideYaw = 75f * MathF.PI / 180f;
    private const float DarkslideDrop = 17f;
    private const float MinFlipFlight = 0.42f, MinTreFlight = 0.55f;

    /// <summary>The attitude the board rests in while on the ground or grinding.</summary>
    public static Attitude Rest(Grind grind) => grind switch
    {
        Grind.Boardslide => new Attitude(0f, BoardslideYaw, 0f),
        Grind.Darkslide => new Attitude(MathF.PI, 0f, DarkslideDrop),
        _ => default,
    };

    /// <summary>Picks a flip that fits the available airtime, or None if there isn't enough.</summary>
    public static Flip PickFlip(Random rng, float flight)
    {
        if (flight < MinFlipFlight) return Flip.None;
        float r = rng.NextSingle();
        if (r < 0.30f) return Flip.Kickflip;
        if (r < 0.55f) return Flip.Heelflip;
        if (r < 0.75f && flight >= MinTreFlight) return Flip.TreFlip;
        if (r < 0.90f) return Flip.ShoveIt;
        return Flip.None;
    }

    /// <summary>Adds the full rotations a flip performs on top of the destination attitude.</summary>
    public static Attitude WithFlip(in Attitude rest, Flip flip) => flip switch
    {
        Flip.Kickflip => new Attitude(rest.Roll + MathF.Tau, rest.Yaw, rest.Drop),
        Flip.Heelflip => new Attitude(rest.Roll - MathF.Tau, rest.Yaw, rest.Drop),
        Flip.TreFlip => new Attitude(rest.Roll + MathF.Tau, rest.Yaw + MathF.Tau, rest.Drop),
        Flip.ShoveIt => new Attitude(rest.Roll, rest.Yaw + MathF.PI, rest.Drop),
        _ => rest,
    };

    public static float Ease(float t) => t * t * (3f - 2f * t);
}
