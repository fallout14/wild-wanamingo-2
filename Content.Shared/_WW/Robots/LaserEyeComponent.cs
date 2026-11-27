using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._WW.Robots;

/// <summary>
/// WW: the Assaultron's laser eye (a battery gun held in its combat module). Its battery is the charge indicator:
/// it starts empty, using it in hand fills it over <see cref="ChargeDuration"/>, and the next shot turns into a beam
/// that keeps firing until the battery is drained (about <see cref="BeamDuration"/>) while the robot can't move.
/// The beam follows the shooter's cursor (the client sends <see cref="LaserEyeAimEvent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class LaserEyeComponent : Component
{
    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(6.5);

    /// <summary>
    /// Longest the beam can last (the battery normally runs dry about then).
    /// </summary>
    [DataField]
    public TimeSpan BeamDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Wait after a beam before it can charge again.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    /// <summary>
    /// Played once when the beam starts (each pulse also plays the gun's own shot sound).
    /// </summary>
    [DataField]
    public SoundSpecifier? BeamSound = new SoundPathSpecifier("/Audio/Misfits/Weapons/Guns/assaultron_beam.ogg");

    [DataField]
    public LocId ChargeEmote = "assaultron-charge-emote";

    [DataField]
    public LocId FireEmote = "assaultron-beam-fire-emote";

    [DataField, AutoNetworkedField]
    public LaserEyeState State = LaserEyeState.Idle;

    /// <summary>
    /// End of the current phase: charge-up, beam or cooldown.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan PhaseEnd;

    [DataField, AutoNetworkedField]
    public EntityUid? Shooter;

    [DataField]
    public EntityCoordinates? Target;
}

[Serializable, NetSerializable]
public enum LaserEyeState : byte
{
    Idle,
    Charging,
    Charged,
    Firing,
    Cooldown,
}

/// <summary>
/// WW: on a robot while its laser eye beam is firing. It can't move.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LaserEyeFiringComponent : Component;

/// <summary>
/// WW: sent by the shooter's client while the beam fires: where the cursor is, so the beam follows it.
/// </summary>
[Serializable, NetSerializable]
public sealed class LaserEyeAimEvent : EntityEventArgs
{
    public NetEntity Gun;
    public NetCoordinates Coordinates;
}
