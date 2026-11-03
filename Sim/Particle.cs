namespace WindowSkater;

/// <summary>A short-lived line segment: spark, dust, smoke or flame. Position is in screen pixels.</summary>
internal struct Particle
{
    public float X, Y, VX, VY, Life, Max, Gravity, Width;
    public uint Rgb;
}
