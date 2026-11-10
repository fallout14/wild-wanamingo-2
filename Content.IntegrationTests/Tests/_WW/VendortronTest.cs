#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._Forge.Trade;
using Content.Shared._Forge.Trade;
using Content.Shared._WW.Vendortrons;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Vendortrons (MS13 robot shopkeepers): buy-only stores with rolled stock, never cheaper than what any trade
/// terminal pays back for the same item, and bolted in place.
/// </summary>
[TestOf(typeof(WWVendortronSystem))]
public sealed class VendortronTest : GameTest
{
    private static readonly EntProtoId[] Vendortrons =
    [
        "WWMobVendortronGunRunners",
        "WWMobVendortronMedTek",
        "WWMobVendortronArcata",
        "WWMobVendortronTools",
        "WWMobVendortronRobCo",
    ];

    // Caps per unit of each store currency, from the terminals' currency exchange listings
    // (2 NCR dollars per cap, 4 caps per denarius).
    private static readonly Dictionary<string, double> CapsPerUnit = new()
    {
        ["F14Caps"] = 1,
        ["F14NCRDollar"] = 0.5,
        ["F14LegionDenarius"] = 4,
    };

    [Test]
    public async Task NoBuyLowSellHigh()
    {
        var server = Pair.Server;
        var protos = server.ProtoMan;
        var factory = server.EntMan.ComponentFactory;

        await server.WaitAssertion(() =>
        {
            // Best price any terminal pays for each item, in caps per unit.
            var sellBack = new Dictionary<string, (double Caps, string Preset)>();
            foreach (var ent in protos.EnumeratePrototypes<EntityPrototype>())
            {
                if (ent.Abstract || !ent.TryGetComponent<NcStoreComponent>(out var store, factory))
                    continue;

                foreach (var presetId in store.SellPresets)
                {
                    var preset = protos.Index<StorePresetStructuredPrototype>(presetId);
                    var rate = CapsPerUnit.GetValueOrDefault(preset.Currency, 1);
                    foreach (var cat in preset.Categories)
                    {
                        foreach (var e in protos.Index<StoreCategoryStructuredPrototype>(cat).Entries)
                        {
                            var caps = e.Price * rate / Math.Max(1, e.Amount);
                            if (!sellBack.TryGetValue(e.Proto, out var best) || caps > best.Caps)
                                sellBack[e.Proto] = (caps, presetId);
                        }
                    }
                }
            }

            Assert.Multiple(() =>
            {
                foreach (var id in Vendortrons)
                {
                    var store = protos.Index(id).Components["NcStore"].Component as NcStoreComponent;
                    Assert.That(store, Is.Not.Null);
                    Assert.That(store!.SellPresets, Is.Empty, $"{id}: vendortrons only sell, like MS13's");
                    foreach (var presetId in store.BuyPresets)
                    {
                        var preset = protos.Index<StorePresetStructuredPrototype>(presetId);
                        Assert.That(preset.Currency, Is.EqualTo("F14Caps"));
                        foreach (var cat in preset.Categories)
                        {
                            foreach (var e in protos.Index<StoreCategoryStructuredPrototype>(cat).Entries)
                            {
                                Assert.That(protos.HasIndex<EntityPrototype>(e.Proto), $"{cat}: {e.Proto} doesn't exist");
                                var caps = (double) e.Price / Math.Max(1, e.Amount);
                                if (sellBack.TryGetValue(e.Proto, out var best))
                                {
                                    Assert.That(caps, Is.GreaterThan(best.Caps),
                                        $"{cat}: {e.Proto} costs {caps} caps/unit but {best.Preset} pays {best.Caps}");
                                }
                            }
                        }
                    }
                }
            });
        });
    }

    [Test]
    public async Task StockIsRolledAndBuyingUsesIt()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protos = server.ProtoMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var loader = entMan.System<StoreSystemStructuredLoader>();
            var logic = entMan.System<NcStoreLogicSystem>();

            foreach (var id in Vendortrons)
            {
                var vendor = entMan.SpawnEntity(id, map.GridCoords);
                var store = entMan.GetComponent<NcStoreComponent>(vendor);
                loader.EnsureLoaded(vendor, store, "test");
                Assert.That(store.Listings, Is.Not.Empty, id.ToString());
                Assert.That(store.Listings.All(l => l.Mode == StoreMode.Buy), id.ToString());

                // Listing ids are preset:mode:category:proto; stock must land inside the entry's countMin..count.
                foreach (var listing in store.Listings)
                {
                    var parts = listing.Id.Split(':');
                    var entry = protos.Index<StoreCategoryStructuredPrototype>(parts[2]).Entries.First(e => e.Proto == parts[3]);
                    Assert.That(listing.RemainingCount, Is.InRange(entry.CountMin ?? entry.Count ?? -1, entry.Count ?? -1), listing.Id);
                }

                // Buy the cheapest thing in stock.
                var buy = store.Listings.Where(l => l.RemainingCount > 0).MinBy(l => l.Cost["F14Caps"]);
                Assert.That(buy, Is.Not.Null, $"{id} rolled no stock at all");
                var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
                var caps = entMan.SpawnEntity("F14CurrencyCap500", map.GridCoords);
                Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, caps));
                var stockBefore = buy!.RemainingCount;

                Assert.That(logic.TryBuy(buy.Id, vendor, store, user), $"{id}: buying {buy.ProductEntity} failed");
                Assert.That(buy.RemainingCount, Is.EqualTo(stockBefore - 1));
                Assert.That(entMan.GetComponent<StackComponent>(caps).Count, Is.LessThan(500), "it should cost caps");

                entMan.DeleteEntity(user);
                entMan.DeleteEntity(vendor);
            }
        });
    }

    [Test]
    public async Task CantBeDragged()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var vendor = entMan.SpawnEntity("WWMobVendortronRobCo", map.GridCoords);
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(entMan.System<PullingSystem>().TryStartPull(user, vendor), Is.False);
        });
    }
}
