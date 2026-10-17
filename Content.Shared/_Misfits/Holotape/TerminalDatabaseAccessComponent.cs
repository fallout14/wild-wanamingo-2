// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Holotape/TerminalDatabaseAccessComponent.cs

using Robust.Shared.GameObjects;

namespace Content.Shared._Misfits.Holotape;

/// <summary>
/// Marks an entity as an authorized gateway to the faction terminal databases.
/// The database itself is still selected from the viewer's access; this component
/// only scopes database BUI messages to real terminals instead of every entity with
/// <see cref="HolotapeDataComponent"/>.
/// </summary>
[RegisterComponent]
public sealed partial class TerminalDatabaseAccessComponent : Component;
