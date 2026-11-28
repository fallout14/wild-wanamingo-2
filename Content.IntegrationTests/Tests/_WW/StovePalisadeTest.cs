#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 sprite team contribution: palisades join each other (DirectionSpriteSmoothing, S=1 E=2 N=4 W=8),
/// and the stove uses their four-way sprite with a lit-burner overlay.
/// </summary>
public sealed class StovePalisadeTest : InteractionTest
{
    [Test]
    public async Task PalisadesJoin()
    {
        // Next to the target tile so the new tiles stay connected to the test grid (isolated tiles split off into new grids).
        var grid = MapData.Grid.Owner;
        var target = SEntMan.System<SharedTransformSystem>().WithEntityId(ToServer(TargetCoords), grid);
        var corner = target.Offset(new System.Numerics.Vector2(1, 0));
        var east = target.Offset(new System.Numerics.Vector2(2, 0));
        var north = target.Offset(new System.Numerics.Vector2(1, 1));
        foreach (var c in new[] { corner, east, north })
            await SetTile(Plating, FromServer(c), MapData.Grid);

        EntityUid sCorner = default, sEast = default;
        await Server.WaitPost(() =>
        {
            sCorner = SEntMan.SpawnAtPosition("F14WallPalisade", corner);
            sEast = SEntMan.SpawnAtPosition("F14WallPalisade", east);
            SEntMan.SpawnAtPosition("F14WallPalisade", north);
        });
        await RunTicks(15);
        // Smoothing is applied in the client's frame update, which a headless test client doesn't run by itself.
        await Client.WaitPost(() => CEntMan.System<Content.Client.IconSmoothing.IconSmoothSystem>().FrameUpdate(0f));

        await Client.WaitAssertion(() =>
        {
            var sprites = CEntMan.System<SpriteSystem>();
            string State(EntityUid s)
            {
                var c = CEntMan.GetEntity(SEntMan.GetNetEntity(s));
                Assert.That(sprites.LayerMapTryGet(c, "direction", out var idx, false), "no smoothing layer");
                return sprites.LayerGetRsiState(c, idx).Name ?? "";
            }

            Assert.That(State(sCorner), Is.EqualTo("palisade6"), "corner should join north and east");
            Assert.That(State(sEast), Is.EqualTo("palisade8"), "east piece should join west");
        });
    }

    [Test]
    public async Task StoveHasItsSprites()
    {
        var stove = await SpawnTarget("F14CookingStove");
        await RunTicks(5);
        await Client.WaitAssertion(() =>
        {
            var c = CEntMan.GetEntity(stove);
            var sprite = CEntMan.GetComponent<SpriteComponent>(c);
            Assert.That(sprite.BaseRSI?.Path.ToString(), Is.EqualTo("/Textures/WildWanamingo/Structures/Machines/stove.rsi"));
            Assert.That(sprite.BaseRSI!.TryGetState("stove_on", out var on), "no lit-burner state");
            Assert.That(on!.RsiDirections, Is.EqualTo(Robust.Shared.Graphics.RSI.RsiDirectionType.Dir4));
        });
    }
}
