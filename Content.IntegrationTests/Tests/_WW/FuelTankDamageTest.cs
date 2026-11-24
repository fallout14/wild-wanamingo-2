#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-23 playtest: one thrown knife (10 slash + 15 piercing, ignoring resistances) destroyed a wide fuel tank
/// and its fuel. A hit like that leaves the tank and its fuel; 100 damage still breaks it.
/// </summary>
public sealed class FuelTankDamageTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";
    private static readonly ProtoId<DamageTypePrototype> Piercing = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [TestCase("F14StorageTankWideFullFuel")]
    [TestCase("F14StorageTankFullFuel")]
    [TestCase("F14StorageTankHugeFullFuel")]
    [TestCase("F14StorageTankVatFullFuel")]
    public async Task ThrownKnifeDoesntWreckIt(string tankId)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var damage = entMan.System<DamageableSystem>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();

        EntityUid tank = default;
        FixedPoint2 fuel = 0;
        await server.WaitPost(() =>
        {
            tank = entMan.SpawnEntity(tankId, map.GridCoords);
            Assert.That(solutions.TryGetSolution(tank, "tank", out _, out var sol));
            fuel = sol!.Volume;
            var knife = new DamageSpecifier(server.ProtoMan.Index(Slash), 10);
            knife += new DamageSpecifier(server.ProtoMan.Index(Piercing), 15);
            damage.TryChangeDamage(tank, knife, ignoreResistances: true);
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.EntityExists(tank), "one thrown knife destroyed the tank");
            Assert.That(solutions.TryGetSolution(tank, "tank", out _, out var sol));
            Assert.That(sol!.Volume, Is.EqualTo(fuel), "the knife drained the fuel");
            Assert.That(fuel, Is.GreaterThan(FixedPoint2.Zero));
            damage.TryChangeDamage(tank, new DamageSpecifier(server.ProtoMan.Index(Blunt), 100), ignoreResistances: true);
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() => Assert.That(entMan.EntityExists(tank), Is.False, "100 damage should still break it"));
    }
}
