using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._WW.Temperature;
using Content.Shared._WW.Botany;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Wasteland cooking (unpowered heaters + Wizden temperature steps) and planting produce from a stack.
/// </summary>
[TestFixture, TestOf(typeof(UnpoweredHeaterSystem)), TestOf(typeof(StackedSeedSystem))]
public sealed class CookingAndPlantingTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: F14TestUnpoweredHeater
  components:
  - type: UnpoweredHeater
  - type: ItemPlacer
    whitelist:
      components:
      - Temperature
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      itemcollide:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.5,-0.5,0.5,0.5""
        layer:
        - HighImpassable
        hard: false

- type: entity
  parent: F14TestUnpoweredHeater
  id: F14TestUnlitFire
  components:
  - type: UnpoweredHeater
    requireHot: true
";

    private const string RawSteak = "F14FoodMeatBrahmin";
    private const string CookedSteak = "F14FoodMeatBrahminCooked";

    [Test]
    public async Task HeaterCooksRawSteak()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        MapId map = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out map);
            entMan.SpawnEntity("F14TestUnpoweredHeater", new MapCoordinates(0, 0, map));
            entMan.SpawnEntity(RawSteak, new MapCoordinates(0, 0, map));
        });

        // Wizden's temperature step replaces the raw steak with the cooked one once it's hot enough inside.
        var cooked = false;
        for (var i = 0; i < 60 && !cooked; i++)
        {
            await Pair.RunSeconds(2f);
            await server.WaitPost(() => cooked = Count(entMan, CookedSteak) > 0);
        }

        await server.WaitAssertion(() =>
        {
            Assert.That(cooked, $"{RawSteak} on an unpowered heater never became {CookedSteak}");
            Assert.That(Count(entMan, RawSteak), Is.Zero);
            entMan.DeleteEntity(mapSys.GetMap(map));
        });
    }

    [Test]
    public async Task UnlitFireDoesNotHeat()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        MapId map = default;
        EntityUid steak = default;
        var before = 0f;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out map);
            entMan.SpawnEntity("F14TestUnlitFire", new MapCoordinates(0, 0, map));
            steak = entMan.SpawnEntity(RawSteak, new MapCoordinates(0, 0, map));
            before = entMan.GetComponent<TemperatureComponent>(steak).Temperature;
        });

        await Pair.RunSeconds(10f);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<TemperatureComponent>(steak).Temperature,
                Is.LessThanOrEqualTo(before + 0.5f),
                "An unlit fire heated the food placed on it");
            entMan.DeleteEntity(mapSys.GetMap(map));
        });
    }

    [Test]
    public async Task PlantingFromStackUsesOne()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        var stackSys = server.System<SharedStackSystem>();
        var traySys = server.System<PlantTraySystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var map);
            var tray = entMan.SpawnEntity("HydroponicsTrayEmpty", new MapCoordinates(0, 0, map));
            var user = entMan.SpawnEntity("MobHuman", new MapCoordinates(1, 0, map));
            var leaves = entMan.SpawnEntity("F14FloraProduceWildAgave", new MapCoordinates(1, 0, map));
            Assert.That(entMan.HasComponent<SeedComponent>(leaves), "Wild agave leaves should be plantable");
            stackSys.SetCount(leaves, 5);

            var ev = new AfterInteractEvent(user, leaves, tray, entMan.GetComponent<TransformComponent>(tray).Coordinates, true);
            entMan.EventBus.RaiseLocalEvent(leaves, ev);

            Assert.That(traySys.HasPlant(tray), "Planting from a stack of produce didn't plant anything");
            Assert.That(entMan.EntityExists(leaves), "Planting used up the whole stack");
            Assert.That(entMan.GetComponent<StackComponent>(leaves).Count, Is.EqualTo(4));

            entMan.DeleteEntity(mapSys.GetMap(map));
        });
    }

    private static int Count(IEntityManager entMan, string proto)
    {
        return entMan.AllEntities<MetaDataComponent>().Count(e => e.Comp.EntityPrototype?.ID == proto);
    }
}
