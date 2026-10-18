// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Pets/PetCollarHolderComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.Pets;

/// <summary>
/// A pet that wears a collar (an ID card) in its id slot; the collar's name becomes the pet's name.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PetCollarHolderComponent : Component
{
}
