// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Storage/Components/HandOrGroundStorageComponent.cs

namespace Content.Shared._Misfits.Storage.Components;

/// <summary>
/// Marks storage that can only open while held by the opener or lying on the ground.
/// </summary>
[RegisterComponent]
public sealed partial class HandOrGroundStorageComponent : Component
{
}
