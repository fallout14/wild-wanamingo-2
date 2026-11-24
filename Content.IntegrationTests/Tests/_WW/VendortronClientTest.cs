#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Forge.Trade;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-23: spawning a vendortron, or opening its store, crashed the game. Spawn each with a connected client
/// and open its store.
/// </summary>
public sealed class VendortronClientTest : InteractionTest
{
    [TestCase("WWMobVendortronGunRunners")]
    [TestCase("WWMobVendortronMedTek")]
    [TestCase("WWMobVendortronArcata")]
    [TestCase("WWMobVendortronTools")]
    [TestCase("WWMobVendortronRobCo")]
    public async Task SpawnAndOpenStore(string proto)
    {
        await SpawnTarget(proto);
        await RunTicks(30);
        await Activate();
        await RunTicks(30);
        Assert.That(SEntMan.EntityExists(STarget), "vendortron should still exist");
        Assert.That(SEntMan.System<SharedUserInterfaceSystem>().IsUiOpen(STarget!.Value, StoreUiKey.Key, SPlayer),
            "store should be open");
    }
}
