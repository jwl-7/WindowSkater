using System;

namespace WindowSkater;

/// <summary>Colours the shared rig uses for the body parts that every character has.</summary>
internal readonly struct Look
{
    public uint Torso { get; init; }
    public uint Plate { get; init; }
    public uint Thigh { get; init; }
    public uint Shin { get; init; }
    public uint Boot { get; init; }
    public uint Arm { get; init; }
    public uint ArmBack { get; init; }
    public uint Glove { get; init; }
    public uint Belt { get; init; }
    public uint Buckle { get; init; }
    public uint Shoulder { get; init; }
}

[Flags]
internal enum Quirks
{
    None = 0,
    Jetpack = 1,    // flame while rising
    PeaceSign = 2,  // front hand up after a landing
    Smoke = 4,      // smoke puff when starting a flip
    Dust = 8,       // armour dust on landing
}

/// <summary>A skater: palette, board, signature grind, and the pieces that make it look different.</summary>
internal abstract class Character
{
    public abstract string Name { get; }
    public abstract Look Look { get; }
    public abstract BoardSpec Board { get; }

    public virtual Grind Signature => Grind.FiftyFifty;
    public virtual Quirks Quirks => Quirks.None;
    public virtual float Speed => 1f;
    public virtual float BailChance => 0.04f;
    public virtual uint SparkColor => 0xFFFFC030;

    /// <summary>Capes, scarves, packs and shields drawn behind the body.</summary>
    public virtual void DrawBack(Gfx g, in Pose p, in Xf body) { }

    public virtual void DrawTorso(Gfx g, in Xf body) => Rig.Torso(g, Look, body);

    public abstract void DrawHead(Gfx g, in Pose p, in Xf body);

    /// <summary>Accents drawn over the body but under the front arm.</summary>
    public virtual void DrawFront(Gfx g, in Pose p, in Xf body) { }

    public static readonly Character[] All =
    {
        new Vader(), new Stormtrooper(), new DoomGuy(), new MasterChief(),
        new BobaFett(), new Neptune(), new Ninja(), new Knight(),
    };

    public static Character Find(string name)
    {
        foreach (Character c in All)
            if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) return c;
        return All[0];
    }
}
