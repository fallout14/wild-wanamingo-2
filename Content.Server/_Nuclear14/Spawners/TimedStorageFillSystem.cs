// Ported from Nuclear 14 (https://github.com/Vault-Overseers/nuclear-14) via Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Server/Spawners/Components/TimedStorageFill.cs, Content.Server/Spawners/EntitySystems/TimedStorageFillSystem.cs
// WW: Wizden's storage APIs; timer as an absolute time instead of a per-tick countdown.

using Content.Server.Storage.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Nuclear14.Spawners;

/// <summary>
/// A container that restocks itself: whenever it is found empty after its timer, it is filled again.
/// Unanchoring it stops the restocking for good.
/// </summary>
[RegisterComponent]
public sealed partial class TimedStorageFillComponent : Component
{
    [DataField]
    public List<EntitySpawnEntry> Contents = new();

    [DataField]
    public int MinimumSeconds = 300;

    [DataField]
    public int MaximumSeconds = 1200;

    [ViewVariables]
    public TimeSpan NextRefill;
}

public sealed partial class TimedStorageFillSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private StorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimedStorageFillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TimedStorageFillComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnMapInit(Entity<TimedStorageFillComponent> ent, ref MapInitEvent args)
    {
        Fill(ent);
        ScheduleNext(ent.Comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<TimedStorageFillComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextRefill)
                continue;

            Fill((uid, comp));
            ScheduleNext(comp);
        }
    }

    private void ScheduleNext(TimedStorageFillComponent comp)
    {
        comp.NextRefill = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(comp.MinimumSeconds, comp.MaximumSeconds + 1));
    }

    private void Fill(Entity<TimedStorageFillComponent> ent)
    {
        if (ent.Comp.Contents.Count == 0)
            return;

        TryComp<StorageComponent>(ent, out var storage);
        TryComp<EntityStorageComponent>(ent, out var entityStorage);
        if (storage == null && entityStorage == null)
        {
            Log.Error($"{nameof(TimedStorageFillComponent)} on {ToPrettyString(ent)} has no storage to fill.");
            return;
        }

        // Only restock when it has been emptied.
        if (storage?.Container?.ContainedEntities.Count > 0 || entityStorage?.Contents.ContainedEntities.Count > 0)
            return;

        var coordinates = Transform(ent).Coordinates;
        foreach (var item in EntitySpawnCollection.GetSpawns(ent.Comp.Contents, _random))
        {
            var spawned = Spawn(item, coordinates);
            if (entityStorage != null && _entityStorage.Insert(spawned, ent, entityStorage))
                continue;

            if (storage != null && _storage.Insert(ent, spawned, out _, storageComp: storage, playSound: false))
                continue;

            Log.Error($"Tried to restock {item} inside {ToPrettyString(ent)} but it didn't fit.");
            Del(spawned);
        }
    }

    private void OnAnchorChanged(Entity<TimedStorageFillComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            RemCompDeferred<TimedStorageFillComponent>(ent);
    }
}
