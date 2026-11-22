using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Reflect;

/// <summary>
/// Entities with this component have a chance to reflect projectiles and hitscan shots
/// Uses <c>ItemToggleComponent</c> to control reflection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectComponent : Component
{
    /// <summary>
    /// What we reflect.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// Select in which inventory slots it will reflect.
    /// By default, it will reflect in any inventory position, except pockets.
    /// </summary>
    [DataField]
    public SlotFlags SlotFlags = SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// Is it allowed to reflect while being in hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReflectingInHands = true;

    /// <summary>
    /// Can only reflect when placed correctly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool InRightPlace;

    /// <summary>
    /// Probability for a projectile to be reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReflectProb = 0.25f;

    // WW Change start - Misfits' per-type chance (power armor deflects medium-caliber rounds less often than small ones)
    /// <summary>
    /// Chance per reflect type, used instead of <see cref="ReflectProb"/> when the shot matches one of these types.
    /// </summary>
    [DataField]
    public Dictionary<ReflectType, float> ReflectProbByType = new();
    // WW Change end

    /// <summary>
    /// Projectile spread. The projectile will be randomly rotated within this angle upon being reflected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle Spread = Angle.FromDegrees(45);

    /// <summary>
    /// The sound to play when reflecting.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnReflect = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg", AudioParams.Default.WithVariation(0.05f));
}

/// <summary>
/// Used for both the projectiles being reflected and the entities reflecting. If there is ever overlap between the
/// reflection types, the projectile will be reflected.
/// </summary>
[Flags, Serializable, NetSerializable]
public enum ReflectType : byte
{
    None = 0,
    NonEnergy = 1 << 0,
    Energy = 1 << 1,
    // WW Change start - Misfits' caliber classes, so power armor deflects pistol and intermediate rounds but not heavy ones
    SmallCaliber = 1 << 2, // .22, 9mm, 10mm, .45 ACP
    MediumCaliber = 1 << 3, // 5mm, 5.45, 5.56, 7.62, 12.7
    // WW Change end
}
