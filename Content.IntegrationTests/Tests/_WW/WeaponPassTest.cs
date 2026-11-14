#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Scope;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Weapon pass 2026-11-10: magazines, rechambers, removals, Wizden-style scopes with magnification,
/// and the Mosin-style right-click bayonet stab.
/// </summary>
public sealed class WeaponPassTest : GameTest
{
    [Test]
    public async Task MagazinesAndChamberings()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            int Capacity(string id) => entMan.GetComponent<BallisticAmmoProviderComponent>(entMan.SpawnEntity(id, map.GridCoords)).Capacity;
            bool Takes(string gunId, string slot, string itemId)
            {
                var gun = entMan.SpawnEntity(gunId, map.GridCoords);
                slots.TryEject(gun, slot, null, out _);
                return slots.TryGetSlot(gun, slot, out var s) && slots.CanInsert(gun, s, entMan.SpawnEntity(itemId, map.GridCoords), null);
            }

            Assert.Multiple(() =>
            {
                Assert.That(Capacity("F14Magazine45SubMachineGun"), Is.EqualTo(30));
                Assert.That(Capacity("F14Magazine45SubMachineGun20"), Is.EqualTo(20));
                Assert.That(Takes("F14WeaponSMG45Reising", "gun_magazine", "F14Magazine45SubMachineGun20"), Is.True);
                Assert.That(Takes("F14WeaponSMG45Reising", "gun_magazine", "F14Magazine45SubMachineGun"), Is.False, "Reising: 20 round only");
                Assert.That(Takes("F14WeaponSMG45Thompson", "gun_magazine", "F14Magazine45SubMachineGun20"), Is.True);
                Assert.That(Takes("F14WeaponSMG45Thompson", "gun_magazine", "F14Magazine45SubMachineGun"), Is.True);

                Assert.That(Takes("F14WeaponRifle303MarksmanCanadian", "gun_magazine", "F14Magazine303Short"), Is.True);
                Assert.That(Takes("F14WeaponRifle303MarksmanCanadian", "gun_magazine", "F14Magazine762AmmoShort"), Is.False);
                Assert.That(entMan.GetComponent<BallisticAmmoProviderComponent>(entMan.SpawnEntity("F14WeaponSniper303Canadian", map.GridCoords)).Proto,
                    Is.EqualTo(new EntProtoId("F14Cartridge303")));
                Assert.That(entMan.GetComponent<RevolverAmmoProviderComponent>(entMan.SpawnEntity("F14WeaponDerringer", map.GridCoords)).FillPrototype,
                    Is.EqualTo(new EntProtoId("F14Cartridge4570")));

                // Butchered rifle: scope yes, suppressor/bayonet no.
                Assert.That(Takes("F14WeaponRifle556Butchered", "weapon_optic", "F14WeaponScopeAttachment"), Is.True);
                Assert.That(Takes("F14WeaponRifle556Butchered", "weapon_muzzle", "F14FirearmSuppressor"), Is.False);
                Assert.That(Takes("F14WeaponRifle556Butchered", "weapon_muzzle", "F14KnifeBayonet"), Is.False);

                // 12 shots per small energy cell.
                foreach (var id in new[] { "F14WeaponLaserPistolWattz", "F14WeaponLaserMagnetoPistol" })
                {
                    var gun = entMan.SpawnEntity(id, map.GridCoords);
                    var ev = new GetAmmoCountEvent();
                    entMan.EventBus.RaiseLocalEvent(gun, ref ev);
                    Assert.That(ev.Count, Is.EqualTo(12), id);
                }

                Assert.That(Takes("F14WeaponHMGcanadian", "gun_magazine", "F14Magazine303Belt"), Is.True, ".303 HMG");
                Assert.That(Takes("F14WeaponHMGcanadian", "gun_magazine", "F14Magazine762AmmoBelt"), Is.False);
                Assert.That(entMan.GetComponent<GunComponent>(entMan.SpawnEntity("F14WeaponPlasmaCaster", map.GridCoords)).SelectedMode,
                    Is.EqualTo(Content.Shared.Weapons.Ranged.Components.SelectiveFire.SemiAuto));
                Assert.That(entMan.GetComponent<GunComponent>(entMan.SpawnEntity("F14WeaponPlasmaCasterTurbo", map.GridCoords)).FireRate,
                    Is.GreaterThan(entMan.GetComponent<GunComponent>(entMan.SpawnEntity("F14WeaponPlasmaCaster", map.GridCoords)).FireRate));
            });
        });
    }

    [Test]
    public async Task RemovedAndRenamed()
    {
        var protos = Pair.Server.ProtoMan;
        await Pair.Server.WaitAssertion(() =>
        {
            foreach (var gone in new[] { "F14WeaponPistolM1911", "F14WeaponPistolM1911Custom", "F14WeaponRangerPowerArmorLMG",
                         "F14WeaponRangerPowerArmorPistol", "F14WeaponUSHMG", "F14WeaponSniper308Canadian", "F14WeaponRifle762MarksmanCanadian" })
                Assert.That(protos.HasIndex<EntityPrototype>(gone), Is.False, gone);
            Assert.That(protos.Index<EntityPrototype>("F14WeaponM240B").Name, Is.EqualTo("M240B"));
            Assert.That(protos.Index<EntityPrototype>("F14WeaponM240B").Description, Does.Not.Contain("egion"));
            Assert.That(protos.Index<EntityPrototype>("F14WeaponM60GPMG").Description, Does.Not.Contain("egion"));
            Assert.That(protos.Index<EntityPrototype>("F14WeaponRifle762SKS").Name, Is.EqualTo("Chinese carbine"));
            Assert.That(protos.HasIndex<EntityPrototype>("F14WeaponSMG9mminfiltrator"), Is.False, "the infiltrator is now F14WeaponRifle556Infiltrator");
        });
    }

    [Test]
    public async Task ScopesUseMagnification()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var scopes = entMan.System<WWScopeSystem>();
            var factory = entMan.ComponentFactory;
            Assert.Multiple(() =>
            {
                // No gun or item uses the old Misfits toggle scope any more.
                foreach (var proto in server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
                    Assert.That(proto.Abstract || !proto.TryGetComponent<ScopeComponent>(out _, factory), Is.True, proto.ID);

                foreach (var (id, mag) in new[] { ("F14WeaponScope2x", 2f), ("F14WeaponScope2_5x", 2.5f), ("F14WeaponScope3x", 3f),
                             ("F14WeaponScopeAttachment", 4f), ("F14WeaponScope6x", 6f), ("F14WeaponScope8x", 8f) })
                {
                    var scope = entMan.GetComponent<WWScopeComponent>(entMan.SpawnEntity(id, map.GridCoords));
                    Assert.That(scope.Magnification, Is.EqualTo(mag), id);
                    Assert.That(scope.MaxOffset, Is.EqualTo(mag * 2f), id);
                }

                // Mount a 6x on a hunting rifle: the view is unlocked only after scoping in, and only while wielded.
                var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
                var rifle = entMan.SpawnEntity("F14WeaponSniperHunting", map.GridCoords);
                var six = entMan.SpawnEntity("F14WeaponScope6x", map.GridCoords);
                Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(rifle, "weapon_optic", six, null));
                Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, rifle));
                Assert.That(entMan.System<SharedWieldableSystem>().TryWield(rifle, user));
                Assert.That(scopes.TryGetActiveScope(rifle, out _), Is.False, "wielded but not scoped in");
                var sixScope = (six, entMan.GetComponent<WWScopeComponent>(six));
                Assert.That(scopes.GetGun(six), Is.EqualTo(rifle));
                scopes.SetActive(sixScope, true, user);
                Assert.That(scopes.TryGetActiveScope(rifle, out var active) && active.Value.Comp.MaxOffset == 12f);
                entMan.System<SharedWieldableSystem>().TryUnwield(rifle, user);
                Assert.That(sixScope.Item2.Active, Is.False, "lowering the gun drops out of the scope");

                // Scope-in time scales with Perception: slow at 1, about instant at 10.
                var special = entMan.System<Content.Shared._Misfits.Special.SharedSpecialSystem>();
                var perception = Content.Shared._Misfits.Special.SpecialStat.Perception;
                entMan.EnsureComponent<Content.Shared._Misfits.Special.Components.SpecialComponent>(user);
                Assert.That(special.TrySetBase(user, perception, 1));
                var slow = scopes.GetScopeDelay(sixScope, user);
                Assert.That(special.TrySetBase(user, perception, 10));
                var fast = scopes.GetScopeDelay(sixScope, user);
                Assert.That(slow, Is.GreaterThan(2.5f), "PER 1");
                Assert.That(fast, Is.LessThan(0.15f), "PER 10");

                // The scope is a button on the gun.
                Assert.That(entMan.GetComponent<FirearmAttachmentHostComponent>(rifle).OpticToggleActionEntity, Is.Not.Null, "scope action");

                // Variable scope cycles.
                var variable = entMan.SpawnEntity("F14WeaponScopeVariable", map.GridCoords);
                var v = entMan.GetComponent<WWScopeComponent>(variable);
                Assert.That(v.Magnification, Is.EqualTo(2f));
                scopes.Cycle((variable, v), null);
                Assert.That(v.Magnification, Is.EqualTo(4f));

                // Every gun that can be scoped can be wielded (the view only follows the cursor while wielded).
                foreach (var proto in server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || !proto.TryGetComponent<GunComponent>(out _, factory))
                        continue;
                    var scoped = proto.TryGetComponent<WWScopeComponent>(out _, factory) ||
                                 proto.TryGetComponent<FirearmAttachmentHostComponent>(out var host, factory) && host.EnableOpticSlot;
                    if (scoped)
                        Assert.That(proto.TryGetComponent<WieldableComponent>(out _, factory), $"{proto.ID} has a scope but can't be wielded");
                }
            });
        });
    }

    [Test]
    public async Task BayonetStab()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var melee = entMan.System<SharedMeleeWeaponSystem>();
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var rifle = entMan.SpawnEntity("F14WeaponRifle308Battle", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, rifle));

            bool CanStab()
            {
                var ev = new AttemptMeleeEvent(user);
                entMan.EventBus.RaiseLocalEvent(rifle, ref ev);
                return !ev.Cancelled;
            }

            Assert.That(CanStab(), Is.False, "no bayonet: no stab");
            var bayonet = entMan.SpawnEntity("F14KnifeBayonet", map.GridCoords);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(rifle, "weapon_muzzle", bayonet, null));
            Assert.That(entMan.HasComponent<AltFireMeleeComponent>(rifle) && entMan.HasComponent<MeleeWeaponComponent>(rifle),
                "mounting a bayonet gives the gun right-click melee");
            Assert.That(CanStab(), Is.False, "bayonet but not wielded");
            Assert.That(entMan.System<SharedWieldableSystem>().TryWield(rifle, user));
            Assert.That(CanStab(), Is.True);

            // Wielded stab = a wielded spear.
            var spear = entMan.SpawnEntity("F14WastelandSpear", map.GridCoords);
            var spearUser = entMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(spearUser, spear));
            Assert.That(entMan.System<SharedWieldableSystem>().TryWield(spear, spearUser));
            var stab = melee.GetDamage(rifle, user);
            var spearHit = melee.GetDamage(spear, spearUser);
            Assert.That(stab.GetTotal(), Is.EqualTo(spearHit.GetTotal()), "bayonet stab should match a wielded spear");

            // Taking it off removes the melee again.
            entMan.System<SharedWieldableSystem>().TryUnwield(rifle, user);
            Assert.That(entMan.System<ItemSlotsSystem>().TryEject(rifle, "weapon_muzzle", null, out _));
            Assert.That(entMan.HasComponent<AltFireMeleeComponent>(rifle) || entMan.HasComponent<MeleeWeaponComponent>(rifle), Is.False);

            // Off the rifle the bayonet is a knife.
            var knife = entMan.SpawnEntity("F14KnifeBayonet", map.GridCoords);
            var combatKnife = entMan.SpawnEntity("CombatKnife", map.GridCoords);
            Assert.That(entMan.GetComponent<MeleeWeaponComponent>(knife).Damage.GetTotal(),
                Is.EqualTo(entMan.GetComponent<MeleeWeaponComponent>(combatKnife).Damage.GetTotal()));

            // Every gun that takes a bayonet can stab with it: melee weapon + wieldable.
            var factory = entMan.ComponentFactory;
            Assert.Multiple(() =>
            {
                foreach (var proto in server.ProtoMan.EnumeratePrototypes<EntityPrototype>().Where(p => !p.Abstract))
                {
                    if (!proto.TryGetComponent<FirearmAttachmentHostComponent>(out _, factory))
                        continue;
                    var gun = entMan.SpawnEntity(proto.ID, map.GridCoords);
                    var host = entMan.GetComponent<FirearmAttachmentHostComponent>(gun);
                    var slots = entMan.System<ItemSlotsSystem>();
                    if (slots.TryGetSlot(gun, host.MuzzleSlotId, out var slot) &&
                        slots.TryInsert(gun, slot, entMan.SpawnEntity("F14KnifeBayonet", map.GridCoords), null))
                    {
                        Assert.That(entMan.HasComponent<MeleeWeaponComponent>(gun), $"{proto.ID} takes a bayonet but has no melee");
                        Assert.That(entMan.HasComponent<WieldableComponent>(gun), $"{proto.ID} takes a bayonet but can't be wielded");
                    }
                    entMan.DeleteEntity(gun);
                }
            });
        });
    }
}
