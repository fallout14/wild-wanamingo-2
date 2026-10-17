// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/VaultDoorConsole/VaultDoorPendingBoltComponent.cs

namespace Content.Server._Misfits.VaultDoorConsole;

[RegisterComponent, Access(typeof(VaultDoorConsoleSystem))]
public sealed partial class VaultDoorPendingBoltComponent : Component
{
    public EntityUid Console;
}
