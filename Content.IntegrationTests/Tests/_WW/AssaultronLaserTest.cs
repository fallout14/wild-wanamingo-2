#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._WW.Robots;
using Content.Shared.Interaction.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The Assaultron laser eye: won't fire uncharged; use fills its battery (the charge indicator); the first charged
/// shot starts a beam that roots the shooter and keeps firing until the battery is drained, then it cools down.
/// </summary>
[TestOf(typeof(LaserEyeComponent))]
public sealed class AssaultronLaserTest : GameTest
{
    [Test]
    public async Task ChargesThenBeams()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();
        var map = await pair.CreateTestMap();
        EntityUid robot = default;
        EntityUid eye = default;
        LaserEyeComponent comp = default!;
        var target = new EntityCoordinates(map.Grid, 5, 0);
        var battery = entMan.System<SharedBatterySystem>();

        await server.WaitPost(() =>
        {
            robot = entMan.SpawnEntity("MobHuman", map.GridCoords);
            eye = entMan.SpawnEntity("F14BorgWeaponLaserEye", map.GridCoords);
            comp = entMan.GetComponent<LaserEyeComponent>(eye);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(battery.GetCharge((eye, null)), Is.Zero, "it starts uncharged");
            Assert.That(Shoot(), Is.False, "an uncharged laser eye can't fire");
            entMan.EventBus.RaiseLocalEvent(eye, new UseInHandEvent(robot));
            Assert.That(comp.State, Is.EqualTo(LaserEyeState.Charging));
        });

        // Halfway through, the cell shows about half charge.
        await server.WaitRunTicks((int) Math.Ceiling(comp.ChargeDuration.TotalSeconds / 2 * timing.TickRate));
        await server.WaitAssertion(() =>
        {
            Assert.That(battery.GetChargeLevel((eye, null)), Is.InRange(0.3f, 0.7f), "the battery fills while charging");
        });

        await server.WaitRunTicks((int) Math.Ceiling(comp.ChargeDuration.TotalSeconds / 2 * timing.TickRate) + 4);
        await server.WaitAssertion(() =>
        {
            Assert.That(comp.State, Is.EqualTo(LaserEyeState.Charged));
            Assert.That(battery.IsFull((eye, null)));
            Assert.That(Shoot(), "a charged laser eye should fire");
            Assert.That(comp.State, Is.EqualTo(LaserEyeState.Firing));
            Assert.That(entMan.HasComponent<LaserEyeFiringComponent>(robot), "the shooter is rooted while the beam fires");
        });

        // The beam keeps pulsing on its own until the battery is drained.
        await server.WaitRunTicks((int) Math.Ceiling(comp.BeamDuration.TotalSeconds * timing.TickRate) + 4);
        await server.WaitAssertion(() =>
        {
            Assert.That(comp.State, Is.EqualTo(LaserEyeState.Cooldown));
            Assert.That(entMan.HasComponent<LaserEyeFiringComponent>(robot), Is.False, "free to move once it ends");
            Assert.That(battery.GetCharge((eye, null)), Is.Zero, "the beam uses up the charge");
        });

        bool Shoot()
        {
            var gun = entMan.GetComponent<GunComponent>(eye);
            return entMan.System<SharedGunSystem>().AttemptShoot(robot, (eye, gun), target);
        }
    }
}
