// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/Overlays/Switchable/
// SwitchableOverlayComponent.cs, ThermalVisionComponent.cs (Misfits' copy of Goobstation's switchable overlays).
// WW: only thermal vision is ported (Wizden has its own night vision); the base class is folded in.

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Overlays.Legacy;

/// <summary>
/// Lets the entity (or, with <see cref="IsEquipment"/>, its wearer) see living creatures through walls.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionComponent : Component
{
    [DataField]
    public bool IsActive;

    /// <summary>
    /// Whether it grants the wearer vision (equipment) or is the mob's own vision.
    /// </summary>
    [DataField]
    public bool IsEquipment;

    /// <summary>
    /// If greater than 0, the vision isn't toggled but pulsed for this many seconds instead.
    /// </summary>
    [DataField]
    public float PulseTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public float PulseAccumulator;

    [DataField]
    public Color Color = Color.FromHex("#F84742");

    /// <summary>
    /// Radius of the faint client-side light thermal vision gives.
    /// </summary>
    [DataField]
    public float LightRadius = 5f;

    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/Misfits/Legacy/Items/Goggles/activate.ogg");

    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/Misfits/Legacy/Items/Goggles/deactivate.ogg");

    [DataField]
    public EntProtoId? ToggleAction = "ToggleThermalVision";

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

public sealed partial class ToggleThermalVisionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class ThermalVisionComponentState : IComponentState
{
    public Color Color;
    public bool IsActive;
    public SoundSpecifier? ActivateSound;
    public SoundSpecifier? DeactivateSound;
    public EntProtoId? ToggleAction;
    public float LightRadius;
}

/// <summary>
/// Raised on the vision source when it turns on or off; <see cref="User"/> is whose view changes.
/// </summary>
[ByRefEvent]
public record struct SwitchableOverlayToggledEvent(EntityUid User, bool Activated);
