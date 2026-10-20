#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Fire;
using Content.Shared._Misfits.Weapons.Ranged.Flamer;
using Content.Shared._Misfits.Weapons.Throwable;
using Content.Shared._WW.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Flamers lay a line of tile fires and burn fuel; lit molotovs spread fire where they land;
/// Mr. Handy's flamer chamber refills on its own.
/// </summary>
[TestOf(typeof(FlamerAmmoProviderComponent))]
public sealed class FlamerTest : GameTest
{
    private async Task<TestMapData> MakeFloor()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitPost(() =>
        {
            var mapSys = server.EntMan.System<SharedMapSystem>();
            for (var x = -3; x <= 9; x++)
            for (var y = -3; y <= 3; y++)
                mapSys.SetTile(map.Grid, new Vector2i(x, y), map.Tile.Tile);
        });
        return map;
    }

    private int CountFires()
    {
        var count = 0;
        var query = Pair.Server.EntMan.EntityQueryEnumerator<TileFireComponent>();
        while (query.MoveNext(out _))
            count++;
        return count;
    }

    [Test]
    public async Task FlamerLaysFireLine()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await MakeFloor();
        EntityUid gun = default, user = default;
        var fuelBefore = 0f;

        await server.WaitPost(() =>
        {
            user = entMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, 0.5f, 0.5f));
            gun = entMan.SpawnEntity("F14WeaponFlamer", new EntityCoordinates(map.Grid, 0.5f, 0.5f));
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            var solution = entMan.System<SharedSolutionContainerSystem>();
            Assert.That(solution.TryGetSolution(gun, "chamber", out _, out var chamber));
            fuelBefore = chamber!.Volume.Float();

            var gunSys = entMan.System<SharedGunSystem>();
            Assert.That(gunSys.AttemptShoot(user, (gun, entMan.GetComponent<GunComponent>(gun)),
                new EntityCoordinates(map.Grid, 6.5f, 0.5f)), "the flamer should fire");
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(CountFires(), Is.GreaterThanOrEqualTo(4), "a flamer shot should leave a line of fire");

            var solution = entMan.System<SharedSolutionContainerSystem>();
            solution.TryGetSolution(gun, "chamber", out _, out var chamber);
            Assert.That(chamber!.Volume.Float(), Is.LessThan(fuelBefore), "firing should use fuel");
        });

        // Tile fires burn out on their own.
        var timing = server.ResolveDependency<IGameTiming>();
        await server.WaitRunTicks(46 * timing.TickRate);
        await server.WaitAssertion(() => Assert.That(CountFires(), Is.Zero, "tile fires should burn out"));
    }

    [Test]
    public async Task LitMolotovSpreadsFire()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await MakeFloor();
        EntityUid bottle = default, cloth = default, user = default;

        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(map.Grid, 3.5f, 0.5f);
            user = entMan.SpawnEntity("MobHuman", coords);
            bottle = entMan.SpawnEntity("DrinkWhiskeyBottleFull", coords);
            cloth = entMan.SpawnEntity("MaterialCloth1", coords);
            Assert.That(entMan.HasComponent<Content.Server._Misfits.Weapons.Throwable.MolotovConvertibleComponent>(bottle),
                "Wizden glass bottles should take a wick");

            // Stuff the wick in, as a player would.
            var wick = new AfterInteractUsingEvent(user, cloth, bottle, coords, true);
            entMan.EventBus.RaiseLocalEvent(bottle, wick);
            Assert.That(entMan.TryGetComponent<MolotovComponent>(bottle, out var molotov), "the cloth should make a molotov");
            molotov!.Ignited = true;
        });
        await server.WaitRunTicks(1);

        await server.WaitPost(() =>
        {
            var ev = new LandEvent(null, false);
            entMan.EventBus.RaiseLocalEvent(bottle, ref ev);
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(bottle), "the bottle should break");
            // A full bottle of whiskey spreads the widest diamond (radius 2: 13 tiles).
            Assert.That(CountFires(), Is.EqualTo(13));
        });
    }

    [Test]
    public async Task MrHandyFuelRefills()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var timing = server.ResolveDependency<IGameTiming>();
        EntityUid handy = default;

        await server.WaitPost(() => handy = entMan.SpawnEntity("F14PlayerSyntheticMrHandy", map.GridCoords));
        await server.WaitRunTicks(2);

        var solution = entMan.System<SharedSolutionContainerSystem>();
        var drained = 0f;
        await server.WaitPost(() =>
        {
            Assert.That(entMan.HasComponent<NamedSolutionRegenerationComponent>(handy));
            Assert.That(solution.TryGetSolution(handy, "chamber", out var chamberEnt, out _));
            solution.SplitSolution(chamberEnt!.Value, 50);
            drained = chamberEnt.Value.Comp.Solution.Volume.Float();
        });

        await server.WaitRunTicks(4 * timing.TickRate);
        await server.WaitAssertion(() =>
        {
            solution.TryGetSolution(handy, "chamber", out _, out var chamber);
            Assert.That(chamber!.Volume.Float(), Is.GreaterThan(drained), "the flamer chamber should refill");
        });
    }

}
