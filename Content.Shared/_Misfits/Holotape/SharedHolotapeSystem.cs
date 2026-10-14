// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Holotape/SharedHolotapeSystem.cs
// WW: trimmed to the holotape/terminal reader. Misfits' terminal notes, faction database, device-link and Overwatch
// tabs are not ported.

using Robust.Shared.Serialization;

namespace Content.Shared._Misfits.Holotape;

/// <summary>
/// UI key for the green-on-black holotape/terminal viewer.
/// </summary>
[Serializable, NetSerializable]
public enum HolotapeUiKey : byte
{
    Key
}

/// <summary>
/// The title and body text to display.
/// </summary>
[Serializable, NetSerializable]
public sealed class HolotapeBoundUserInterfaceState(string title, string content, bool isHolotapeItem)
    : BoundUserInterfaceState
{
    /// <summary>
    /// Display title shown at the top of the viewer (e.g. "OVERSEER'S LOG #3").
    /// </summary>
    public readonly string Title = title;

    /// <summary>
    /// Body content in BB code, rendered in green monospace text.
    /// </summary>
    public readonly string Content = content;

    /// <summary>
    /// True for a holotape played in a reader or Pip-Boy, false for a terminal's own entry.
    /// </summary>
    public readonly bool IsHolotapeItem = isHolotapeItem;
}
