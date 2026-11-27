using Robust.Shared.GameStates;

namespace Content.Shared._WW.Weapons.Attachments;

/// <summary>
/// WW: a muzzle attachment that vents gas upward to fight muzzle climb. While fitted to a gun's muzzle slot the
/// spread grows more slowly shot to shot (so sustained and automatic fire stays tighter for longer) and the camera
/// kicks less. The gun's minimum and maximum spread are unchanged. It goes in the same slot as a suppressor or bayonet,
/// so it's one or the other. Applied by the Misfits SharedFirearmAttachmentSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecoilCompensatorAttachmentComponent : Component
{
    /// <summary>Multiplies the gun's per-shot spread growth (AngleIncrease).</summary>
    [DataField, AutoNetworkedField]
    public float AngleIncreaseMultiplier = 0.6f;

    /// <summary>Multiplies the camera kick.</summary>
    [DataField, AutoNetworkedField]
    public float CameraRecoilMultiplier = 0.6f;
}
