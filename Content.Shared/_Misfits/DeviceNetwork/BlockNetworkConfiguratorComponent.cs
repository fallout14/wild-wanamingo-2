// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Server/DeviceNetwork/Components/BlockNetworkConfiguratorComponent.cs (a Misfits addition in Wizden's folder)

namespace Content.Shared._Misfits.DeviceNetwork;

/// <summary>
/// Players can't link this device with a network configurator or multitool, except from the allowed source
/// prototypes (e.g. a vault door's own console).
/// </summary>
[RegisterComponent]
public sealed partial class BlockNetworkConfiguratorComponent : Component
{
    [DataField]
    public HashSet<string> AllowedSourcePrototypes = new();
}
