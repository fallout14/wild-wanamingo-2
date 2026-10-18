// Ported from Goobstation content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// the drawing part of Content.Shared/_Goobstation/Vehicles/SharedVehicleComponent.cs (renderOver, overlayPrototype)
// WW Change - riding itself is Wizden's VehicleComponent; this only keeps Goob's vehicle sprites.

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Vehicles;

/// <summary>
/// Vehicle sprites from Goob's vehicles: the vehicle draws over its rider when facing one of <see cref="RenderOver"/>,
/// an optional overlay entity (a fairing or roll cage) is drawn over the rider while ridden, and the
/// <see cref="VehicleVisualLayers.AutoAnimate"/> layer only animates while the vehicle can run.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleDrawOverComponent : Component
{
    [DataField, AutoNetworkedField]
    public VehicleRenderOver RenderOver = VehicleRenderOver.None;

    /// <summary>
    /// Spawned on the vehicle while it has a rider, drawn over mobs.
    /// </summary>
    [DataField]
    public EntProtoId? OverlayPrototype;

    [ViewVariables]
    public EntityUid? ActiveOverlay;
}

[Flags]
public enum VehicleRenderOver
{
    None = 0,
    North = 1,
    NorthEast = 2,
    East = 4,
    SouthEast = 8,
    South = 16,
    SouthWest = 32,
    West = 64,
    NorthWest = 128,
}

[Serializable, NetSerializable]
public enum VehicleVisualLayers : byte
{
    AutoAnimate,
    Cover,
}
