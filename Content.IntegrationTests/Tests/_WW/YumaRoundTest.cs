#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// A connected client plays a round on Yuma, goes back to the lobby and joins the next round without errors
/// (client-side errors fail the test).
/// </summary>
public sealed class YumaRoundTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true,
        Dirty = true,
    };

    [Test]
    public async Task RoundAndRestart()
    {
        var pair = Pair;
        pair.Server.CfgMan.SetCVar(CCVars.GameMap, "Yuma");
        var ticker = pair.Server.System<ServerGameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        for (var round = 0; round < 2; round++)
        {
            ticker.ToggleReadyAll(true);
            await pair.Server.WaitPost(() => ticker.StartRound());
            await pair.RunTicksSync(60);
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

            await pair.Server.WaitPost(() => ticker.RestartRound());
            await pair.RunTicksSync(60);
        }
    }
}
