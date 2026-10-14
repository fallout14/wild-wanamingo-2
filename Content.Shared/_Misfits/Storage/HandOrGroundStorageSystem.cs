// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Storage/HandOrGroundStorageSystem.cs

using Content.Shared._Misfits.Storage.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._Misfits.Storage;

/// <summary>
/// Restricts marked storage to opening only from hand or ground.
/// </summary>
public sealed partial class HandOrGroundStorageSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandOrGroundStorageComponent, StorageInteractAttemptEvent>(OnStorageInteractAttempt);
    }

    private void OnStorageInteractAttempt(Entity<HandOrGroundStorageComponent> ent, ref StorageInteractAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out _))
            return;

        if (args.User is { } user && _hands.IsHolding(user, ent.Owner))
            return;

        args.Cancelled = true;
    }
}
