#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._WW.Weapons.SpinUp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 playtest: miniguns and the laser gatling spin up for a moment when you hold the trigger, then fire;
/// they slow you while held and much more while spinning, and spin down after you let go.
/// </summary>
public sealed class SpinUpGunTest : GameTest
{
    [TestCase("F14WeaponMinigun")]
    [TestCase("F14WeaponMinigunAvenger")]
    [TestCase("F14WeaponLaserGatling")]
    public async Task SpinsUpThenFires(string gunId)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var guns = entMan.System<SharedGunSystem>();

        EntityUid human = default, gun = default;
        await server.WaitPost(() =>
        {
            human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            gun = entMan.SpawnEntity(gunId, map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(human, gun));
        });
        await Pair.RunTicksSync(5);
        // Rack the bolt (ballistic miniguns start with it open), the way a player does: use it in hand.
        await server.WaitPost(() => entMan.System<SharedInteractionSystem>().UseInHandInteraction(human, gun));
        await Pair.RunTicksSync(5);

        float heldSpeed = 0, startAmmo = 0;
        await server.WaitPost(() =>
        {
            heldSpeed = entMan.GetComponent<MovementSpeedModifierComponent>(human).WalkSpeedModifier;
            startAmmo = guns.GetAmmoCount(gun);
        });
        Assert.That(heldSpeed, Is.LessThan(0.9f), "holding it doesn't slow you much");
        Assert.That(startAmmo, Is.GreaterThan(0));

        var target = new EntityCoordinates(map.Grid.Owner, 5, 0);
        async Task HoldTrigger(float seconds)
        {
            var ticks = (int) (seconds * 30);
            for (var i = 0; i < ticks; i++)
            {
                await server.WaitPost(() => guns.AttemptShoot(human, (gun, entMan.GetComponent<GunComponent>(gun)), target));
                await Pair.RunTicksSync(1);
            }
        }

        // Spinning up: no shots yet, and much slower.
        await HoldTrigger(0.3f);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<SpinUpGunComponent>(gun).Spinning);
            Assert.That(guns.GetAmmoCount(gun), Is.EqualTo(startAmmo), "fired before spinning up");
            Assert.That(entMan.GetComponent<MovementSpeedModifierComponent>(human).WalkSpeedModifier,
                Is.LessThan(heldSpeed * 0.6f), "spinning doesn't slow you much more");
        });

        // Spun up: it fires.
        await HoldTrigger(1f);
        await server.WaitAssertion(() =>
            Assert.That(guns.GetAmmoCount(gun), Is.LessThan(startAmmo), "didn't fire once spun up"));

        // Let go: it spins down and the extra slowdown goes.
        await Pair.RunTicksSync(30);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<SpinUpGunComponent>(gun).Spinning, Is.False, "didn't spin down");
            Assert.That(entMan.GetComponent<MovementSpeedModifierComponent>(human).WalkSpeedModifier,
                Is.EqualTo(heldSpeed).Within(0.01f));
        });
    }
}

