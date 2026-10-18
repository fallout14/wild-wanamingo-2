// Ported from Goobstation content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Shared/_Goobstation/Vehicles/MotorbikeComponent.cs
// WW Change - runs on Wizden's VehicleComponent instead of Goob's vehicle system.

using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Vehicles;

/// <summary>
/// A vehicle that burns welding fuel while ridden with its key in, is refuelled by hand with the key out, and catches
/// fire and explodes once it takes too much damage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MotorbikeComponent : Component
{
    [DataField]
    public string FuelSolution = "motorbikeFuel";

    [DataField]
    public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

    [DataField]
    public FixedPoint2 FuelUsePerSecond = FixedPoint2.New(0.13f);

    [DataField]
    public FixedPoint2 RefillAmount = FixedPoint2.New(200f);

    /// <summary>
    /// Total damage at which the motorbike catches fire.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxIntegrity = FixedPoint2.New(280f);

    [DataField]
    public TimeSpan RefillDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long a burning motorbike takes to explode.
    /// </summary>
    [DataField]
    public TimeSpan ExplosionDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public string ExplosionType = "Default";

    [DataField]
    public float ExplosionTotalIntensity = 10f;

    [DataField]
    public float ExplosionSlope = 10f;

    [DataField]
    public float ExplosionMaxTileIntensity = 5f;

    [DataField]
    public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField]
    public SoundSpecifier FuseSound = new SoundPathSpecifier("/Audio/Nuclear14/Weapons/Effects/Wpn_dynam_fuse.ogg");

    [ViewVariables]
    public float FuelAccumulator;

    [ViewVariables]
    public DoAfterId? RefuelDoAfter;

    [ViewVariables, AutoNetworkedField]
    public bool Burning;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan? ExplodeAt;
}

[Serializable, NetSerializable]
public enum MotorbikeVisuals : byte
{
    Burning,
}

[Serializable, NetSerializable]
public sealed partial class MotorbikeRefuelDoAfterEvent : SimpleDoAfterEvent;
