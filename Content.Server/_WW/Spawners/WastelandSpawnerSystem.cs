using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._WW.Spawners;

/// <summary>
/// Runs <see cref="WastelandSpawnerComponent"/>. Missed intervals aren't made up: a spawner that sat dormant for
/// an hour fires once when a player shows up, not once per interval. A spawn blocked only by where players are
/// is retried after <see cref="WastelandSpawnerComponent.RetrySeconds"/>; anything else waits a full interval.
/// </summary>
public sealed partial class WastelandSpawnerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<ActorComponent>> _players = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WastelandSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WastelandSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextFire = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.IntervalSeconds);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WastelandSpawnerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spawner, out var xform))
        {
            if (spawner.NextFire > now)
                continue;

            var delay = TrySpawn((uid, spawner, xform)) ? spawner.IntervalSeconds : spawner.RetrySeconds;
            spawner.NextFire = now + TimeSpan.FromSeconds(Math.Max(1, delay));
        }
    }

    /// <summary>
    /// Returns false when the spawn was blocked only by where players are (retry soon), true otherwise.
    /// </summary>
    private bool TrySpawn(Entity<WastelandSpawnerComponent, TransformComponent> ent)
    {
        var (uid, spawner, xform) = ent;
        if (spawner.Prototypes.Count == 0)
            return true;

        var coords = _transform.GetMapCoordinates(uid, xform);

        if (spawner.ActivationRange > 0 && !LivingPlayerWithin(coords, spawner.ActivationRange))
            return false;

        if (!spawner.IgnoreSpawnBlock && spawner.SpawnBlockRange > 0 && LivingPlayerWithin(coords, spawner.SpawnBlockRange))
            return false;

        if (spawner.MaxAlivePerPrototype > 0 && CountAlive(spawner, xform.MapUid) >= spawner.MaxAlivePerPrototype)
            return true;

        var room = int.MaxValue;
        if (spawner.MaxAliveFromSpawner > 0)
        {
            spawner.Spawned.RemoveWhere(e => TerminatingOrDeleted(e) || _mobState.IsDead(e));
            room = spawner.MaxAliveFromSpawner - spawner.Spawned.Count;
            if (room <= 0)
                return true;
        }

        if (!_random.Prob(spawner.Chance))
            return true;

        var count = Math.Min(room, _random.Next(spawner.MinimumEntitiesSpawned, spawner.MaximumEntitiesSpawned + 1));
        for (var i = 0; i < count; i++)
        {
            spawner.Spawned.Add(SpawnAtPosition(_random.Pick(spawner.Prototypes), xform.Coordinates));
        }

        return true;
    }

    /// <summary>
    /// Fires the spawner now, ignoring its timer (tests, admin use). Returns false if it was blocked by players.
    /// </summary>
    public bool ForceFire(Entity<WastelandSpawnerComponent> ent)
    {
        return TrySpawn((ent, ent.Comp, Transform(ent)));
    }

    private bool LivingPlayerWithin(Robust.Shared.Map.MapCoordinates coords, float range)
    {
        _players.Clear();
        _lookup.GetEntitiesInRange(coords, range, _players);
        // Ghosts and observers have no mob state, so they neither wake spawners nor block them.
        return _players.Any(p => HasComp<MobStateComponent>(p.Owner) && !_mobState.IsDead(p.Owner));
    }

    private int CountAlive(WastelandSpawnerComponent spawner, EntityUid? map)
    {
        var count = 0;
        var query = EntityQueryEnumerator<MobStateComponent, MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var mob, out var state, out var meta, out var xform))
        {
            if (xform.MapUid != map || meta.EntityPrototype is not { } proto || _mobState.IsDead(mob, state))
                continue;

            if (spawner.Prototypes.Contains(proto.ID))
                count++;
        }

        return count;
    }
}
