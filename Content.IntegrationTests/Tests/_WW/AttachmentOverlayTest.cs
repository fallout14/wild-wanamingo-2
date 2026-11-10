#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Scope;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Every gun that can take an attachment shows it: for each attachment a gun's slots accept, there is an overlay
/// state to draw and a position for it (TODO 4). Placement itself still needs eyes in game.
/// </summary>
[TestOf(typeof(FirearmAttachmentHostComponent))]
public sealed class AttachmentOverlayTest : GameTest
{
    [Test]
    public async Task EveryAcceptedAttachmentHasAnOverlay()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var res = server.ResolveDependency<IResourceManager>();
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            var factory = entMan.ComponentFactory;
            var suppressor = entMan.SpawnEntity("F14FirearmSuppressor", map.GridCoords);
            var bayonet = entMan.SpawnEntity("F14KnifeBayonet", map.GridCoords);
            var scope = entMan.SpawnEntity("F14WeaponScopeAttachment", map.GridCoords);
            var junk = entMan.SpawnEntity("F14Wrench", map.GridCoords);
            var defaultRsi = new FirearmAttachmentHostComponent().OpticVisualRsi;
            var hosts = 0;

            bool StateExists(ResPath rsi, string state) => res.ContentFileExists(rsi / $"{state}.png");

            Assert.Multiple(() =>
            {
                foreach (var proto in server.ProtoMan.EnumeratePrototypes<EntityPrototype>().OrderBy(p => p.ID))
                {
                    if (proto.Abstract || !proto.TryGetComponent<FirearmAttachmentHostComponent>(out _, factory))
                        continue;

                    hosts++;
                    var gun = entMan.SpawnEntity(proto.ID, map.GridCoords);
                    var host = entMan.GetComponent<FirearmAttachmentHostComponent>(gun);
                    bool Accepts(string slotId, EntityUid item) =>
                        slots.TryGetSlot(gun, slotId, out var slot) && slots.CanInsert(gun, slot, item, null);

                    Assert.That(Accepts(host.MuzzleSlotId, junk), Is.False, $"{proto.ID}: muzzle slot takes any item");

                    var orientation = host.VisualOrientation == FirearmAttachmentVisualOrientation.Horizontal
                        ? "horizontal"
                        : "diagonal";
                    if (host.ShowMuzzleVisual && Accepts(host.MuzzleSlotId, suppressor))
                    {
                        var state = host.SuppressorVisualState ?? $"suppressor-{orientation}";
                        Assert.That(StateExists(host.MuzzleVisualRsi, state), $"{proto.ID}: no '{state}' state");
                        Assert.That(host.SuppressorVisualOffset ?? host.MuzzleVisualOffset, Is.Not.EqualTo(Vector2.Zero),
                            $"{proto.ID}: suppressor has no position (drawn on the middle of the gun)");
                    }

                    if (host.ShowMuzzleVisual && Accepts(host.MuzzleSlotId, bayonet))
                    {
                        var state = host.BayonetVisualState ?? $"bayonet-{orientation}";
                        Assert.That(StateExists(host.MuzzleVisualRsi, state), $"{proto.ID}: no '{state}' state");
                        Assert.That(host.BayonetVisualOffset ?? host.MuzzleVisualOffset, Is.Not.EqualTo(Vector2.Zero),
                            $"{proto.ID}: bayonet has no position (drawn on the middle of the gun)");
                    }

                    var takesScope = Accepts(host.OpticSlotId, scope);
                    if (takesScope && host.ShowOpticVisual)
                    {
                        Assert.That(host.OpticVisualState, Is.Not.Null, $"{proto.ID}: takes a scope but has no scope overlay");
                        if (host.OpticVisualState != null)
                        {
                            Assert.That(StateExists(host.OpticVisualRsi, host.OpticVisualState),
                                $"{proto.ID}: no '{host.OpticVisualState}' state in {host.OpticVisualRsi}");
                        }

                        // The shared scope art needs moving onto the gun; a gun's own scope state is drawn in place.
                        if (host.OpticVisualRsi == defaultRsi)
                            Assert.That(host.OpticVisualOffset, Is.Not.EqualTo(Vector2.Zero), $"{proto.ID}: scope has no position");
                    }

                    Assert.That(takesScope && entMan.HasComponent<ScopeComponent>(gun), Is.False,
                        $"{proto.ID}: has a built-in scope and a scope slot");

                    entMan.DeleteEntity(gun);
                }
            });

            Assert.That(hosts, Is.GreaterThan(50), "attachment hosts went missing");
        });
    }
}
