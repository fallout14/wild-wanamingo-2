#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared._WW.Weapons;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Weapon pass 2026-11-12: one sledgehammer and one fire axe, the wood axe name, magazine visuals on the M60 and the
/// MS13 pistols, and the Pitt-style infiltrator (5.56mm, semi-auto, locked suppressor, fixed 4x scope).
/// </summary>
public sealed class WeaponPass2Test : GameTest
{
    [Test]
    public async Task MeleeMerge()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;
        await Pair.Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(protos.HasIndex<EntityPrototype>("F14SledgeHammer"), Is.False);
                var sledge = protos.Index<EntityPrototype>("F14Sledgehammer");
                Assert.That(sledge.TryGetComponent<MeleeWeaponComponent>(out var melee, factory), Is.True);
                Assert.That(melee!.Damage.DamageDict["Blunt"].Float(), Is.EqualTo(15f));
                Assert.That(melee.AttackRate, Is.EqualTo(1.35f));
                Assert.That(sledge.TryGetComponent<IncreaseDamageOnWieldComponent>(out var wield, factory), Is.True);
                Assert.That(wield!.BonusDamage.DamageDict["Blunt"].Float(), Is.EqualTo(45f));

                Assert.That(protos.HasIndex<EntityPrototype>("F14FireAxeBase"), Is.False, "abstract: F14FireAxe is the only fire axe");
                Assert.That(protos.Index<EntityPrototype>("F14BaseAxe").Name, Is.EqualTo("wood axe"));
            });
        });
    }

    [Test]
    public async Task MagazineVisuals()
    {
        // MagazineVisuals is a client component, so check the client's prototypes.
        var protos = Pair.Client.ProtoMan;
        var factory = Pair.Client.EntMan.ComponentFactory;
        await Pair.Client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in new[] { "F14WeaponM60GPMG", "F14WeaponPistol357Automag", "F14WeaponPistol10mmMilitary",
                             "F14WeaponPistol10mmPolice", "F14WeaponDesertEagle44", "F14WeaponRifle556Infiltrator" })
                {
                    Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<MagazineVisualsComponent>(out _, factory), Is.True, id);
                    Assert.That(protos.Index<EntityPrototype>(id).TryGetComponent<AppearanceComponent>(out _, factory), Is.True, id);
                }
            });
        });
    }

    [Test]
    public async Task Infiltrator()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var slots = entMan.System<ItemSlotsSystem>();
            var gun = entMan.SpawnEntity("F14WeaponRifle556Infiltrator", map.GridCoords);
            Assert.Multiple(() =>
            {
                var gunComp = entMan.GetComponent<GunComponent>(gun);
                Assert.That(gunComp.SelectedMode, Is.EqualTo(SelectiveFire.SemiAuto));
                Assert.That(gunComp.AvailableModes, Is.EqualTo(SelectiveFire.SemiAuto));

                // Integral suppressor: present and can't come off.
                Assert.That(slots.TryGetSlot(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, out var muzzle), Is.True);
                Assert.That(entMan.GetComponent<MetaDataComponent>(muzzle!.Item!.Value).EntityPrototype!.ID, Is.EqualTo("F14FirearmSuppressor"));
                Assert.That(muzzle.Locked, Is.True);
                Assert.That(slots.TryEject(gun, muzzle, null, out _), Is.False);

                // Fixed 4x scope, no optic slot.
                Assert.That(entMan.GetComponent<WWScopeComponent>(gun).Magnification, Is.EqualTo(4f));
                Assert.That(slots.TryGetSlot(gun, FirearmAttachmentHostComponent.DefaultOpticSlot, out _), Is.False);

                Assert.That(entMan.HasComponent<CriticalHitBonusComponent>(gun), Is.True);
                Assert.That(slots.TryGetSlot(gun, "gun_magazine", out var mag), Is.True);
                Assert.That(entMan.GetComponent<MetaDataComponent>(mag!.Item!.Value).EntityPrototype!.ID, Is.EqualTo("F14Magazine556Rifle"));
            });
        });
    }
}
