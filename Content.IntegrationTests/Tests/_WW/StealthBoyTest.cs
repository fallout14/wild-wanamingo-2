#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._WW.Stealth;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Stealth.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Stealth Boys cloak the user for a while; a normal one is spent on use (and becomes the burnt-out one), the
/// Assaultron's built-in one stays and recharges.
/// </summary>
[TestOf(typeof(StealthBoyComponent))]
public sealed class StealthBoyTest : GameTest
{
    [Test]
    public async Task OneUseThenSpent()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();
        var map = await Pair.CreateTestMap();
        EntityUid user = default;
        EntityUid boy = default;
        var duration = TimeSpan.Zero;

        await server.WaitAssertion(() =>
        {
            var hands = entMan.System<SharedHandsSystem>();
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            boy = entMan.SpawnEntity("F14StealthBoy", map.GridCoords);
            duration = entMan.GetComponent<StealthBoyComponent>(boy).Duration;
            Assert.That(hands.TryPickupAnyHand(user, boy));

            var ev = new UseInHandEvent(user);
            entMan.EventBus.RaiseLocalEvent(boy, ev);
            Assert.That(ev.Handled);
            Assert.That(entMan.HasComponent<StealthComponent>(user), "using it should cloak you");
        });
        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(boy), "a Stealth Boy is spent after one use");
            var held = entMan.System<SharedHandsSystem>().GetActiveItem(user);
            Assert.That(held, Is.Not.Null);
            Assert.That(entMan.GetComponent<MetaDataComponent>(held!.Value).EntityPrototype?.ID, Is.EqualTo("F14StealthBoyUsed"));
        });

        await server.WaitRunTicks((int) Math.Ceiling(duration.TotalSeconds * timing.TickRate) + 2);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<StealthComponent>(user), Is.False, "the cloak should wear off");
        });
    }

    [Test]
    public async Task BuiltInTogglesAndRecharges()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var timing = server.ResolveDependency<IGameTiming>();
        var map = await Pair.CreateTestMap();
        var battery = entMan.System<SharedBatterySystem>();
        EntityUid user = default;
        EntityUid boy = default;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            boy = entMan.SpawnEntity("F14BorgStealthBoy", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, boy));
            Assert.That(battery.IsFull((boy, null)));

            entMan.EventBus.RaiseLocalEvent(boy, new UseInHandEvent(user));
            Assert.That(entMan.HasComponent<StealthComponent>(user), "switching it on should cloak you");
            Assert.That(entMan.Deleted(boy), Is.False, "the built-in one isn't used up");
        });

        await server.WaitRunTicks(2 * timing.TickRate);
        float drained = 0;
        await server.WaitAssertion(() =>
        {
            drained = battery.GetCharge((boy, null));
            Assert.That(drained, Is.LessThan(100f), "it drains while on");

            entMan.EventBus.RaiseLocalEvent(boy, new UseInHandEvent(user));
        });
        await server.WaitRunTicks(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<StealthComponent>(user), Is.False, "switching it off should uncloak you");
        });

        await server.WaitRunTicks(2 * timing.TickRate);
        await server.WaitAssertion(() =>
        {
            Assert.That(battery.GetCharge((boy, null)), Is.GreaterThan(drained), "it recharges while off");
        });
    }
}
