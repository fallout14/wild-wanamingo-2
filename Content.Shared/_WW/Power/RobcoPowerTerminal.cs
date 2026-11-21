using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Power;

/// <summary>
/// A RobCo terminal that lists the generators, storage units, substations and breakers within <see cref="Range"/>,
/// on a green-screen window. The wasteland's stand-in for Wizden's power monitoring console: it reads only what's
/// nearby instead of the whole grid (on a planet map that's the entire map).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RobcoPowerTerminalComponent : Component
{
    /// <summary>
    /// Tiles around the terminal it can see.
    /// </summary>
    [DataField]
    public float Range = 32f;

    [ViewVariables]
    public float Accumulator;
}

[Serializable, NetSerializable]
public enum RobcoPowerTerminalUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum RobcoPowerDeviceKind : byte
{
    Generator,
    Storage,
    Substation,
    Breaker,
}

[Serializable, NetSerializable]
public sealed class RobcoPowerDevice
{
    public string Name = string.Empty;
    public RobcoPowerDeviceKind Kind;

    /// <summary>
    /// Watts going out: a generator's supply, or a battery's discharge.
    /// </summary>
    public float Output;

    /// <summary>
    /// Watts a generator can supply right now, or the most a battery can discharge.
    /// </summary>
    public float Capacity;

    /// <summary>
    /// Watts a battery is charging at.
    /// </summary>
    public float Input;

    /// <summary>
    /// Battery charge, 0 to 1. Null for generators.
    /// </summary>
    public float? Charge;

    /// <summary>
    /// Fuel left, 0 to 1, for generators that report it (fusion cores). Null otherwise.
    /// </summary>
    public float? Fuel;

    public int Distance;
    public Direction Direction;
}

[Serializable, NetSerializable]
public sealed class RobcoPowerTerminalState(List<RobcoPowerDevice> devices, float range) : BoundUserInterfaceState
{
    public readonly List<RobcoPowerDevice> Devices = devices;
    public readonly float Range = range;
}
