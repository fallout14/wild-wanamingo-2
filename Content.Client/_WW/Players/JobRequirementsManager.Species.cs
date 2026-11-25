using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;

// ReSharper disable once CheckNamespace
namespace Content.Client.Players.PlayTimeTracking;

/// <summary>
/// WW: species whitelists ride along with the job whitelists (stored under the species ID).
/// </summary>
public sealed partial class JobRequirementsManager
{
    public bool IsSpeciesAllowed(SpeciesPrototype species)
    {
        if (!species.Whitelisted || !_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        return _jobWhitelists.Contains(species.ID);
    }
}
