// Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) workbench blueprints, moved out of their LatheSystem edit
// (Content.Server/Lathe/LatheSystem.cs, AddBlueprintRecipesFromStorage) onto Wizden's BlueprintComponent.

using Content.Server._WW.Recipes;
using Content.Server.Lathe;
using Content.Shared._Misfits.Crafting;
using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.Crafting;

/// <summary>
/// Blueprints kept in a workbench's storage add their recipes to the bench while they stay there.
/// </summary>
public sealed partial class WorkbenchBlueprintsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Before the disabled-recipes list, so a disabled recipe stays hidden even on a blueprint.
        SubscribeLocalEvent<WorkbenchBlueprintsComponent, LatheGetRecipesEvent>(OnGetRecipes,
            after: [typeof(LatheSystem)], before: [typeof(DisabledLatheRecipesSystem)]);
        SubscribeLocalEvent<WorkbenchBlueprintsComponent, EntInsertedIntoContainerMessage>(OnStorageChanged);
        SubscribeLocalEvent<WorkbenchBlueprintsComponent, EntRemovedFromContainerMessage>(OnStorageChanged);
    }

    private void OnGetRecipes(EntityUid uid, WorkbenchBlueprintsComponent component, LatheGetRecipesEvent args)
    {
        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        foreach (var item in storage.Container.ContainedEntities)
        {
            if (!TryComp<BlueprintComponent>(item, out var blueprint))
                continue;

            foreach (var recipe in blueprint.ProvidedRecipes)
            {
                // A stale recipe id on an old blueprint mustn't break the bench's menu.
                if (_proto.HasIndex(recipe))
                    args.Recipes.Add(recipe);
            }
        }
    }

    private void OnStorageChanged(EntityUid uid, WorkbenchBlueprintsComponent component, ContainerModifiedMessage args)
    {
        if (!TryComp<StorageComponent>(uid, out var storage) || args.Container != storage.Container ||
            !HasComp<BlueprintComponent>(args.Entity))
            return;

        // Same refresh Wizden's blueprint slot uses: the lathe rebuilds its recipe list and UI.
        var ev = new TechnologyDatabaseModifiedEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
