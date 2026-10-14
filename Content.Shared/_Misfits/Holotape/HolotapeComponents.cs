// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Holotape/
// HolotapeReaderComponent.cs, HolotapeDataComponent.cs, TerminalRandomContentComponent.cs

using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Holotape;

/// <summary>
/// Marks an entity as capable of reading holotapes via InteractUsing (terminals, Pip-Boys).
/// </summary>
[RegisterComponent]
public sealed partial class HolotapeReaderComponent : Component
{
}

/// <summary>
/// Holds the title and body text for a holotape or terminal entry.
/// Both fields are treated as FTL locale keys and resolved on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class HolotapeDataComponent : Component
{
    /// <summary>
    /// FTL key for the display title. Resolved to localized string on MapInit.
    /// </summary>
    [DataField]
    public string Title = string.Empty;

    /// <summary>
    /// FTL key for the body content (BB code). Resolved to localized string on MapInit.
    /// </summary>
    [DataField]
    public string Content = string.Empty;

    /// <summary>
    /// Tracks whether locale resolution has already been applied.
    /// </summary>
    [DataField]
    public bool Localized;
}

/// <summary>
/// On MapInit, picks a random entry from <see cref="ContentPool"/> datasets and writes it into the entity's
/// <see cref="HolotapeDataComponent"/>. Each dataset value is an FTL key prefix; "-title" and "-content" are appended.
/// </summary>
[RegisterComponent]
public sealed partial class TerminalRandomContentComponent : Component
{
    [DataField]
    public List<ProtoId<DatasetPrototype>> ContentPool = new();
}
