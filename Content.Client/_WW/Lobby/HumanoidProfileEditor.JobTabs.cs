// WW: Misfits' job tabs (No Faction / Minor Factions / Major Factions / Whitelist) for the Jobs tab.
// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs
// (job tab buttons). Changed for Wild Wanamingo: whitelisted jobs (Wizden job whitelist) go on the Whitelist tab.

using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private DepartmentUICategory _jobTab = DepartmentUICategory.NoFaction;
    private (Button Button, DepartmentUICategory Tab)[]? _jobTabs;

    private void InitializeJobTabs()
    {
        if (_jobTabs != null)
            return;

        _jobTabs = new[]
        {
            (JobTabNoFaction, DepartmentUICategory.NoFaction),
            (JobTabMinorFactions, DepartmentUICategory.MinorFaction),
            (JobTabMajorFactions, DepartmentUICategory.MajorFaction),
            (JobTabWhitelist, DepartmentUICategory.Whitelist),
        };

        foreach (var (button, tab) in _jobTabs)
        {
            button.Pressed = tab == _jobTab;
            button.OnPressed += _ =>
            {
                _jobTab = tab;
                foreach (var (other, otherTab) in _jobTabs)
                {
                    other.Pressed = otherTab == tab;
                }

                RefreshJobs();
            };
        }
    }

    private bool IsOnCurrentJobTab(DepartmentPrototype department, JobPrototype job)
    {
        return _jobTab == DepartmentUICategory.Whitelist
            ? job.Whitelisted
            : !job.Whitelisted && department.UiCategory == _jobTab;
    }
}
