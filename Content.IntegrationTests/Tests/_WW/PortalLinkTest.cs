#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._WW.Teleportation;
using Content.Shared.Teleportation.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Ladders and bunker hatches are Wizden portals, paired by link id (and side, for the bunker tunnels).
/// </summary>
[TestOf(typeof(F14PortalLinkSystem))]
public sealed class PortalLinkTest : GameTest
{
    private static readonly EntProtoId LadderTop = "F14LadderTop";
    private static readonly EntProtoId LadderBottom = "F14LadderBottom";
    private static readonly EntProtoId Hatch = "F14BunkerHatchTunnel";
    private static readonly EntProtoId TunnelDoor = "F14BunkerTunnelDoor";

    [Test]
    public async Task LaddersPairByLinkId()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var links = entMan.System<F14PortalLinkSystem>();
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var top = entMan.SpawnEntity(LadderTop, map.MapCoords);
            var bottom = entMan.SpawnEntity(LadderBottom, new MapCoordinates(20, 20, map.MapId));
            var stray = entMan.SpawnEntity(LadderTop, new MapCoordinates(-20, -20, map.MapId));
            Assert.That(entMan.HasComponent<PortalComponent>(top) && entMan.HasComponent<PortalComponent>(bottom));

            // Unnamed ladders don't pair with anything.
            Assert.That(!entMan.TryGetComponent<LinkedEntityComponent>(top, out var none) || none.LinkedEntities.Count == 0);

            links.SetLinkId((top, entMan.GetComponent<F14PortalLinkComponent>(top)), "test-ladder");
            links.SetLinkId((bottom, entMan.GetComponent<F14PortalLinkComponent>(bottom)), "test-ladder");

            Assert.That(entMan.GetComponent<LinkedEntityComponent>(top).LinkedEntities, Is.EquivalentTo(new[] { bottom }));
            Assert.That(entMan.GetComponent<LinkedEntityComponent>(bottom).LinkedEntities, Is.EquivalentTo(new[] { top }));
            Assert.That(!entMan.TryGetComponent<LinkedEntityComponent>(stray, out var strayLinks) || strayLinks.LinkedEntities.Count == 0);
        });
    }

    [Test]
    public async Task BunkerHatchesLeadToTunnelDoors()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var hatchA = entMan.SpawnEntity(Hatch, map.MapCoords);
            var hatchB = entMan.SpawnEntity(Hatch, new MapCoordinates(5, 0, map.MapId));
            var door = entMan.SpawnEntity(TunnelDoor, new MapCoordinates(30, 30, map.MapId));

            // Each hatch leads to the tunnel door, never to the other hatch.
            Assert.That(entMan.GetComponent<LinkedEntityComponent>(hatchA).LinkedEntities, Is.EquivalentTo(new[] { door }));
            Assert.That(entMan.GetComponent<LinkedEntityComponent>(hatchB).LinkedEntities, Is.EquivalentTo(new[] { door }));
            Assert.That(entMan.GetComponent<LinkedEntityComponent>(door).LinkedEntities, Is.EquivalentTo(new[] { hatchA, hatchB }));
        });
    }
}
