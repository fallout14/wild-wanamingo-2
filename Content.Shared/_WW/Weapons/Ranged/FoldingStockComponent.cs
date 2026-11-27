using Content.Shared.Actions;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Weapons.Ranged;

/// <summary>
/// WW: a gun with a folding stock. Fold it (the held gun's action, or the right-click verb) to make it a size smaller
/// so it fits in a bag, at the cost of a looser spread and harder kick; extend it again for full accuracy. A stowed
/// gun has to be taken out first. The client swaps the gun's base and magazine sprites for their
/// <see cref="FoldedStatePrefix"/> versions (e.g. "folded-base") while folded, where the RSI has them.
/// Replaces the Misfits FoldableStock, which went on Wizden's Foldable and so took over the gun's alt-click.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FoldingStockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Folded;

    [DataField]
    public ProtoId<ItemSizePrototype> FoldedSize = "Normal";

    [DataField]
    public ProtoId<ItemSizePrototype> UnfoldedSize = "Large";

    /// <summary>Folded, the gun's min/max spread and per-shot spread growth are multiplied by this.</summary>
    [DataField]
    public float FoldedSpreadMultiplier = 1.4f;

    /// <summary>Folded, the camera kick is multiplied by this.</summary>
    [DataField]
    public float FoldedRecoilMultiplier = 1.5f;

    [DataField]
    public SoundSpecifier? FoldSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/shotgun_open.ogg");

    [DataField]
    public SoundSpecifier? UnfoldSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/shotgun_close.ogg");

    [DataField]
    public EntProtoId Action = "ActionF14ToggleFoldingStock";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>Sprite states used while folded are this plus the normal state's name.</summary>
    [DataField]
    public string FoldedStatePrefix = "folded-";
}

/// <summary>
/// WW: the held gun's fold/extend stock action. Its own event so it doesn't also toggle a built-in scope.
/// </summary>
public sealed partial class ToggleFoldingStockEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum FoldingStockVisuals : byte
{
    Folded,
}
