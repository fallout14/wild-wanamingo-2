// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_NC/Trade/Store/Runtime/NcStoreLogicSystem.Core.cs

using Content.Shared.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Misfits.Special;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Forge.Trade;


public sealed partial class NcStoreLogicSystem : EntitySystem
{
    private ISawmill Sawmill => Log; // WW: per-system logger (a static one outlived its server in tests)
    private static readonly IComparer<string> OrdinalIds = new OrdinalIdComparer();

    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] public NcStoreCurrencySystem _currency = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] public IEntityManager _ents = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] public NcStoreInventorySystem _inventory = default!;
    [Dependency] public IPrototypeManager _protos = default!;
    [Dependency] private SharedSpecialSystem _special = default!;
    [Dependency] public SharedStackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeServices();
    }
    public void InvalidateInventoryCache(EntityUid root) => _inventory.InvalidateInventoryCache(root);

    public void QueuePickupToHandsOrCrateNextTick(EntityUid user, EntityUid spawned)
    {
        Timer.Spawn(0, () =>
        {
            if (!Exists(user) || !Exists(spawned))
                return;

            if (_ents.TryGetComponent(spawned, out TransformComponent? xform) && xform.ParentUid == user)
            {
                InvalidateInventoryCache(user);
                return;
            }

            var pickedUp = false;
            if (_ents.HasComponent<HandsComponent>(user))
                pickedUp = _hands.TryPickupAnyHand(user, spawned, false);

            if (pickedUp)
            {
                InvalidateInventoryCache(user);
                return;
            }

            if (TryGetPulledClosedCrate(user, out var crate) && Exists(crate))
            {
                _entityStorage.Insert(spawned, crate);
                InvalidateInventoryCache(crate);
            }

            InvalidateInventoryCache(user);
        });
    }

    public EntityUid? GetPulledClosedCrate(EntityUid user) =>
        TryGetPulledClosedCrate(user, out var crate) ? crate : null;

    public bool TryGetPulledClosedCrate(EntityUid user, out EntityUid crate)
    {
        crate = default;
        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands))) // WW: Wizden hands rework
            {
                if (TryComp<EntityStorageComponent>(held, out var storage) && !storage.Open)
                {
                    crate = held;
                    return true;
                }
            }
        }

        if (!TryComp(user, out PullerComponent? puller) || puller.Pulling is not { } pulled)
            return false;
        if (!TryComp<EntityStorageComponent>(pulled, out var pulledStorage) || pulledStorage.Open)
            return false;
        crate = pulled;
        return true;
    }

    private sealed class OrdinalIdComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}
