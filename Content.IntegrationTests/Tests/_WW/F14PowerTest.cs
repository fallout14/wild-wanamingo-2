#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._WW.Power;
using Content.Server.Power.Components;
using Content.Server.Power.Generator;
using Content.Shared._WW.Power;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Light.Components;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Generator;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// F14 power (2026-11-16): every generator powers a load, fusion cores fuel and run down, wasteland solar follows
/// the day, the RobCo power monitor sees nearby devices, and the cell variants were folded into one each.
/// </summary>
[TestFixture, TestOf(typeof(FusionCoreGeneratorSystem)), TestOf(typeof(WastelandSolarSystem)), TestOf(typeof(RobcoPowerTerminalSystem))]
public sealed class F14PowerTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: F14TestHVLoad
  components:
  - type: Transform
    anchored: true
  - type: NodeContainer
    nodes:
      input:
        !type:CableDeviceNode
        nodeGroupID: HVPower
  - type: PowerConsumer
    drawRate: 200

# LV loads draw through an extension cable, like lights and machines.
- type: entity
  id: F14TestLVLoad
  components:
  - type: Transform
    anchored: true
  - type: ApcPowerReceiver
    powerLoad: 200
  - type: ExtensionCableReceiver
";

    private static readonly EntProtoId FusionCore = "F14FusionCore";
    private static readonly EntProtoId LargeGenerator = "F14GeneratorLarge";
    private static readonly EntProtoId Terminal = "F14ComputerTerminalPowerMonitor";

    // Removed 2026-11-16, so plain strings rather than EntProtoIds.
    private static readonly string[] Removed =
    {
        "F14MicrofusionCellRed", "F14MicrofusionCellGreen", "F14MicrofusionCellBlue", "F14MicrofusionCellViolet",
        "F14FusionCoreRed", "F14FusionCoreGreen", "F14FusionCoreBlue", "F14FusionCoreViolet",
        "F14PulseEnergyCell", "F14PulseMicrofusionCell",
    };

    /// <summary>
    /// Generator, its output cable, the load prototype on that voltage, and whether it's a fuel generator that
    /// spawns switched off.
    /// </summary>
    private static readonly object[] Generators =
    {
        new object[] { "F14GeneratorLarge", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorFusioncore", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorFusionMini", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorFusionMiniRusted", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorFusionEnclave", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorVaultTecReactor", "F14CableHV", "F14TestHVLoad", false },
        new object[] { "F14GeneratorPrewar", "F14CableHV", "F14TestHVLoad", true },
        new object[] { "F14GeneratorPrewarUranium", "F14CableHV", "F14TestHVLoad", true },
        new object[] { "F14PortableGeneratorWeldingFuel", "F14CableLV", "F14TestLVLoad", false },
        new object[] { "F14SolarPanel", "F14CableHV", "F14TestHVLoad", false },
    };

    [Test, TestCaseSource(nameof(Generators))]
    public async Task GeneratorPowersLoad(string generator, string cable, string load, bool startOff)
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        PowerConsumerComponent? consumer = null;
        ApcPowerReceiverComponent? receiver = null;
        var anchored = false;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapSys.CreateGridEntity(mapId);
            for (var y = 0; y < 3; y++)
            {
                mapSys.SetTile(grid, new Vector2i(0, y), new Tile(1));
                entMan.SpawnAttachedTo(cable, grid.Owner.ToCoordinates(0, y));
            }

            var gen = entMan.SpawnAttachedTo(generator, grid.Owner.ToCoordinates(0, 0));
            var loadEnt = entMan.SpawnAttachedTo(load, grid.Owner.ToCoordinates(0, 2));
            entMan.TryGetComponent(loadEnt, out consumer);
            entMan.TryGetComponent(loadEnt, out receiver);
            var genXform = entMan.GetComponent<TransformComponent>(gen);
            anchored = genXform.Anchored;

            if (startOff)
                entMan.System<GeneratorSystem>().SetFuelGeneratorOn(gen, true);
        });

        await Pair.RunSeconds(4f);

        await server.WaitAssertion(() =>
        {
            Assert.That(anchored, $"{generator} didn't anchor");
            if (consumer != null)
            {
                Assert.That(consumer.ReceivedPower, Is.EqualTo(consumer.DrawRate).Within(1f),
                    $"{generator} didn't power a {consumer.DrawRate} W load on {cable}");
            }
            else
            {
                Assert.That(receiver!.Powered, $"{generator} didn't power an LV machine on {cable}");
            }
        });
    }

    [Test]
    public async Task FusionCoreRunsDownAndSwaps()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        var slots = server.System<ItemSlotsSystem>();
        var battery = server.System<SharedBatterySystem>();
        var appearance = server.System<SharedAppearanceSystem>();
        EntityUid gen = default;
        EntityUid core = default;
        PowerSupplierComponent supplier = default!;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapSys.CreateGridEntity(mapId);
            for (var y = 0; y < 3; y++)
            {
                mapSys.SetTile(grid, new Vector2i(0, y), new Tile(1));
                entMan.SpawnAttachedTo("F14CableHV", grid.Owner.ToCoordinates(0, y));
            }

            gen = entMan.SpawnAttachedTo(LargeGenerator, grid.Owner.ToCoordinates(0, 0));
            var consumer = entMan.SpawnAttachedTo("F14TestHVLoad", grid.Owner.ToCoordinates(0, 2));
            entMan.GetComponent<PowerConsumerComponent>(consumer).DrawRate = 10000; // full output
            supplier = entMan.GetComponent<PowerSupplierComponent>(gen);

            // Burn a full core in 3 seconds instead of an hour.
            entMan.GetComponent<FusionCoreGeneratorComponent>(gen).CoreLifetime = TimeSpan.FromSeconds(3);
            Assert.That(slots.GetItemOrNull(gen, "fusion_core"), Is.Not.Null, "spawns with a core");
            core = slots.GetItemOrNull(gen, "fusion_core")!.Value;
        });

        await Pair.RunSeconds(2f);

        await server.WaitAssertion(() =>
        {
            Assert.That(supplier.MaxSupply, Is.EqualTo(10000f));
            Assert.That(supplier.CurrentSupply, Is.GreaterThan(0f));
            Assert.That(battery.GetChargeLevel(core), Is.LessThan(1f), "a loaded generator drains its core");
            Assert.That(appearance.TryGetData(gen, FusionCoreGeneratorVisuals.State, out FusionCoreGeneratorState state)
                && state == FusionCoreGeneratorState.Running);
        });

        // The supply ramps up at 1 kW/s, so the core empties a little slower than its 3 s lifetime.
        await Pair.RunSeconds(12f);

        await server.WaitAssertion(() =>
        {
            Assert.That(battery.GetChargeLevel(core), Is.Zero, "core spent");
            Assert.That(supplier.MaxSupply, Is.Zero, "no power from a spent core");
            Assert.That(appearance.TryGetData(gen, FusionCoreGeneratorVisuals.State, out FusionCoreGeneratorState state)
                && state == FusionCoreGeneratorState.NoCore);

            // Swap in a fresh core.
            Assert.That(slots.TryEject(gen, "fusion_core", null, out _), "core ejects");
            var fresh = entMan.SpawnEntity(FusionCore, entMan.GetComponent<TransformComponent>(gen).Coordinates);
            Assert.That(slots.TryInsert(gen, "fusion_core", fresh, null), "fresh core inserts");
            Assert.That(supplier.MaxSupply, Is.EqualTo(10000f), "running again on the fresh core");

            // Only fusion cores fit.
            slots.TryEject(gen, "fusion_core", null, out _);
            var cell = entMan.SpawnEntity("F14MicrofusionCell", entMan.GetComponent<TransformComponent>(gen).Coordinates);
            Assert.That(slots.TryInsert(gen, "fusion_core", cell, null), Is.False, "a microfusion cell doesn't fit");
            Assert.That(supplier.MaxSupply, Is.Zero, "no core, no power");
        });
    }

    [Test]
    public async Task SolarFollowsTheDay()
    {
        await Pair.Server.WaitAssertion(() =>
        {
            var cycle = new LightCycleComponent { Duration = TimeSpan.FromMinutes(30) };
            var length = (float) cycle.Duration.TotalSeconds;
            Assert.Multiple(() =>
            {
                Assert.That(WastelandSolarSystem.Daylight(cycle, 0f), Is.Zero, "midnight");
                Assert.That(WastelandSolarSystem.Daylight(cycle, length / 2f), Is.EqualTo(1f), "midday");
                Assert.That(WastelandSolarSystem.Daylight(cycle, length / 4f), Is.InRange(0.1f, 0.5f), "mid-morning");
                // The moonlight floor (minLightLevel, the test map's 1.5) doesn't make power.
                cycle.MinLightLevel = 1.5f;
                Assert.That(WastelandSolarSystem.Daylight(cycle, 0f), Is.Zero, "moonlight");
            });
        });
    }

    [Test]
    public async Task TerminalListsNearbyDevices()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        EntityUid terminal = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapSys.CreateGridEntity(mapId);
            for (var x = 0; x < 50; x++)
                mapSys.SetTile(grid, new Vector2i(x, 0), new Tile(1));

            terminal = entMan.SpawnAttachedTo(Terminal, grid.Owner.ToCoordinates(0, 0));
            entMan.SpawnAttachedTo(LargeGenerator, grid.Owner.ToCoordinates(5, 0));
            entMan.SpawnAttachedTo("F14SubstationBasic", grid.Owner.ToCoordinates(8, 0));
            entMan.SpawnAttachedTo("F14APCBreaker", grid.Owner.ToCoordinates(10, 0));
            entMan.SpawnAttachedTo("F14PowerStorageBasic", grid.Owner.ToCoordinates(12, 0));
            entMan.SpawnAttachedTo("F14GeneratorVaultTecReactor", grid.Owner.ToCoordinates(45, 0)); // out of range
        });

        await Pair.RunSeconds(1f);

        await server.WaitAssertion(() =>
        {
            var devices = server.System<RobcoPowerTerminalSystem>()
                .GetDevices((terminal, entMan.GetComponent<RobcoPowerTerminalComponent>(terminal)));
            var kinds = devices.Select(d => d.Kind).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(kinds.Count(k => k == RobcoPowerDeviceKind.Generator), Is.EqualTo(1), "only the generator in range");
                Assert.That(kinds, Does.Contain(RobcoPowerDeviceKind.Substation));
                Assert.That(kinds, Does.Contain(RobcoPowerDeviceKind.Breaker));
                Assert.That(kinds, Does.Contain(RobcoPowerDeviceKind.Storage));

                var gen = devices.Single(d => d.Kind == RobcoPowerDeviceKind.Generator);
                Assert.That(gen.Fuel, Is.EqualTo(1f).Within(0.01f), "fusion core level reported");
                Assert.That(gen.Distance, Is.EqualTo(5));
                Assert.That(gen.Direction, Is.EqualTo(Direction.East));
            });
        });
    }

    [Test]
    public async Task OneOfEachCell()
    {
        var server = Pair.Server;
        var protos = server.ResolveDependency<IPrototypeManager>();
        var entMan = server.ResolveDependency<IEntityManager>();
        var slots = server.System<ItemSlotsSystem>();

        await server.WaitAssertion(() =>
        {
            foreach (var id in Removed)
            {
                Assert.That(protos.HasIndex<EntityPrototype>(id), Is.False, $"{id} should be gone");
            }

            // Pulse weapons take the standard cells now.
            foreach (var (gun, cell) in new[]
                     {
                         ("F14WeaponPulsePistol", "F14PowerCellSmall"),
                         ("F14WeaponPulseRifle", "F14MicrofusionCell"),
                         ("F14WeaponLaserRifleAER12", "F14MicrofusionCell"),
                     })
            {
                var g = entMan.SpawnEntity(gun, MapCoordinates.Nullspace);
                slots.TryEject(g, "gun_magazine", null, out var loaded);
                Assert.That(loaded, Is.Not.Null, $"{gun} spawns loaded");
                Assert.That(entMan.GetComponent<MetaDataComponent>(loaded!.Value).EntityPrototype?.ID, Is.EqualTo(cell));
                var spare = entMan.SpawnEntity(cell, MapCoordinates.Nullspace);
                Assert.That(slots.TryInsert(g, "gun_magazine", spare, null), $"{gun} takes {cell}");
                entMan.DeleteEntity(g);
                entMan.DeleteEntity(loaded);
            }
        });
    }
}
