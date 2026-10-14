// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/SpecialStats/Components/StrengthIgnoreClothingSlowdownComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.SpecialStats.Components;

/// <summary>
/// Allows sufficiently strong characters to ignore this item's clothing speed penalties.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StrengthIgnoreClothingSlowdownComponent : Component
{
    [DataField]
    public int MinimumStrength = 7;

    /// <summary>
    /// Walking speed floor used by strong wearers instead of removing all slowdown.
    /// </summary>
    [DataField]
    public float StrengthWalkModifier = 1f;

    /// <summary>
    /// Sprint speed floor used by strong wearers instead of removing all slowdown.
    /// </summary>
    [DataField]
    public float StrengthSprintModifier = 1f;

    /// <summary>
    /// Applies the configured high-Strength speed floor.
    /// </summary>
    public float ApplyStrengthModifier(float modifier, bool sprint)
    {
        return MathF.Max(modifier, sprint ? StrengthSprintModifier : StrengthWalkModifier);
    }
}
