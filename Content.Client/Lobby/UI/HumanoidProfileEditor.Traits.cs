using System.Linq;
using Content.Shared._WW.Traits;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

// WW Change: the Traits tab is Misfits' talent tree (Content.Client/_Misfits/Lobby/TalentTreeView.cs). Every trait is a
// node in its category's column, picks draw on one point pool (TraitTree.PointBudget) and a node needs its
// prerequisite. Wizden's per-category caps (speech traits) still apply.
public sealed partial class HumanoidProfileEditor
{
    private bool _talentTreeHooked;

    /// <summary>
    /// Refreshes traits selector
    /// </summary>
    public void RefreshTraits()
    {
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (!_talentTreeHooked)
        {
            TalentTree.NodePressed += OnTalentTreeNodePressed;
            _talentTreeHooked = true;
        }

        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().ToList();
        var owned = Profile?.TraitPreferences ?? new HashSet<ProtoId<TraitPrototype>>();

        // One column per category, uncategorised traits last.
        var branches = traits
            .GroupBy(t => t.Category?.Id)
            .Select(group =>
            {
                var name = group.Key != null && _prototypeManager.TryIndex<TraitCategoryPrototype>(group.Key, out var category)
                    ? Loc.GetString(category.Name)
                    : Loc.GetString("humanoid-profile-editor-traits-other");

                var perks = group
                    .OrderBy(t => t.Tier)
                    .ThenBy(t => Loc.GetString(t.Name))
                    .Select(t => new TreePerk(
                        t.ID,
                        Loc.GetString(t.Name),
                        -t.Cost,
                        t.Tier,
                        t.Prerequisite?.Id,
                        t.Description is { } desc ? Loc.GetString(desc) : null))
                    .ToList();

                return (Key: group.Key, Branch: new TreeBranch(name, perks));
            })
            .OrderBy(b => b.Key == null)
            .ThenBy(b => b.Key != "F14Perks")
            .ThenBy(b => b.Branch.CategoryName)
            .Select(b => b.Branch)
            .ToList();

        TalentTree.SetTrees(branches);

        // Usable = could be taken right now (prerequisite owned, fits the pool and its category's cap).
        var usable = new HashSet<string>();
        foreach (var trait in traits)
        {
            if (owned.Contains(trait.ID) ||
                Profile?.WithTraitPreference(trait.ID, _prototypeManager).TraitPreferences.Contains(trait.ID) == true)
            {
                usable.Add(trait.ID);
            }
        }

        TalentTree.RefreshStates(owned.Select(t => t.Id).ToHashSet(), usable);

        var spent = TraitTree.SpentPoints(owned, _prototypeManager);
        var left = TraitTree.PointBudget - spent;
        TraitPointsLabel.Text = Loc.GetString("humanoid-profile-editor-trait-points", ("left", left), ("max", TraitTree.PointBudget));
        TraitPointsBar.MaxValue = TraitTree.PointBudget;
        TraitPointsBar.Value = Math.Clamp(left, 0, TraitTree.PointBudget);
    }

    private void OnTalentTreeNodePressed(string traitId, bool owned)
    {
        if (Profile == null)
            return;

        Profile = owned
            ? Profile.WithTraitPreference(traitId, _prototypeManager)
            : Profile.WithoutTraitPreference(traitId, _prototypeManager);

        SetDirty();
        RefreshTraits();
    }
}
