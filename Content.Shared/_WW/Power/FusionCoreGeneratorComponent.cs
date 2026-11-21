using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Power;

/// <summary>
/// A generator that runs on a fusion core in an item slot. It supplies <see cref="SupplyRate"/> watts while the
/// core has charge and drains the core in proportion to the power actually drawn. A spent core stays spent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FusionCoreGeneratorComponent : Component
{
    /// <summary>
    /// The <see cref="Content.Shared.Containers.ItemSlots.ItemSlotsComponent"/> slot holding the core.
    /// </summary>
    [DataField]
    public string Slot = "fusion_core";

    /// <summary>
    /// Watts supplied while a charged core is in.
    /// </summary>
    [DataField]
    public float SupplyRate = 10000f;

    /// <summary>
    /// How long a full core lasts at full output. A partly loaded generator stretches it out
    /// (half the load, twice as long); with no load at all the core doesn't drain.
    /// </summary>
    [DataField]
    public TimeSpan CoreLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// Time since the core was last drained.
    /// </summary>
    [ViewVariables]
    public float Accumulator;
}

[Serializable, NetSerializable]
public enum FusionCoreGeneratorVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum FusionCoreGeneratorState : byte
{
    /// <summary>
    /// No core, or a spent one.
    /// </summary>
    NoCore,

    /// <summary>
    /// A charged core, but nothing is drawing power.
    /// </summary>
    Idle,

    /// <summary>
    /// Supplying power.
    /// </summary>
    Running,
}
