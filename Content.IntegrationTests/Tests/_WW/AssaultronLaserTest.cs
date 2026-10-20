#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Misfits.Robot;
using Content.Shared._Misfits.Weapons.Legacy;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The assaultron head laser: the first use charges, the next use once charged fires, then it cools down.
/// </summary>
[TestOf(typeof(ActionGunComponent))]
public sealed class AssaultronLaserTest : GameTest
{
    [Test]
    public async Task ChargesThenFires()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();
        var map = await pair.CreateTestMap();
        EntityUid robot = default;

        await server.WaitPost(() => robot = entMan.SpawnEntity("F14PlayerSyntheticAssaultron", map.GridCoords));
        await server.WaitRunTicks(2);

        ActionGunComponent gunAction = default!;
        AssaultronBeamChargeComponent charge = default!;
        await server.WaitAssertion(() =>
        {
            gunAction = entMan.GetComponent<ActionGunComponent>(robot);
            Assert.That(gunAction.ActionEntity, Is.Not.Null, "the assaultron should get its head laser action");
            Assert.That(gunAction.Gun, Is.Not.Null, "the integrated gun should exist");
            charge = entMan.GetComponent<AssaultronBeamChargeComponent>(gunAction.Gun!.Value);
        });

        await Shoot();
        await server.WaitAssertion(() =>
        {
            Assert.That(charge.IsCharging, "the first use should start the charge-up");
            Assert.That(charge.CooldownEndTime, Is.EqualTo(TimeSpan.Zero), "nothing fired yet");
        });

        // Wait out the charge, then fire.
        await server.WaitRunTicks((int) Math.Ceiling(charge.ChargeDuration * timing.TickRate) + 2);
        await Shoot();
        await server.WaitAssertion(() =>
        {
            Assert.That(charge.IsCharging, Is.False);
            Assert.That(charge.CooldownEndTime, Is.GreaterThan(timing.CurTime), "firing should start the cooldown");
        });

        async Task Shoot()
        {
            await server.WaitPost(() =>
            {
                var actionUid = gunAction.ActionEntity!.Value;
                var ev = new ActionGunShootEvent
                {
                    Performer = robot,
                    Action = (actionUid, entMan.GetComponent<ActionComponent>(actionUid)),
                    Target = new EntityCoordinates(map.Grid, 5, 0),
                };
                entMan.EventBus.RaiseLocalEvent(robot, ev);
            });
            await server.WaitRunTicks(1);
        }
    }
}
