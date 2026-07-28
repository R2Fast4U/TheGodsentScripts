/// <summary>
/// Core movement kit. Unlockable (for metroidvania gating), but PlayerStats can treat them
/// as always-available via its "all base abilities unlocked" flag.
/// </summary>
public enum BaseAbility
{
    Move,
    Jump,
    WallGrab,
    WallClimb,
    WallSlide,
    LedgeClimb
}
