using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._WW.Species;

/// <summary>
/// WW: a Protectron that hasn't picked its module yet. Players pick the plain Protectron in the lobby; once in game
/// they get one action per module (police, builder, fire, tribal). Using one turns them into that model for good
/// (a forced, permanent polymorph); the new body has no module actions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ProtectronModuleComponent : Component
{
    /// <summary>
    /// Action prototypes to grant, each raising <see cref="ProtectronModuleActionEvent"/>.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Actions = new();

    [DataField]
    public List<EntityUid> GrantedActions = new();
}

public sealed partial class ProtectronModuleActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}
