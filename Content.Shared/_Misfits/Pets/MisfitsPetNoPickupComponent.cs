// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Pets/MisfitsPetNoPickupComponent.cs

namespace Content.Shared._Misfits.Pets;

/// <summary>
/// Marks a player-controlled pet mob as unable to pick up items into hand slots.
/// The pet can still drag/pull other entities via the Puller component.
/// </summary>
[RegisterComponent]
public sealed partial class MisfitsPetNoPickupComponent : Component
{
}
