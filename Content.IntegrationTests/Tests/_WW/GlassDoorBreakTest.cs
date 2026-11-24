#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 playtest: "F14GlassDoorBroken when hit with enough damage to destroy, spawns a glass shard without
/// destroying". A broken glass door hit hard enough goes away, and hitting it doesn't keep making shards.
/// </summary>
public sealed class GlassDoorBreakTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [TestCase("F14DoorGlassBroken")]
    [TestCase("F14DoorGlassStoreBroken")]
    public async Task BrokenDoorBreaks(string door)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        EntityUid uid = default;
        await server.WaitPost(() => uid = entMan.SpawnEntity(door, map.GridCoords));
        await Pair.RunTicksSync(2);

        string Report()
        {
            var left = entMan.GetEntities()
                .Where(e => !entMan.Deleted(e) && entMan.GetComponent<MetaDataComponent>(e).EntityPrototype is not null &&
                            entMan.GetComponent<TransformComponent>(e).ParentUid == map.Grid.Owner)
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype!.ID)
                .ToList();
            return string.Join(", ", left);
        }

        for (var hit = 1; hit <= 8; hit++)
        {
            await server.WaitPost(() =>
            {
                if (entMan.Deleted(uid))
                    return;
                entMan.System<DamageableSystem>().TryChangeDamage(uid, new DamageSpecifier(server.ProtoMan.Index(Blunt), 15));
            });
            await Pair.RunTicksSync(3);
            await server.WaitPost(() => TestContext.Out.WriteLine(
                $"{door} hit {hit}: deleted={entMan.Deleted(uid)} damage={(entMan.Deleted(uid) ? 0 : entMan.System<DamageableSystem>().GetTotalDamage(uid))} left=[{Report()}]"));
        }

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(uid), $"{door} survived 8 hits: {Report()}");
            var shards = Report().Split(", ").Count(s => s.StartsWith("ShardGlass"));
            Assert.That(shards, Is.LessThanOrEqualTo(2), $"{door} made {shards} shards");
        });
    }
}

/// <summary>
/// Since Wizden #44644 a child's Destructible thresholds are added to its parents'. Prototypes ported from N14/Misfits
/// opt out with <c>- replaceInherited</c>; these must break at their own threshold, not a parent's lower one.
/// </summary>
public sealed class ReplaceInheritedThresholdsTest : GameTest
{
    [TestCase("F14DoorGlassBroken", 50)]
    [TestCase("F14DoorGlassStore", 75)]
    [TestCase("F14MobRobotSentryBot", 750)]
    [TestCase("F14RockGreyMining", 400)]
    [TestCase("F14BarricadeMetal", 200)]
    public async Task OwnThresholdsOnly(string id, int lowest)
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var uid = server.EntMan.SpawnEntity(id, map.GridCoords);
            var destructible = server.EntMan.GetComponent<Content.Server.Destructible.DestructibleComponent>(uid);
            var damages = destructible.Thresholds
                .Select(t => t.Trigger)
                .OfType<Content.Shared.Destructible.Thresholds.Triggers.DamageTrigger>()
                .Select(t => t.Damage.Float())
                .ToList();
            Assert.That(damages, Is.Not.Empty, id);
            Assert.That(damages.Min(), Is.EqualTo(lowest), $"{id} has inherited thresholds: {string.Join(", ", damages)}");
        });
    }
}
