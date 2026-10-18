// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Pets/GhostRoleJobComponent.cs

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Pets;

/// <summary>
/// Added to an entity that has a GhostRoleComponent. When a player takes the ghost role, the mind gets a job role
/// with this job, so playtime is tracked under the job's playTimeTracker.
/// </summary>
[RegisterComponent]
public sealed partial class GhostRoleJobComponent : Component
{
    /// <summary>
    /// The job to give the mind. It should have <c>setPreference: false</c> so it doesn't show in character setup.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;
}
