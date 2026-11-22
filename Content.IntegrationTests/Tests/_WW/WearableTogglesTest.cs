#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Overlays.Legacy;
using Content.Shared.Clothing.Components;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.NightVision;
using Content.Shared.Overlays;
using Robust.Client.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-21 playtest: the NCR flak vest opens and closes as one item; night vision gear reaches the wearer and can be
/// toggled; the night vision and thermal scopes carry their vision.
/// </summary>
public sealed class WearableTogglesTest : GameTest
{
    private const string FlakVest = "F14ClothingOuterNCRFlakVest";
    private static readonly EntProtoId NightVisionScope = "F14WeaponScopeNightVision";
    private static readonly EntProtoId ThermalScope = "F14WeaponScopeThermal";

    [Test]
    public async Task FlakVestOpensAndCloses()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var res = server.ResolveDependency<IResourceManager>();

        await server.WaitAssertion(() =>
        {
            var inv = entMan.System<InventorySystem>();
            var fold = entMan.System<FoldableSystem>();
            var human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var vest = entMan.SpawnEntity(FlakVest, map.GridCoords);
            Assert.That(inv.TryEquip(human, vest, "outerClothing", silent: true, force: true));

            var foldable = entMan.GetComponent<FoldableComponent>(vest);
            var clothing = entMan.GetComponent<ClothingComponent>(vest);
            Assert.That(foldable.IsFolded, Is.False, "the vest starts closed");
            Assert.That(clothing.EquippedPrefix, Is.Null);

            Assert.That(fold.TryToggleFold(vest, foldable, human), "can't open the vest while worn");
            Assert.That(clothing.EquippedPrefix, Is.EqualTo("open"));
            Assert.That(fold.TryToggleFold(vest, foldable, human), "can't close the vest again");
            Assert.That(clothing.EquippedPrefix, Is.Null);

            var rsi = new ResPath("/Textures/Nuclear14/Clothing/OuterClothing/Armor/falloutncrflakvest.rsi");
            foreach (var state in new[] { "icon", "icon-open", "equipped-OUTERCLOTHING", "open-equipped-OUTERCLOTHING" })
                Assert.That(res.ContentFileExists(rsi / $"{state}.png"), $"no {state}");
        });
    }

    [Test]
    public async Task FlakVestIconSwaps()
    {
        var client = Pair.Client;
        await client.WaitAssertion(() =>
        {
            var entMan = client.EntMan;
            var vest = entMan.SpawnEntity(FlakVest, Robust.Shared.Map.MapCoordinates.Nullspace);
            var sprite = entMan.GetComponent<SpriteComponent>(vest);
            var spriteSys = entMan.System<SpriteSystem>();
            Assert.That(spriteSys.LayerMapTryGet((vest, sprite), "unfoldedLayer", out var closed, false));
            Assert.That(spriteSys.LayerMapTryGet((vest, sprite), "foldedLayer", out var open, false));
            // Only the two icon layers: no extra always-visible copy of the closed icon underneath.
            Assert.That(sprite.AllLayers.Count(), Is.EqualTo(2));
            Assert.That(sprite[closed].Visible, Is.True);
            Assert.That(sprite[open].Visible, Is.False);
            entMan.DeleteEntity(vest);
        });
    }

    [TestCase("F14ClothingHeadHelmetPowerArmorX01")]
    [TestCase("F14ClothingEyesNCRNVGs")]
    [TestCase("ClothingEyesNightVisionGoggles")]
    public async Task NightVisionGearReachesTheWearer(string id)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var factory = entMan.ComponentFactory;

        Assert.That(server.ProtoMan.Index<EntityPrototype>(id).TryGetComponent<NightVisionComponent>(out _, factory), $"{id} has no night vision");

        await server.WaitAssertion(() =>
        {
            var inv = entMan.System<InventorySystem>();
            var nv = entMan.System<SharedNightVisionSystem>();
            var human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var gear = entMan.SpawnEntity(id, map.GridCoords);
            var slot = entMan.GetComponent<ClothingComponent>(gear).Slots.HasFlag(SlotFlags.EYES) ? "eyes" : "head";
            Assert.That(inv.TryEquip(human, gear, slot, silent: true, force: true));

            var comp = entMan.GetComponent<NightVisionComponent>(gear);
            Assert.That(comp.RelayOverlay, Is.True, "night vision would only apply to the item, not the wearer");
            Assert.That(comp.ActionEntity, Is.Not.Null, "no toggle action was given to the wearer");
            Assert.That(comp.Enabled, Is.False, "starts off");

            nv.SetEnabled((gear, comp), true, human);
            var ev = new RefreshNightVisionEvent();
            entMan.EventBus.RaiseLocalEvent(human, ref ev);
            Assert.That(ev.Entities.Any(e => e.Owner == gear), "the wearer doesn't see through it when on");
        });
    }

    [Test]
    public async Task VisionScopesCarryTheirVision()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protos.Index(NightVisionScope)
                .TryGetComponent<NightVisionComponent>(out var nv, factory), Is.True);
            Assert.That(nv!.RelayOverlay, Is.True);
            Assert.That(protos.Index(ThermalScope)
                .TryGetComponent<ThermalVisionComponent>(out var thermal, factory), Is.True);
            Assert.That(thermal!.IsActive, Is.True);
            Assert.That(thermal.ToggleAction, Is.Null);
        });
    }
}
