using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.Warps;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Misfits: ladders link to the other ladder with the same id.
/// </summary>
[TestFixture, TestOf(typeof(WarperSystem))]
public sealed class WarperTest : GameTest
{
    [Test]
    public async Task LinkedLaddersMoveUser()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapSys = server.System<SharedMapSystem>();
        var xformSys = server.System<SharedTransformSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var map);
            var top = entMan.SpawnEntity("N14LadderTop", new MapCoordinates(0, 0, map));
            var bottom = entMan.SpawnEntity("N14LadderBottom", new MapCoordinates(20, 20, map));
            var unlinked = entMan.SpawnEntity("N14LadderTop", new MapCoordinates(-20, -20, map));
            entMan.GetComponent<WarperComponent>(top).ID = "test-ladder";
            entMan.GetComponent<WarperComponent>(bottom).ID = "test-ladder";

            var user = entMan.SpawnEntity("MobHuman", new MapCoordinates(1, 0, map));

            // Using the top ladder puts the user on the bottom one.
            entMan.EventBus.RaiseLocalEvent(top, new InteractHandEvent(user, top));
            Assert.That(xformSys.GetWorldPosition(user), Is.EqualTo(new Vector2(20, 20)));

            // And back up.
            entMan.EventBus.RaiseLocalEvent(bottom, new InteractHandEvent(user, bottom));
            Assert.That(xformSys.GetWorldPosition(user), Is.EqualTo(Vector2.Zero));

            // A ladder with no partner goes nowhere.
            xformSys.SetWorldPosition(user, new Vector2(-19, -20));
            entMan.EventBus.RaiseLocalEvent(unlinked, new InteractHandEvent(user, unlinked));
            Assert.That(xformSys.GetWorldPosition(user), Is.EqualTo(new Vector2(-19, -20)));

            entMan.DeleteEntity(mapSys.GetMap(map));
        });
    }
}
