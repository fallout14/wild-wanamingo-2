using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Species;

/// <summary>
/// WW: a Protectron that hasn't picked its module yet. Players pick the plain Protectron in the lobby; once in game
/// a chassis-selection window (like the cyborg type picker) offers the modules (police, builder, fire, tribal).
/// Confirming one turns them into that model for good (a forced, permanent polymorph); the new body has no picker.
/// The window pops up when a player takes the body over, and the select-module action reopens it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProtectronModuleComponent : Component
{
    /// <summary>
    /// The modules on offer, in menu order.
    /// </summary>
    [DataField(required: true)]
    public List<ProtectronModuleOption> Modules = new();

    [DataField]
    public EntProtoId SelectAction = "ActionF14ProtectronSelectModule";

    [DataField]
    public EntityUid? SelectActionEntity;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ProtectronModuleOption
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// What the menu shows for this module (the finished model).
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Dummy;

    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}

/// <summary>
/// WW: opens (or closes) the module picker.
/// </summary>
public sealed partial class ProtectronSelectModuleEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum ProtectronModuleUiKey : byte
{
    Key,
}

/// <summary>
/// WW: confirmed option <see cref="Index"/> of <see cref="ProtectronModuleComponent.Modules"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class ProtectronModuleSelectMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
