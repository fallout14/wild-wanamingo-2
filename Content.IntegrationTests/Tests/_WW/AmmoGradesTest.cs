#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Research.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The ammo grades added 2026-11-14 (hollow point, AP, +P, surplus, incendiary, shotgun shells and hand loads).
/// </summary>
public sealed class AmmoGradesTest : GameTest
{
    private static readonly string[] BenchBoxes =
    {
        "F14MagazineBox22JHP", "F14MagazineBox22Surplus", "F14MagazineBox5mmAP", "F14MagazineBox9mmJHP",
        "F14MagazineBox9mmPlusP", "F14MagazineBox38JHP", "F14MagazineBox10mmJHP", "F14MagazineBox10mmPlusP",
        "F14MagazineBox45JHP", "F14MagazineBox45PlusP", "F14MagazineBox357PlusP", "F14MagazineBox127JHP",
        "F14MagazineBox556Surplus", "F14MagazineBox762Surplus", "F14MagazineBox762AP", "F14MagazineBox762Incendiary",
        "F14MagazineBox303JHP", "F14MagazineBox308Incendiary", "F14MagazineBox50Incendiary",
        "F14MagazineBox12gaugeMagnum", "F14MagazineBox12gaugeSlug", "F14MagazineBox12gaugeCoin",
        "F14MagazineBox12gaugeDragonsBreath", "F14MagazineBox20gaugeMagnum", "F14MagazineBox20gaugeSlug",
        "F14MagazineBox20gaugeCoin", "F14MagazineBox12gaugeFlare",
    };

    private static readonly string[] HandLoadBoxes =
    {
        "F14MagazineBox5mmJSP", "F14MagazineBox45Super", "F14MagazineBox357JFP", "F14MagazineBox44SWC",
        "F14MagazineBox4570SWC", "F14MagazineBox556Match", "F14MagazineBox50Match",
        "F14MagazineBox12gaugeBuckMagnum", "F14MagazineBox20gaugeBuckMagnum",
    };

    /// <summary>
    /// Every grade box spawns full, and its rounds fit wherever the caliber's standard rounds do: the standard box
    /// (its parent) accepts them, and so does every magazine that takes the standard cartridge, by the same tag.
    /// </summary>
    [Test]
    public async Task GradesFitTheirCaliber()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protos = server.ProtoMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var guns = entMan.System<SharedGunSystem>();
            var whitelist = entMan.System<EntityWhitelistSystem>();
            Assert.Multiple(() =>
            {
                foreach (var id in BenchBoxes.Concat(HandLoadBoxes))
                {
                    var box = entMan.SpawnEntity(id, map.GridCoords);
                    var provider = entMan.GetComponent<BallisticAmmoProviderComponent>(box);
                    Assert.That(guns.GetAmmoCount(box), Is.EqualTo(provider.Capacity).And.GreaterThan(0), id);

                    var parentId = protos.Index<EntityPrototype>(id).Parents![0];
                    var standard = entMan.SpawnEntity(parentId, map.GridCoords);
                    var standardProvider = entMan.GetComponent<BallisticAmmoProviderComponent>(standard);
                    var round = entMan.SpawnEntity(provider.Proto!.Value, map.GridCoords);
                    Assert.That(whitelist.IsWhitelistPassOrNull(standardProvider.Whitelist, round), $"{id} rounds fit {parentId}");
                    Assert.That(provider.Proto, Is.Not.EqualTo(standardProvider.Proto), id);
                }
            });
        });
    }

    /// <summary>Bench grades are on the ammo bench; hand loads only come from the hand loader's notebook.</summary>
    [Test]
    public async Task HandLoadsNeedTheNotebook()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            var bench = protos.Index<LatheRecipePackPrototype>("F14WorkbenchAmmobenchStatic").Recipes.Select(r => r.Id).ToHashSet();
            Assert.That(protos.Index<EntityPrototype>("F14BlueprintHandLoads").TryGetComponent<BlueprintComponent>(out var notebook, factory));
            var provided = notebook!.ProvidedRecipes.Select(r => r.Id).ToHashSet();
            Assert.Multiple(() =>
            {
                foreach (var id in BenchBoxes)
                    Assert.That(bench, Does.Contain(id));
                foreach (var id in HandLoadBoxes)
                {
                    Assert.That(provided, Does.Contain(id));
                    Assert.That(bench, Does.Not.Contain(id));
                }
            });
        });
    }

    /// <summary>Hollow points hit harder but armour stops more; AP hits softer but armour stops less.</summary>
    [Test]
    public async Task HollowPointAndArmourPiercing()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            ProjectileComponent P(string id)
            {
                Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<ProjectileComponent>(out var p, factory), id);
                return p!;
            }

            Assert.Multiple(() =>
            {
                foreach (var (standard, jhp) in new[] { ("F14Bullet9mm", "F14Bullet9mmJHP"), ("F14Bullet10mm", "F14Bullet10mmJHP"), ("F14Bullet303", "F14Bullet303JHP") })
                {
                    Assert.That(P(jhp).Damage.GetTotal(), Is.GreaterThan(P(standard).Damage.GetTotal()), jhp);
                    Assert.That(P(jhp).ArmorPenetration, Is.LessThan(0f), jhp);
                }
                foreach (var (standard, ap) in new[] { ("F14Bullet5mm", "F14Bullet5mmAP"), ("F14Bullet762", "F14Bullet762AP") })
                {
                    Assert.That(P(ap).Damage.GetTotal(), Is.LessThan(P(standard).Damage.GetTotal()), ap);
                    Assert.That(P(ap).ArmorPenetration, Is.GreaterThan(0f), ap);
                }
                Assert.That(P("F14Bullet303JHP").Damage.DamageDict["Piercing"].Float(), Is.EqualTo(43f));
            });
        });
    }
}
