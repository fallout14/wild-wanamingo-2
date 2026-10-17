// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Mount/Components/RiderComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Forge.Mountable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiderComponent : Component
{
    /// <summary>
    /// The animal that this character is currently riding on
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Mount;
}
