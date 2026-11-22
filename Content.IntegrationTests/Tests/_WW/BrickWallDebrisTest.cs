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
/// 2026-11-22 playtest: "brick walls crumble into wood". Breaking a brick or adobe wall leaves no wood behind.
/// </summary>
public sealed class BrickWallDebrisTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> Structural = "Structural";

    [TestCase("F14WallBrick")]
    [TestCase("F14WallBrickSlanted")]
    [TestCase("F14WallBrickAltSlanted")]
    [TestCase("F14WallBrickGraySlanted")]
    [TestCase("F14WallBrickHalf")]
    [TestCase("F14WallBrickTall")]
    [TestCase("F14WallAdobeSlanted")]
    [TestCase("F14WallAdobeTall")]
    public async Task BrokenBrickLeavesNoWood(string wall)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        EntityUid uid = default;
        await server.WaitPost(() =>
        {
            uid = entMan.SpawnEntity(wall, map.GridCoords);
            var damage = entMan.System<DamageableSystem>();
            var hit = new DamageSpecifier(server.ProtoMan.Index(Blunt), 5000);
            hit += new DamageSpecifier(server.ProtoMan.Index(Structural), 5000);
            damage.ChangeDamage(uid, hit, ignoreResistances: true);
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var left = entMan.GetEntities()
                .Where(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype is { } p &&
                            entMan.GetComponent<TransformComponent>(e).ParentUid == map.Grid.Owner)
                .Select(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype!.ID)
                .ToList();
            TestContext.Out.WriteLine($"{wall}: deleted={entMan.Deleted(uid)} left=[{string.Join(", ", left)}]");
            Assert.That(left.Any(id => id.Contains("Wood") || id.Contains("Plank")), Is.False,
                $"{wall} left wood: {string.Join(", ", left)}");
        });
    }
}
