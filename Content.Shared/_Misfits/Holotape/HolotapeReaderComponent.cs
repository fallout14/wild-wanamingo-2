// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Holotape/HolotapeReaderComponent.cs

using Robust.Shared.GameObjects;

// #Misfits Add - Marker component for entities that can play holotapes (terminals, Pip-Boys).

namespace Content.Shared._Misfits.Holotape;

/// <summary>
/// Marks an entity as capable of reading holotapes via InteractUsing.
/// Add to computer terminals and Pip-Boys.
/// </summary>
[RegisterComponent]
public sealed partial class HolotapeReaderComponent : Component
{
}
