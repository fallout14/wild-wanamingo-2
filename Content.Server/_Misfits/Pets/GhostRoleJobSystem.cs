// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/Pets/GhostRoleJobSystem.cs

using Content.Shared._Misfits.Pets;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server._Misfits.Pets;

/// <summary>
/// Gives the mind a job role when a player takes a ghost role with <see cref="GhostRoleJobComponent"/>.
/// </summary>
public sealed partial class GhostRoleJobSystem : EntitySystem
{
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedMindSystem _minds = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostRoleJobComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, GhostRoleJobComponent component, MindAddedMessage args)
    {
        if (!_minds.TryGetMind(uid, out var mindId, out var mind))
            return;

        // Don't replace a job the mind already has.
        if (_roles.MindHasRole<JobRoleComponent>((mindId, mind), out _))
            return;

        _roles.MindAddJobRole(mindId, mind, silent: true, jobPrototype: component.Job);
    }
}
