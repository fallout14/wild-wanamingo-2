#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Clothing.Legacy;
using Content.Shared._Misfits.PowerArmor;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Power armor needs training to wear, deploys its pieces from the suit, and its integrity soaks most of a hit.
/// </summary>
[TestOf(typeof(PowerArmorIntegritySystem))]
public sealed class PowerArmorTest : GameTest
{
    private const string Suit = "N14ClothingOuterPowerArmorT45";
    private static readonly ProtoId<Content.Shared.Damage.Prototypes.DamageTypePrototype> Blunt = "Blunt";

    [Test]
    public async Task TrainingDeployAndIntegrity()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid user = default, suit = default;

        await server.WaitPost(() =>
        {
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            suit = entMan.SpawnEntity(Suit, map.GridCoords);
        });

        var inventory = entMan.System<InventorySystem>();
        await server.WaitAssertion(() =>
        {
            Assert.That(inventory.TryEquip(user, suit, "outerClothing", silent: true), Is.False,
                "power armor needs training");
            entMan.AddComponent<PowerArmorProficiencyComponent>(user);
            Assert.That(inventory.TryEquip(user, suit, "outerClothing", silent: true), "trained wearers can suit up");
        });
        await server.WaitRunTicks(2);

        // Deploy the helmet, gauntlets, boots and power pack.
        await server.WaitPost(() =>
        {
            // The suit action opens a radial menu of pieces; deploy each one as the menu would.
            var comp = entMan.GetComponent<MultiToggleableClothingComponent>(suit);
            var toggle = entMan.System<MultiToggleableClothingSystem>();
            foreach (var piece in comp.ClothingUids.Keys.ToList())
                toggle.ToggleClothing(user, (suit, comp), piece);
        });
        await server.WaitRunTicks(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(inventory.TryGetSlotEntity(user, "head", out var head), "the helmet should deploy");
            Assert.That(entMan.GetComponent<MetaDataComponent>(head!.Value).EntityPrototype?.ID,
                Does.Contain("PowerArmor"));
        });

        // A heavy hit is mostly absorbed by the suit's integrity.
        await server.WaitAssertion(() =>
        {
            var damageable = entMan.System<DamageableSystem>();
            var blunt = server.ProtoMan.Index(Blunt);
            damageable.TryChangeDamage(user, new DamageSpecifier(blunt, 100));
            var taken = damageable.GetTotalDamage((user, entMan.GetComponent<DamageableComponent>(user)));
            // Every worn piece with integrity (suit, helmet...) shares the hit.
            var soaked = 0f;
            var query = entMan.EntityQueryEnumerator<PowerArmorIntegrityComponent, DamageableComponent>();
            while (query.MoveNext(out var piece, out _, out var pieceDamage))
                soaked += damageable.GetTotalDamage((piece, pieceDamage)).Float();
            Assert.That(taken.Float(), Is.LessThan(30f), "most of the hit should not reach the wearer");
            Assert.That(soaked, Is.GreaterThan(0f), "the armor's integrity should take the hit");
        });
    }
}
