#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Mojave Sun 13 weapon sprites (2026-11-09): every weapon drawn with an MS13 sprite has its in-hand sprites,
/// and uses MS13's worn sprite for a slot whenever MS13 has one. The new MS13 guns load and fire.
/// </summary>
public sealed class MS13WeaponsTest : GameTest
{
    private static readonly string[] NewGuns =
    [
        "F14WeaponPistol10mmPolice", "F14WeaponPistol10mmMilitary", "F14WeaponDerringer", "F14WeaponDerringerGold",
        "F14WeaponLaserPistolWattz", "F14WeaponNailgun", "F14WeaponNeedler",
        "F14WeaponDesertEagle44", "F14WeaponPistol223", "F14WeaponShotgunSawedOff", "F14WeaponLaserMagnetoPistol",
    ];

    private static readonly (SlotFlags Slot, string State)[] WornStates =
    [
        (SlotFlags.BACK, "equipped-BACKPACK"),
        (SlotFlags.SUITSTORAGE, "equipped-SUITSTORAGE"),
        (SlotFlags.BELT, "equipped-BELT"),
    ];

    [Test]
    public async Task MS13WeaponsHaveInhandAndWornSprites()
    {
        var client = Pair.Client;
        var res = client.ResolveDependency<IResourceManager>();
        var checkedCount = 0;

        await client.WaitAssertion(() =>
        {
            var entMan = client.EntMan;
            Assert.Multiple(() =>
            {
                foreach (var proto in client.ProtoMan.EnumeratePrototypes<EntityPrototype>().OrderBy(p => p.ID))
                {
                    if (proto.Abstract || !proto.Components.ContainsKey("Sprite") || !proto.Components.ContainsKey("Item"))
                        continue;

                    var ent = entMan.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
                    var sprite = entMan.GetComponent<SpriteComponent>(ent);
                    var item = entMan.GetComponent<ItemComponent>(ent);
                    var itemRsi = item.RsiPath != null
                        ? new ResPath("/Textures") / item.RsiPath
                        : sprite.BaseRSI?.Path ?? ResPath.Empty;

                    if (!itemRsi.ToString().Contains("MojaveSun/Objects/Weapons") || itemRsi.ToString().Contains("Ammunition"))
                    {
                        entMan.DeleteEntity(ent);
                        continue;
                    }

                    checkedCount++;
                    bool Has(ResPath rsi, string state) => res.ContentFileExists(rsi / $"{state}.png");
                    var prefix = item.HeldPrefix == null ? "" : $"{item.HeldPrefix}-";
                    // MS13 has no in-hand sheath sprite (Wizden's sabre sheath has none either).
                    foreach (var side in proto.ID == "F14KatanaSheath" ? [] : new[] { "left", "right" })
                        Assert.That(Has(itemRsi, $"{prefix}inhand-{side}"), $"{proto.ID}: no {prefix}inhand-{side} in {itemRsi}");

                    if (entMan.TryGetComponent<WieldableComponent>(ent, out var wield) && wield.WieldedInhandPrefix != null)
                        Assert.That(Has(itemRsi, $"{wield.WieldedInhandPrefix}-inhand-left"), $"{proto.ID}: wieldable but no wielded in-hands");

                    if (entMan.TryGetComponent<ClothingComponent>(ent, out var clothing))
                    {
                        var clothingRsi = clothing.RsiPath != null ? new ResPath("/Textures") / clothing.RsiPath : itemRsi;
                        foreach (var (slot, state) in WornStates)
                        {
                            if ((clothing.Slots & slot) == 0 || !Has(itemRsi, state))
                                continue;
                            Assert.That(Has(clothingRsi, state) && clothingRsi == itemRsi, Is.True,
                                $"{proto.ID}: MS13 has a {state} sprite but the item is drawn with {clothingRsi}");
                        }
                    }

                    entMan.DeleteEntity(ent);
                }
            });
        });

        Assert.That(checkedCount, Is.GreaterThan(50), "MS13 weapons went missing");
    }

    [Test]
    public async Task NewMS13GunsFire()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in NewGuns)
                {
                    var gun = entMan.SpawnEntity(id, map.GridCoords);
                    if (entMan.HasComponent<ChamberMagazineAmmoProviderComponent>(gun))
                    {
                        // Chamber + magazine guns: the starting magazine is in and loaded.
                        var mag = entMan.System<ItemSlotsSystem>().GetItemOrNull(gun, "gun_magazine");
                        Assert.That(mag, Is.Not.Null, $"{id} has no magazine");
                        var count = new GetAmmoCountEvent();
                        if (mag != null)
                            entMan.EventBus.RaiseLocalEvent(mag.Value, ref count);
                        Assert.That(count.Count, Is.GreaterThan(0), $"{id}'s magazine is empty");
                        continue;
                    }

                    var ev = new TakeAmmoEvent(1, new List<(EntityUid? Entity, IShootable Shootable)>(),
                        entMan.GetComponent<TransformComponent>(gun).Coordinates, null);
                    entMan.EventBus.RaiseLocalEvent(gun, ev);
                    Assert.That(ev.Ammo, Has.Count.EqualTo(1), $"{id} didn't fire");
                }
            });
        });
    }

    [Test]
    public async Task KatanaSheathTakesKatanas()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            foreach (var katana in new[] { "F14Katana", "F14KatanaRed" })
            {
                var sheath = entMan.SpawnEntity("F14KatanaSheath", map.GridCoords);
                var blade = entMan.SpawnEntity(katana, map.GridCoords);
                Assert.That(slots.TryInsert(sheath, "item", blade, null), katana);
            }

            var sheath2 = entMan.SpawnEntity("F14KatanaSheath", map.GridCoords);
            Assert.That(slots.TryInsert(sheath2, "item", entMan.SpawnEntity("F14Machete", map.GridCoords), null), Is.False);
        });
    }
}
