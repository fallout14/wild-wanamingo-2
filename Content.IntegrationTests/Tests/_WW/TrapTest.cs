using Content.Server.Gravity;
using Content.Shared._WW.Traps;
using Content.Shared.Gravity;
using Content.Shared.Damage.Systems;
using Content.Shared.Ensnaring;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.UnitTesting;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Traps: arm and disarm with do-afters, armed traps are anchored and can't be picked up, stepping on one springs it
/// on the victim, and Light Step never sets one off.
/// </summary>
[TestFixture]
public sealed class TrapTest
{
    private static void EnableGravity(IEntityManager entMan, TestMapData map)
    {
        // Weightless mobs never set off step triggers.
        entMan.EnsureComponent<GravityComponent>(map.Grid);
        entMan.System<GravitySystem>().EnableGravity(map.Grid);
    }

    private static async Task WaitSeconds(RobustIntegrationTest.ServerIntegrationInstance server, float seconds)
    {
        await server.WaitRunTicks((int) (seconds * server.ResolveDependency<IGameTiming>().TickRate) + 5);
    }

    [Test]
    public async Task ArmDisarmAndPickup()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        EntityUid user = default, trap = default;

        await server.WaitAssertion(() =>
        {
            user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            trap = entMan.SpawnEntity("WWBearTrap", map.GridCoords);
            Assert.That(entMan.System<WWTrapSystem>().StartArming((trap, entMan.GetComponent<WWTrapComponent>(trap)), user, true));
        });
        await WaitSeconds(server, 3);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Armed, Is.True);
            Assert.That(entMan.GetComponent<TransformComponent>(trap).Anchored, Is.True);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, trap), Is.False, "armed traps can't be picked up");
            Assert.That(entMan.System<WWTrapSystem>().StartArming((trap, entMan.GetComponent<WWTrapComponent>(trap)), user, false));
        });
        await WaitSeconds(server, 4);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Armed, Is.False);
            Assert.That(entMan.GetComponent<TransformComponent>(trap).Anchored, Is.False);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, trap), Is.True);
            // Can't arm it in your hands.
            Assert.That(entMan.System<WWTrapSystem>().StartArming((trap, entMan.GetComponent<WWTrapComponent>(trap)), user, true), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task BearTrapClampsVictim(bool lightStep)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        EntityUid victim = default, trap = default;

        await server.WaitAssertion(() =>
        {
            EnableGravity(entMan, map);
            trap = entMan.SpawnEntity("WWBearTrap", map.GridCoords);
            entMan.System<WWTrapSystem>().SetArmed((trap, entMan.GetComponent<WWTrapComponent>(trap)), true);
            victim = entMan.SpawnEntity("MobHuman", entMan.GetComponent<TransformComponent>(trap).Coordinates);
            if (lightStep)
                entMan.AddComponent<LightStepComponent>(victim);
        });
        await WaitSeconds(server, 1);

        await server.WaitAssertion(() =>
        {
            var ensnared = entMan.System<SharedEnsnareableSystem>().IsEnsnared(victim);
            var damage = entMan.System<DamageableSystem>().GetTotalDamage(victim);
            if (lightStep)
            {
                Assert.That(ensnared, Is.False, "Light Step should not set off the trap");
                Assert.That(damage.Float(), Is.LessThan(10f), "no trap damage (the airless test map hurts a little)");
                Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Armed, Is.True);
            }
            else
            {
                Assert.That(ensnared, Is.True, "the bear trap should clamp the victim");
                Assert.That(damage.Float(), Is.GreaterThanOrEqualTo(20f), "15 piercing + 5 blunt from the jaws");
                Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Armed, Is.False, "it disarms when sprung");
                Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Spent, Is.False, "bear traps are reusable");
            }
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TripwireKnocksDown()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        EntityUid victim = default, trap = default;

        await server.WaitAssertion(() =>
        {
            EnableGravity(entMan, map);
            trap = entMan.SpawnEntity("WWTripwire", map.GridCoords);
            entMan.System<WWTrapSystem>().SetArmed((trap, entMan.GetComponent<WWTrapComponent>(trap)), true);
            victim = entMan.SpawnEntity("MobHuman", entMan.GetComponent<TransformComponent>(trap).Coordinates);
        });
        await server.WaitRunTicks(10);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<KnockedDownComponent>(victim), Is.True);
            Assert.That(entMan.GetComponent<WWTrapComponent>(trap).Armed, Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FragMineIsSingleUse()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();
        EntityUid trap = default;

        await server.WaitAssertion(() =>
        {
            EnableGravity(entMan, map);
            trap = entMan.SpawnEntity("WWFragMine", map.GridCoords);
            var comp = entMan.GetComponent<WWTrapComponent>(trap);
            Assert.That(comp.Rearmable, Is.False);
            entMan.System<WWTrapSystem>().SetArmed((trap, comp), true);
            entMan.SpawnEntity("MobHuman", entMan.GetComponent<TransformComponent>(trap).Coordinates);
        });
        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(trap), Is.True, "the mine should blow up");
        });

        await pair.CleanReturnAsync();
    }
}
