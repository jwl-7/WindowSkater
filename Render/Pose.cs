namespace WindowSkater;

/// <summary>Everything the renderer needs to draw one frame of a skater.</summary>
internal struct Pose
{
    public int Dir;
    public float Time;

    // rider
    public float Crouch, Lean, Tuck;
    public float ArmUp, FrontArmUp, ArmSpread;
    public float CapeFlow;
    public bool Thrust;

    // board attitude (radians) and extra drop in art units
    public float Pitch, Roll, Yaw, Drop;
}
