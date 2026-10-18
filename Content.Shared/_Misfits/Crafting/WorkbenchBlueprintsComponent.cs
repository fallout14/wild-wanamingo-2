// Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) workbench blueprints, moved out of their LatheSystem edit
// (Content.Server/Lathe/LatheSystem.cs, AddBlueprintRecipesFromStorage) onto Wizden's BlueprintComponent.

namespace Content.Shared._Misfits.Crafting;

/// <summary>
/// A workbench whose storage holds blueprints: every blueprint (Wizden's <c>Blueprint</c> component) kept in the
/// bench's storage adds its recipes to the bench, and taking it out removes them again.
/// </summary>
[RegisterComponent]
public sealed partial class WorkbenchBlueprintsComponent : Component
{
}
