using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._WW.Stealth;

/// <summary>
/// WW: a Stealth Boy. It cloaks the user the way the Chinese stealth suit does (Stealth at <see cref="Visibility"/>).
/// One-use kind (<see cref="UsedPrototype"/> set): use it to cloak for <see cref="Duration"/>; it turns into the
/// spent one. Rechargeable kind (the Assaultron's, needs a Battery, which is its charge indicator): use it to switch
/// the cloak on or off at will; it drains <see cref="DrainRate"/> a second while on and refills
/// <see cref="RegenRate"/> a second while off.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StealthBoyComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How see-through the user gets (the Chinese stealth suit uses 0.1).
    /// </summary>
    [DataField]
    public float Visibility = 0.1f;

    /// <summary>
    /// What a one-use Stealth Boy turns into once used. Null means it is the rechargeable kind.
    /// </summary>
    [DataField]
    public EntProtoId? UsedPrototype;

    [DataField]
    public float DrainRate = 5f;

    [DataField]
    public float RegenRate = 1f;

    /// <summary>
    /// Charge needed to switch a rechargeable one on.
    /// </summary>
    [DataField]
    public float MinCharge = 10f;

    /// <summary>
    /// A rechargeable one that is currently cloaking someone.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/MojaveSun/Items/stealthboy_activate.ogg");

    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/MojaveSun/Items/stealthboy_deactivate.ogg");
}

/// <summary>
/// WW: on someone cloaked by a Stealth Boy, until <see cref="EndTime"/> (one-use) or until the rechargeable
/// <see cref="Source"/> is switched off, runs dry or leaves their hands.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class StealthBoyCloakedComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// False if they were already stealthed (a stealth suit) and the Stealth Boy must leave that alone.
    /// </summary>
    [DataField]
    public bool AddedStealth;

    [DataField]
    public EntityUid? Source;

    [DataField]
    public SoundSpecifier? DeactivateSound;
}
