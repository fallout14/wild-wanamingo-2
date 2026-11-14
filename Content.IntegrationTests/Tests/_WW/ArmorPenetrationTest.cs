#nullable enable
using Content.Shared._WW.Armor;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Armour penetration, re-ported from Misfits: positive values (AP) let more damage through armour,
/// negative values (hollow points) let less through, and the AP/JHP rifle rounds carry their values again.
/// </summary>
[TestFixture]
public sealed class ArmorPenetrationTest
{
    [Test]
    public void HelperMath()
    {
        var set = new DamageModifierSet();
        set.Coefficients["Piercing"] = 0.6f;
        set.Coefficients["Heat"] = 1.2f; // a weakness, left alone
        set.FlatReductions["Blunt"] = 10f;

        Assert.That(ArmorPenetrationHelper.Apply(set, 0f), Is.SameAs(set));

        var ap = ArmorPenetrationHelper.Apply(set, 0.35f);
        Assert.That(ap.Coefficients["Piercing"], Is.EqualTo(0.74f).Within(0.0001f));
        Assert.That(ap.Coefficients["Heat"], Is.EqualTo(1.2f));
        Assert.That(ap.FlatReductions["Blunt"], Is.EqualTo(6.5f).Within(0.0001f));

        var jhp = ArmorPenetrationHelper.Apply(set, -0.3f);
        Assert.That(jhp.Coefficients["Piercing"], Is.EqualTo(0.48f).Within(0.0001f));
        Assert.That(jhp.FlatReductions["Blunt"], Is.EqualTo(13f).Within(0.0001f));

        Assert.That(ArmorPenetrationHelper.Apply(set, 5f).Coefficients["Piercing"], Is.EqualTo(1f), "clamped to 1");
        Assert.That(ArmorPenetrationHelper.Apply(set, -5f).Coefficients["Piercing"], Is.EqualTo(0.2f).Within(0.0001f), "clamped to -1");
    }

    [Test]
    public async Task ArmourOnAMob()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.System<DamageableSystem>();
            var inventory = entMan.System<InventorySystem>();
            var hit = new DamageSpecifier();
            hit.DamageDict["Piercing"] = 20;

            float Hit(float penetration, bool armoured)
            {
                var mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
                if (armoured)
                {
                    var vest = entMan.SpawnEntity("F14ClothingOuterCombatArmor", map.GridCoords);
                    Assert.That(entMan.GetComponent<ArmorComponent>(vest).Modifiers.Coefficients["Piercing"], Is.LessThan(1f));
                    Assert.That(inventory.TryEquip(mob, vest, "outerClothing", silent: true, force: true));
                }

                var dealt = damageable.ChangeDamage(mob, hit, armorPenetration: penetration).GetTotal().Float();
                entMan.DeleteEntity(mob);
                return dealt;
            }

            var bare = Hit(0f, false);
            var fmj = Hit(0f, true);
            var ap = Hit(0.35f, true);
            var jhp = Hit(-0.3f, true);

            Assert.Multiple(() =>
            {
                Assert.That(fmj, Is.LessThan(bare), "the vest stops some piercing");
                Assert.That(ap, Is.GreaterThan(fmj).And.LessThan(bare), "AP gets more through the vest");
                Assert.That(jhp, Is.LessThan(fmj), "hollow points get less through the vest");
                Assert.That(Hit(-0.3f, false), Is.EqualTo(bare), "penetration does nothing without armour");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RifleRoundsCarryValues()
    {
        await using var pair = await PoolManager.GetServerClient();
        var protos = pair.Server.ResolveDependency<IPrototypeManager>();
        var factory = pair.Server.ResolveDependency<IComponentFactory>();

        await pair.Server.WaitAssertion(() =>
        {
            float Pen(string id)
            {
                Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<ProjectileComponent>(out var proj, factory));
                return proj!.ArmorPenetration;
            }

            Assert.Multiple(() =>
            {
                Assert.That(Pen("F14Bullet762x51AP"), Is.EqualTo(0.35f));
                Assert.That(Pen("F14Bullet556AP"), Is.EqualTo(0.35f));
                Assert.That(Pen("F14Bullet308JHP"), Is.EqualTo(-0.3f));
                Assert.That(Pen("F14Bullet223JHP"), Is.EqualTo(-0.3f));
                Assert.That(Pen("F14Bullet762x51FMJ"), Is.Zero);
            });
        });

        await pair.CleanReturnAsync();
    }
}
