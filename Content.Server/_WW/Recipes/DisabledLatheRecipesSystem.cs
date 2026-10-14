using Content.Server.Lathe;
using Content.Shared._WW.Recipes;
using Content.Shared.Lathe;

namespace Content.Server._WW.Recipes;

/// <summary>
/// No workbench offers a recipe on a <see cref="DisabledRecipesPrototype"/> list, so it can't be queued either.
/// </summary>
public sealed partial class DisabledLatheRecipesSystem : EntitySystem
{
    [Dependency] private DisabledRecipesSystem _disabled = default!;

    public override void Initialize()
    {
        base.Initialize();
        // After research and emag recipes are added.
        SubscribeLocalEvent<LatheComponent, LatheGetRecipesEvent>(OnGetRecipes, after: [typeof(LatheSystem)]);
    }

    private void OnGetRecipes(Entity<LatheComponent> ent, ref LatheGetRecipesEvent args)
    {
        args.Recipes.RemoveWhere(_disabled.IsDisabled);
    }
}
