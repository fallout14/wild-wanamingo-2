using System.Linq;
using Content.Server._WW.Spawners;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Critter spawners: every timed mob spawner has a map cap, a per-spawner cap and an activation range, and a den
/// only refills after its own critters die.
/// </summary>
[TestFixture]
public sealed class WastelandSpawnerTest
{
    [Test]
    public async Task EveryMobSpawnerIsCapped()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protos = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var checkedCount = 0;
            foreach (var proto in protos.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || !proto.TryGetComponent<WastelandSpawnerComponent>(out var spawner, factory))
                    continue;

                // Only mob spawners (droppings and eggs use the same component).
                if (!spawner.Prototypes.All(p => protos.Index(p).TryGetComponent<MobStateComponent>(out _, factory)))
                    continue;

                checkedCount++;
                Assert.Multiple(() =>
                {
                    Assert.That(spawner.MaxAlivePerPrototype, Is.GreaterThan(0), $"{proto.ID} has no map cap");
                    Assert.That(spawner.MaxAliveFromSpawner, Is.GreaterThan(0), $"{proto.ID} has no per-spawner cap");
                    Assert.That(spawner.ActivationRange, Is.GreaterThan(0), $"{proto.ID} is never dormant");
                    Assert.That(spawner.IgnoreSpawnBlock, Is.False, $"{proto.ID} spawns in view of players");
                });
            }

            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(70));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DenRefillsOnlyAfterItsCrittersDie()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var system = entMan.System<WastelandSpawnerSystem>();
            var uid = entMan.SpawnEntity("F14TimedSpawnerMolerat", map.GridCoords);
            var spawner = entMan.GetComponent<WastelandSpawnerComponent>(uid);
            Assert.That(spawner.MaxAliveFromSpawner, Is.EqualTo(4));

            // No player within the activation range: blocked, so it retries soon instead of waiting an interval.
            Assert.That(system.ForceFire((uid, spawner)), Is.False);
            Assert.That(spawner.Spawned, Is.Empty);

            spawner.ActivationRange = 0;
            spawner.SpawnBlockRange = 0;
            spawner.Chance = 1;
            spawner.MinimumEntitiesSpawned = 3;
            spawner.MaximumEntitiesSpawned = 3;

            Assert.That(system.ForceFire((uid, spawner)), Is.True);
            Assert.That(spawner.Spawned, Has.Count.EqualTo(3));
            system.ForceFire((uid, spawner));
            Assert.That(spawner.Spawned, Has.Count.EqualTo(4), "only room for one more");
            system.ForceFire((uid, spawner));
            Assert.That(spawner.Spawned, Has.Count.EqualTo(4), "den is full");

            var victim = spawner.Spawned.First();
            entMan.System<MobStateSystem>().ChangeMobState(victim, MobState.Dead);
            system.ForceFire((uid, spawner));
            Assert.That(spawner.Spawned, Has.Count.EqualTo(4), "one dead critter makes room for one new one");
            Assert.That(spawner.Spawned, Does.Not.Contain(victim));
        });

        await pair.CleanReturnAsync();
    }
}
