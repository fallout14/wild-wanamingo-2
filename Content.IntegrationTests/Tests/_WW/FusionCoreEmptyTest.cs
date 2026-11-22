#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Client.PowerCell;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 playtest: "Fusion core does not change to its empty icon state". A drained fusion core, and one spawned
/// empty, show the spent sprite.
/// </summary>
public sealed class FusionCoreEmptyTest : InteractionTest
{
    [TestCase("F14FusionCore", 0f)]
    [TestCase("F14FusionCore", 5f)] // less than a gatling shot left: the gun can't take it lower
    [TestCase("F14FusionCoreEmpty", -1f)]
    public async Task EmptyCoreShowsSpentSprite(string proto, float drainTo)
    {
        var core = await Spawn(proto);
        var sCore = ToServer(core);
        if (drainTo >= 0f)
        {
            await Server.WaitPost(() =>
            {
                var battery = SEntMan.System<SharedBatterySystem>();
                battery.SetCharge(sCore, drainTo);
            });
        }
        await RunTicks(10);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<SharedAppearanceSystem>().TryGetData(sCore, BatteryVisuals.State, out BatteryState state),
                "no battery state on the core");
            Assert.That(state, Is.EqualTo(BatteryState.Empty));
        });
        await Client.WaitAssertion(() =>
        {
            var cCore = ToClient(core);
            var sprite = CEntMan.GetComponent<SpriteComponent>(cCore);
            var sprites = CEntMan.System<SpriteSystem>();
            Assert.That(sprites.LayerMapTryGet((cCore, sprite), PowerCellVisualLayers.Base, out var layer, false));
            Assert.That(sprite[layer].RsiState.Name, Is.EqualTo("icon-dead"));
        });
    }
}
