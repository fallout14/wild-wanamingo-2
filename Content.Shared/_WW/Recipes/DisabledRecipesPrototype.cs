using Content.Shared.Construction.Prototypes;
using Content.Shared.Kitchen;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._WW.Recipes;

/// <summary>
/// Recipes switched off for Wild Wanamingo, Wizden's or ours, without editing the recipes themselves.
/// Any number of these prototypes may exist; everything they list is disabled.
/// </summary>
[Prototype]
public sealed partial class DisabledRecipesPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Crafting-menu (construction) recipes: hidden from the menu.
    /// </summary>
    [DataField]
    public List<ProtoId<ConstructionPrototype>> Construction = new();

    /// <summary>
    /// Workbench / lathe recipes: never offered by any lathe, so they can't be queued.
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> Lathe = new();

    /// <summary>
    /// Microwave / cooking recipes: never cooked.
    /// </summary>
    [DataField]
    public List<ProtoId<MicrowaveMealRecipePrototype>> Microwave = new();
}
