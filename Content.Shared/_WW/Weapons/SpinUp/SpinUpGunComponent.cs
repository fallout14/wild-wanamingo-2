using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._WW.Weapons.SpinUp;

/// <summary>
/// Rotary guns (miniguns, the laser gatling): holding the trigger spins the barrels up for <see cref="SpinUpTime"/>
/// before the first shot, and they spin down <see cref="SpinDownDelay"/> after you let go. While spinning the holder is
/// slowed by <see cref="SpinningSpeedModifier"/>, on top of the gun's own held slowdown (HeldSpeedModifier).
/// Replaces Misfits' MinigunToggle, where you had to switch the spin on by hand (2026-11-22 playtest).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SpinUpGunComponent : Component
{
    /// <summary>
    /// How long the barrels take to spin up before the first shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpinUpTime = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// How long after the trigger is released the barrels stop.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SpinDownDelay = TimeSpan.FromSeconds(0.4);

    /// <summary>
    /// Movement speed multiplier for the holder while the barrels spin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpinningSpeedModifier = 0.4f;

    [DataField]
    public SoundSpecifier? SpinUpSound = new SoundPathSpecifier("/Audio/Misfits/Legacy/Weapons/spinon.ogg");

    [DataField]
    public SoundSpecifier? SpinDownSound = new SoundPathSpecifier("/Audio/Misfits/Legacy/Weapons/spinoff.ogg");

    [AutoNetworkedField]
    public bool Spinning;

    /// <summary>
    /// Who is spinning it (so their speed can be put back when it stops, even after dropping it).
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Spinner;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan SpinStart;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan LastTrigger;
}
