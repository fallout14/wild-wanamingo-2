#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Weapons.Attachments.Components;
using Content.Shared._Misfits.Weapons.Sears;
using Content.Shared._WW.Weapons.Attachments;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The recoil compensator muzzle attachment, and which guns take burst/auto sears.
/// </summary>
public sealed class WeaponModsTest : GameTest
{
    [Test]
    [TestOf(typeof(RecoilCompensatorAttachmentComponent))]
    public async Task CompensatorCutsRecoil()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid gun = default;
        EntityUid comp = default;
        double increase = 0, min = 0, max = 0;
        float camera = 0;

        await server.WaitAssertion(() =>
        {
            gun = entMan.SpawnEntity("F14WeaponRifle556R91", map.GridCoords);
            var firearm = entMan.GetComponent<GunComponent>(gun);
            entMan.System<SharedGunSystem>().RefreshModifiers((gun, firearm));
            increase = firearm.AngleIncreaseModified.Degrees;
            min = firearm.MinAngleModified.Degrees;
            max = firearm.MaxAngleModified.Degrees;
            camera = firearm.CameraRecoilScalarModified;

            comp = entMan.SpawnEntity("F14FirearmRecoilCompensator", map.GridCoords);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, comp, null));
        });
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var firearm = entMan.GetComponent<GunComponent>(gun);
            var c = entMan.GetComponent<RecoilCompensatorAttachmentComponent>(comp);
            Assert.Multiple(() =>
            {
                Assert.That(firearm.AngleIncreaseModified.Degrees, Is.EqualTo(increase * c.AngleIncreaseMultiplier).Within(0.01));
                Assert.That(firearm.CameraRecoilScalarModified, Is.EqualTo(camera * c.CameraRecoilMultiplier).Within(0.001));
                Assert.That(firearm.MinAngleModified.Degrees, Is.EqualTo(min).Within(0.01), "spread floor unchanged");
                Assert.That(firearm.MaxAngleModified.Degrees, Is.EqualTo(max).Within(0.01), "spread ceiling unchanged");
            });

            // The muzzle slot has disableEject (attachments come off by verb), so take it out of the container directly.
            Assert.That(entMan.System<ItemSlotsSystem>().TryGetSlot(gun, FirearmAttachmentHostComponent.DefaultMuzzleSlot, out var slot));
            Assert.That(entMan.System<SharedContainerSystem>().Remove(comp, slot!.ContainerSlot!));
        });
        await server.WaitRunTicks(3);

        await server.WaitAssertion(() =>
        {
            var firearm = entMan.GetComponent<GunComponent>(gun);
            Assert.That(firearm.AngleIncreaseModified.Degrees, Is.EqualTo(increase).Within(0.01), "taking it off restores the recoil");
            Assert.That(firearm.CameraRecoilScalarModified, Is.EqualTo(camera).Within(0.001));
        });
    }

    // gun, takes a burst sear, takes an auto sear
    private static readonly (string Id, bool Burst, bool Auto)[] SearGuns =
    {
        ("F14WeaponRifle556Service", true, true),
        ("F14WeaponRifle556ServiceScoped", true, true),
        ("F14WeaponRifle556Marksman", true, true),
        ("F14WeaponRifle556Rangemaster", true, true),
        ("F14WeaponRifle556Mini14", true, true),
        ("F14WeaponRifle308M14", false, true),
        ("F14WeaponRifle303MarksmanCanadian", false, true),
        ("F14WeaponRifle10mmM1Carbine", false, true),
        ("F14WeaponRifle10mmSkirmisher", false, true),
        ("F14WeaponSniper556Tribal", false, true),
        ("F14Glock", false, true),
        ("F14WeaponPistol10mmPipe", false, true),
        ("F14WeaponPistol9mmBerettaM92", true, false),
    };

    [Test]
    [TestOf(typeof(FirearmSearCompatibleComponent))]
    public async Task SearCompatibleGuns()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (id, burst, auto) in SearGuns)
                {
                    var gun = entMan.SpawnEntity(id, map.GridCoords);
                    Assert.That(entMan.TryGetComponent<FirearmSearCompatibleComponent>(gun, out var sear), id);
                    Assert.That(sear!.AllowBurst, Is.EqualTo(burst), id + " burst");
                    Assert.That(sear.AllowFullAuto, Is.EqualTo(auto), id + " auto");
                }
            });
        });
    }
}
