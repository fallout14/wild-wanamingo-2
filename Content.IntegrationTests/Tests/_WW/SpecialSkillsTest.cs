#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Shared._Misfits.Holotape;
using Content.Shared._Misfits.Overlays.Legacy;
using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.SpecialStats;
using Content.Shared._WW.DoAfter;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Medical;
using Content.Shared.Movement.Components;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The rest of SPECIAL (Strength carrying, Intelligence building/workbenches/medicine) and the small ports that came
/// with it: holotapes, power armor thermal vision, and solutions moved off the old format.
/// </summary>
public sealed class SpecialSkillsTest : GameTest
{
    private static async Task<(EntityUid Low, EntityUid High)> SpawnPair(TestPair pair, SpecialProfile low, SpecialProfile high)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        var special = entMan.System<SharedSpecialSystem>();
        var map = await pair.CreateTestMap();
        EntityUid lowUid = default, highUid = default;

        await server.WaitPost(() =>
        {
            lowUid = entMan.SpawnEntity("MobHuman", map.MapCoords);
            highUid = entMan.SpawnEntity("MobHuman", map.MapCoords);

            foreach (var (uid, stats) in new[] { (lowUid, low), (highUid, high) })
            {
                var profile = HumanoidCharacterProfile.DefaultWithSpecies().WithSpecial(stats);
                Assert.That(special.TryApplyProfileBaseValues(uid, profile));
                var ev = new SpecialStatsReadyEvent(uid);
                entMan.EventBus.RaiseLocalEvent(uid, ref ev, true);
            }
        });
        await server.WaitRunTicks(2);
        return (lowUid, highUid);
    }

    // Same 40 points; only Strength and Intelligence trade places with Charisma.
    private static readonly SpecialProfile Weak = new()
        { Strength = 1, Perception = 5, Endurance = 5, Charisma = 9, Intelligence = 1, Agility = 5, Luck = 10 };
    private static readonly SpecialProfile Strong = new()
        { Strength = 10, Perception = 5, Endurance = 5, Charisma = 1, Intelligence = 10, Agility = 5, Luck = 4 };

    [Test]
    public async Task StrengthCarriesADuffelWithoutSlowdown()
    {
        var pair = Pair;
        var entMan = pair.Server.EntMan;
        var inventory = entMan.System<InventorySystem>();
        Assert.That(Weak.IsValid && Strong.IsValid);

        var (weak, strong) = await SpawnPair(pair, Weak, Strong);
        float weakBefore = 0, strongBefore = 0;

        await pair.Server.WaitPost(() =>
        {
            weakBefore = entMan.GetComponent<MovementSpeedModifierComponent>(weak).CurrentSprintSpeed;
            strongBefore = entMan.GetComponent<MovementSpeedModifierComponent>(strong).CurrentSprintSpeed;

            foreach (var uid in new[] { weak, strong })
            {
                var bag = entMan.SpawnEntity("ClothingBackpackDuffel", entMan.GetComponent<TransformComponent>(uid).Coordinates);
                inventory.TryUnequip(uid, "back", silent: true, force: true);
                Assert.That(inventory.TryEquip(uid, bag, "back", silent: true, force: true));
            }
        });
        await pair.Server.WaitRunTicks(2);

        await pair.Server.WaitAssertion(() =>
        {
            // A duffel slows sprinting to 90%; Strength 7+ ignores it.
            Assert.That(entMan.GetComponent<MovementSpeedModifierComponent>(weak).CurrentSprintSpeed,
                Is.EqualTo(weakBefore * 0.9f).Within(0.01f));
            Assert.That(entMan.GetComponent<MovementSpeedModifierComponent>(strong).CurrentSprintSpeed,
                Is.EqualTo(strongBefore).Within(0.01f));
        });
    }

    [Test]
    public async Task IntelligenceSetsCraftingAndMedicalSpeed()
    {
        var pair = Pair;
        var entMan = pair.Server.EntMan;
        var special = entMan.System<SharedSpecialSystem>();

        var (dull, smart) = await SpawnPair(pair, Weak, Strong);

        await pair.Server.WaitAssertion(() =>
        {
            // Intelligence 1 can't build or work a bench; 10 can, and does it faster.
            Assert.That(special.CanConstructWithIntelligence(dull), Is.False);
            Assert.That(special.CanConstructWithIntelligence(smart));
            Assert.That(special.CanUseLatheWithIntelligence(dull), Is.False);
            Assert.That(special.CanUseLatheWithIntelligence(smart));
            Assert.That(special.GetIntelligenceConstructionDelay(smart, 10f), Is.LessThan(10f));
            Assert.That(special.GetIntelligenceLatheProductionTime(smart, TimeSpan.FromSeconds(10)),
                Is.LessThan(special.GetIntelligenceLatheProductionTime(dull, TimeSpan.FromSeconds(10))));

            // Medical do-afters are scaled as they start; others aren't.
            TimeSpan Delay(EntityUid user, DoAfterEvent ev)
            {
                var args = new DoAfterArgs(entMan, user, TimeSpan.FromSeconds(10), ev, user);
                var modify = new ModifyDoAfterDelayEvent(args);
                entMan.EventBus.RaiseLocalEvent(user, ref modify);
                return args.Delay;
            }

            Assert.That(Delay(smart, new HealingDoAfterEvent()), Is.LessThan(Delay(dull, new HealingDoAfterEvent())));
            Assert.That(Delay(smart, new AwaitedDoAfterEvent()), Is.EqualTo(TimeSpan.FromSeconds(10)));
        });
    }

    [Test]
    public void CharismaGrantsLoadoutPoints()
    {
        Assert.That(SharedSpecialSystem.GetCharismaLoadoutPointModifier(10), Is.GreaterThan(0));
        Assert.That(SharedSpecialSystem.GetCharismaLoadoutPointModifier(5), Is.EqualTo(0));
        Assert.That(SharedSpecialSystem.GetCharismaLoadoutPointModifier(1), Is.LessThan(0));
    }

    [TestCase("F14HolotapeLoreProspector")]
    [TestCase("F14HolotapeRandomBlue")]
    [TestCase("F14ComputerTerminal")]
    public async Task HolotapesAndTerminalsHaveText(string proto)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnEntity(proto, map.GridCoords);
            var data = entMan.GetComponent<HolotapeDataComponent>(uid);
            Assert.That(data.Localized);
            Assert.That(data.Title, Is.Not.Empty.And.Not.EndsWith("-title"), "the title is localized text, not a key");
            Assert.That(data.Content, Is.Not.Empty.And.Not.EndsWith("-content"));
            entMan.DeleteEntity(uid);
        });
    }

    private static readonly EntProtoId PipBoy = "F14Pipboy";

    [Test]
    public async Task PipBoysPlayHolotapes()
    {
        var protoMan = Pair.Server.ProtoMan;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protoMan.Index(PipBoy).TryGetComponent<HolotapeReaderComponent>(out _, Pair.Server.EntMan.ComponentFactory));
        });
    }

    [TestCase("F14ClothingHeadHelmetPowerArmorX01")]
    [TestCase("F14ClothingHeadHelmetPowerArmorMidwestCommander")]
    public async Task PowerArmorHelmetsHaveThermalVision(string proto)
    {
        var protoMan = Pair.Server.ProtoMan;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protoMan.Index(proto).TryGetComponent<ThermalVisionComponent>(out var thermal,
                Pair.Server.EntMan.ComponentFactory));
            Assert.That(thermal!.IsEquipment);
        });
    }

    [TestCase("F14Sink", new[] { "drainBuffer", "tank" })]
    [TestCase("F14SinkFilledWater", new[] { "drainBuffer", "tank" })]
    [TestCase("F14MobNightstalker", new[] { "melee", "udder" })]
    [TestCase("F14IngotIron", new[] { "iron" })]
    public async Task OldSolutionsStillExist(string proto, string[] solutions)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var solutionSystem = entMan.System<SharedSolutionContainerSystem>();
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnEntity(proto, map.GridCoords);
            foreach (var name in solutions)
            {
                Assert.That(solutionSystem.TryGetSolution(uid, name, out _), $"{proto} has its '{name}' solution");
            }
            entMan.DeleteEntity(uid);
        });
    }
}
