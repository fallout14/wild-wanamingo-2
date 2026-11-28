#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// MS13 clothing import (2026-11-20, Reports/porter/ms13/import_clothing.py): every F14Clothing*MS13* item (WWClothing* until the 2026-11-28 F14 rename) has an
/// icon and the worn sprite for each slot it fits, and can be put on a human in that slot.
/// </summary>
public sealed class MS13ClothingTest : GameTest
{
    private static readonly (SlotFlags Slot, string Name, string State)[] Slots =
    [
        (SlotFlags.HEAD, "head", "equipped-HELMET"),
        (SlotFlags.OUTERCLOTHING, "outerClothing", "equipped-OUTERCLOTHING"),
        (SlotFlags.INNERCLOTHING, "jumpsuit", "equipped-INNERCLOTHING"),
        (SlotFlags.FEET, "shoes", "equipped-FEET"),
        (SlotFlags.GLOVES, "gloves", "equipped-HAND"),
        (SlotFlags.MASK, "mask", "equipped-MASK"),
        (SlotFlags.EYES, "eyes", "equipped-EYES"),
        (SlotFlags.NECK, "neck", "equipped-NECK"),
        (SlotFlags.BACK, "back", "equipped-BACKPACK"),
        (SlotFlags.BELT, "belt", "equipped-BELT"),
    ];

    private static bool IsMS13Clothing(EntityPrototype p) =>
        !p.Abstract && p.ID.StartsWith("F14Clothing") && p.ID.Contains("MS13");

    [Test]
    public async Task MS13ClothingHasSprites()
    {
        var client = Pair.Client;
        var res = client.ResolveDependency<IResourceManager>();
        var count = 0;

        await client.WaitAssertion(() =>
        {
            var entMan = client.EntMan;
            Assert.Multiple(() =>
            {
                foreach (var proto in client.ProtoMan.EnumeratePrototypes<EntityPrototype>().Where(IsMS13Clothing))
                {
                    count++;
                    var ent = entMan.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
                    var sprite = entMan.GetComponent<SpriteComponent>(ent);
                    var clothing = entMan.GetComponent<ClothingComponent>(ent);
                    var rsi = sprite.BaseRSI?.Path ?? ResPath.Empty;
                    Assert.That(rsi.ToString(), Does.Contain("MojaveSun/Clothing"), $"{proto.ID}: not an MS13 sprite");
                    Assert.That(res.ContentFileExists(rsi / "icon.png"), $"{proto.ID}: no icon in {rsi}");
                    var clothingRsi = clothing.RsiPath != null ? new ResPath("/Textures") / clothing.RsiPath : rsi;
                    var slots = Slots.Where(s => (clothing.Slots & s.Slot) != 0).ToList();
                    Assert.That(slots, Is.Not.Empty, $"{proto.ID}: fits no slot");
                    foreach (var (_, _, state) in slots)
                        Assert.That(res.ContentFileExists(clothingRsi / $"{state}.png"), $"{proto.ID}: no {state} in {clothingRsi}");
                    entMan.DeleteEntity(ent);
                }
            });
        });

        Assert.That(count, Is.GreaterThan(350), "MS13 clothing went missing");
    }

    [Test]
    public async Task MS13ClothingCanBeWorn()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var ids = server.ProtoMan.EnumeratePrototypes<EntityPrototype>().Where(IsMS13Clothing).Select(p => p.ID).ToList();

        await server.WaitAssertion(() =>
        {
            var inv = entMan.System<InventorySystem>();
            var human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.Multiple(() =>
            {
                foreach (var id in ids)
                {
                    var item = entMan.SpawnEntity(id, map.GridCoords);
                    var clothing = entMan.GetComponent<ClothingComponent>(item);
                    var slot = Slots.First(s => (clothing.Slots & s.Slot) != 0).Name;
                    if (inv.TryGetSlotEntity(human, slot, out var old))
                        entMan.DeleteEntity(old);
                    Assert.That(inv.TryEquip(human, item, slot, silent: true, force: true), $"{id}: can't be worn in {slot}");
                    Assert.That(inv.TryGetSlotEntity(human, slot, out var worn) && worn == item, $"{id}: not in {slot}");
                }
            });
            entMan.DeleteEntity(human);
        });
    }
}
