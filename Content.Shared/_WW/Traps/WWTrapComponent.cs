using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Traps;

/// <summary>
/// A wasteland trap (bear trap, tripwire, mine). Set it on the floor and use the "Arm" verb (alt-click); an armed
/// trap is anchored, can't be picked up, and fires its triggers (Wizden trigger effects, target = whoever stepped on
/// it) when someone steps on it. "Disarm" makes it safe to pick up. Reusable traps disarm themselves when they go
/// off; single-use ones delete themselves through their trigger effects (e.g. ExplodeOnTrigger).
/// Mobs with <see cref="LightStepComponent"/> never set traps off.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WWTrapComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Armed;

    [DataField]
    public TimeSpan ArmDelay = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan DisarmDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// False for single-use traps: once fired they can't be armed again (they normally delete themselves anyway).
    /// </summary>
    [DataField]
    public bool Rearmable = true;

    /// <summary>
    /// Set once a single-use trap has fired.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Spent;

    /// <summary>
    /// Clamp the victim with this trap's <see cref="Content.Shared.Ensnaring.Components.EnsnaringComponent"/>
    /// (bear trap): they can't walk until they or someone else pries it off.
    /// </summary>
    [DataField]
    public bool EnsnareVictim;

    [DataField]
    public SoundSpecifier? ArmSound = new SoundPathSpecifier("/Audio/Weapons/click.ogg");
}

[Serializable, NetSerializable]
public enum WWTrapVisuals : byte
{
    Armed,
}

[Serializable, NetSerializable]
public sealed partial class WWTrapDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// True to arm, false to disarm.
    /// </summary>
    public bool Arm;

    public WWTrapDoAfterEvent(bool arm)
    {
        Arm = arm;
    }

    public override DoAfterEvent Clone() => this;
}
