using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

// ReSharper disable once CheckNamespace
namespace Content.Server.Players.JobWhitelist;

/// <summary>
/// WW: species whitelists. A whitelisted species (<see cref="SpeciesPrototype.Whitelisted"/>) is stored in the job
/// whitelist table under its species ID, so it loads, syncs to the client and persists exactly like a job whitelist.
/// </summary>
public sealed partial class JobWhitelistManager
{
    public bool IsSpeciesAllowed(NetUserId player, ProtoId<SpeciesPrototype> species)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_prototypes.TryIndex(species, out var speciesPrototype) || !speciesPrototype.Whitelisted)
            return true;

        return _whitelists.TryGetValue(player, out var whitelists) && whitelists.Contains(species.Id);
    }

    public void AddSpeciesWhitelist(NetUserId player, ProtoId<SpeciesPrototype> species)
    {
        AddWhitelist(player, new ProtoId<JobPrototype>(species.Id));
    }

    public void RemoveSpeciesWhitelist(NetUserId player, ProtoId<SpeciesPrototype> species)
    {
        RemoveWhitelist(player, new ProtoId<JobPrototype>(species.Id));
    }
}
