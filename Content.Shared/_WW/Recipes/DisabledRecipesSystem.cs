using Content.Shared.Construction.Prototypes;
using Content.Shared.Kitchen;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._WW.Recipes;

/// <summary>
/// Applies every <see cref="DisabledRecipesPrototype"/>: hides crafting-menu recipes and drops microwave recipes.
/// Workbench recipes are filtered where lathes gather their recipes (server).
/// </summary>
public sealed partial class DisabledRecipesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RecipeSystem _recipes = default!;

    private readonly HashSet<ProtoId<ConstructionPrototype>> _construction = new();
    private readonly HashSet<ProtoId<LatheRecipePrototype>> _lathe = new();
    private readonly HashSet<ProtoId<MicrowaveMealRecipePrototype>> _microwave = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded, after: [typeof(RecipeSystem)]);
        Reload();
    }

    public bool IsDisabled(ProtoId<LatheRecipePrototype> recipe) => _lathe.Contains(recipe);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<DisabledRecipesPrototype>()
            || args.WasModified<ConstructionPrototype>()
            || args.WasModified<MicrowaveMealRecipePrototype>())
        {
            Reload();
        }
    }

    private void Reload()
    {
        // A recipe taken off the list comes back.
        foreach (var id in _construction)
        {
            if (_proto.TryIndex(id, out var proto))
                proto.Hide = false;
        }

        _construction.Clear();
        _lathe.Clear();
        _microwave.Clear();
        foreach (var list in _proto.EnumeratePrototypes<DisabledRecipesPrototype>())
        {
            _construction.UnionWith(list.Construction);
            _lathe.UnionWith(list.Lathe);
            _microwave.UnionWith(list.Microwave);
        }

        foreach (var id in _construction)
        {
            if (_proto.TryIndex(id, out var proto))
                proto.Hide = true;
        }

        _recipes.Recipes.RemoveAll(r => _microwave.Contains(r.ID));
    }
}
