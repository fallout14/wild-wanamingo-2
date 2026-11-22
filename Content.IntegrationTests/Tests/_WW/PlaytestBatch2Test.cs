#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.CCVar;
using Content.Shared.Clothing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Reflect;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>2026-11-22 playtest batch: the smaller prototype fixes.</summary>
public sealed class PlaytestBatch2Test : GameTest
{
    private static readonly EntProtoId PowerArmor = "F14ClothingOuterPowerArmorT45";

    [Test]
    public async Task PowerArmorDeflectsOnlyWhenWorn()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var suit = entMan.SpawnEntity(PowerArmor, map.GridCoords);
            var reflect = entMan.GetComponent<ReflectComponent>(suit);

            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(human, suit));
            Assert.That(reflect.InRightPlace, Is.False, "power armor deflects while carried in a hand");

            entMan.System<SharedHandsSystem>().TryDrop(human, suit);
            Assert.That(entMan.System<InventorySystem>().TryEquip(human, suit, "outerClothing", silent: true, force: true));
            Assert.That(reflect.InRightPlace, Is.True, "worn power armor doesn't deflect");
        });
    }

    [Test]
    public async Task CaliberClasses()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            ReflectType Shot(string id) => protos.Index<EntityPrototype>(id).TryGetComponent<ReflectiveComponent>(out var r, factory)
                ? r.Reflective
                : ReflectType.None;

            var pa = protos.Index(PowerArmor).Components.Values.Select(c => c.Component).OfType<ReflectComponent>().Single();
            Assert.That(pa.Reflects & Shot("F14Bullet9mm"), Is.Not.EqualTo(ReflectType.None), "9mm should glance off power armor");
            Assert.That(pa.Reflects & Shot("F14Bullet556"), Is.Not.EqualTo(ReflectType.None), "5.56 should glance off power armor");
            Assert.That(pa.Reflects & Shot("F14Bullet50"), Is.EqualTo(ReflectType.None), ".50 deflects off power armor");
            Assert.That(Shot("F14Bullet50"), Is.EqualTo(ReflectType.None), ".50 can deflect off something");
            Assert.That(pa.ReflectingInHands, Is.False);
            Assert.That(ReflectSystem.GetReflectProb(pa, ReflectType.MediumCaliber), Is.LessThan(pa.ReflectProb),
                "medium rounds should glance off less often than pistol rounds");
        });
    }

    [TestCase("F14ClothingHeadHatNCRCombatHelmetBrodie")]
    [TestCase("F14ClothingHeadHatNCRHelmetMetalDesert")]
    [TestCase("F14ClothingHeadHatNCRM1Helmet")]
    public async Task NcrHelmetsShowHair(string id)
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<HideLayerClothingComponent>(out var hide, factory));
#pragma warning disable CS0618 // Slots is what the Corvax base helmet set
            Assert.That(hide!.Slots, Is.Empty, $"{id} still hides hair");
#pragma warning restore CS0618
            Assert.That(hide.Layers, Is.Empty);
        });
    }

    [TestCase("F14WeaponPulseRifle")]
    [TestCase("F14WeaponPulseCarbine")]
    [TestCase("F14WeaponZAXPulseRifle")]
    [TestCase("F14WeaponSniper50AntiMateriel")]
    public async Task TwoHandedGuns(string id)
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
            Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<GunRequiresWieldComponent>(out _, factory), $"{id} fires one-handed"));
    }

    [TestCase("F14Ripper")]
    [TestCase("F14RipperAdvanced")]
    public async Task RippersSwingFast(string id)
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<MeleeWeaponComponent>(out var melee, factory));
            Assert.That(melee!.AttackRate, Is.GreaterThanOrEqualTo(2f), $"{id} swings slowly");
        });
    }

    [Test]
    public async Task SuperSledgeSlowsYou()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid human = default, sledge = default;
        await server.WaitPost(() =>
        {
            human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            sledge = entMan.SpawnEntity("F14SuperSledgeHammer", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(human, sledge));
        });
        await Pair.RunTicksSync(5);
        float held = 0;
        await server.WaitPost(() =>
        {
            held = entMan.GetComponent<MovementSpeedModifierComponent>(human).WalkSpeedModifier;
            Assert.That(entMan.System<Content.Shared.Wieldable.SharedWieldableSystem>()
                .TryWield(sledge, human));
        });
        await Pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            Assert.That(held, Is.LessThan(1f), "holding the super sledge doesn't slow you");
            Assert.That(entMan.GetComponent<MovementSpeedModifierComponent>(human).WalkSpeedModifier, Is.LessThan(held),
                "wielding it doesn't slow you more");
        });
    }

    [Test]
    public async Task CameraRotationIsLocked()
    {
        await Pair.Server.WaitAssertion(() =>
            Assert.That(Pair.Server.ResolveDependency<IConfigurationManager>().GetCVar(CCVars.CameraRotationLocked)));
    }

    [Test]
    public async Task OldHairStylesArePorted()
    {
        var protos = Pair.Server.ProtoMan;
        await Pair.Server.WaitAssertion(() =>
        {
            foreach (var id in new[] { "F14ZetaHairNerfish", "F14HumanHairBraidedExtension", "F14HairGloomy",
                         "F14HumanHairArabicGathered", "HumanHairShoulderLengthOverEye" })
            {
                Assert.That(protos.TryIndex<MarkingPrototype>(id, out var marking), $"{id} missing");
                Assert.That(marking!.BodyPart, Is.EqualTo(Content.Shared.Humanoid.HumanoidVisualLayers.Hair));
            }
        });
    }
}
