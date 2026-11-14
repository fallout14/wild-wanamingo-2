using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Weapons.Scopes;

/// <summary>
/// A rifle scope. Wield the gun and use the scope action: after a Perception-scaled do-after the view is unlocked and
/// follows your cursor (Wizden's binocular/Hristov cursor offset). Higher magnification lets the view go further ahead.
/// Put it on a scope item that fits a gun's optic slot, or on a gun with a built-in scope. With more than one
/// magnification, a verb or the cycle action switches between them (variable scope).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WWScopeComponent : Component
{
    /// <summary>Magnifications this scope can be set to, e.g. [4] or [2, 4, 6].</summary>
    [DataField(required: true), AutoNetworkedField]
    public List<float> Magnifications = new();

    [DataField, AutoNetworkedField]
    public int Index;

    /// <summary>Scoped in: the view follows the cursor while the gun stays wielded.</summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>The scope action, for guns with a built-in scope (attached scopes use the gun's optic action).</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>Scope-in time at Perception 5: BaseDelay + DelayPerMagnification x magnification (seconds).</summary>
    [DataField]
    public float BaseDelay = 0.4f;

    [DataField]
    public float DelayPerMagnification = 0.1f;

    /// <summary>How far ahead (in tiles) the view can go per 1x of magnification.</summary>
    [DataField]
    public float TilesPerMagnification = 2f;

    public float Magnification => Magnifications.Count == 0 ? 1f : Magnifications[Math.Clamp(Index, 0, Magnifications.Count - 1)];

    public float MaxOffset => Magnification * TilesPerMagnification;

    /// <summary>Extra view range for the server, so things at the edge of the scoped view are sent to the client.</summary>
    public float PvsIncrease => MaxOffset / 24f;
}

[Serializable, NetSerializable]
public sealed partial class WWScopeDoAfterEvent : SimpleDoAfterEvent;
