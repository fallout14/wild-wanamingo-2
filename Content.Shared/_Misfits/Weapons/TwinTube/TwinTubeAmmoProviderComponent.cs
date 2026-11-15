// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/TwinTube/TwinTubeAmmoProviderComponent.cs
// Changed for Wild Wanamingo: a one-shell chamber in front of the two tubes (the Neostead's 6+6+1), and alt-click
// switches tubes.

using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.Weapons.TwinTube;

/// <summary>
/// Two fixed magazine tubes and a one-shell chamber, each a contained entity with its own
/// <see cref="Content.Shared.Weapons.Ranged.Components.BallisticAmmoProviderComponent"/>, so each keeps its own
/// ammunition type, order and capacity. Firing uses the chamber first, then the selected tube, switching to the other
/// tube when the selected one is empty. Loading fills the selected tube, then the chamber.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TwinTubeAmmoProviderComponent : Component
{
    public const string LeftTube = "left_tube";
    public const string RightTube = "right_tube";
    public const string ChamberTube = "chamber_tube";

    [DataField, AutoNetworkedField]
    public bool RightSelected;
}
