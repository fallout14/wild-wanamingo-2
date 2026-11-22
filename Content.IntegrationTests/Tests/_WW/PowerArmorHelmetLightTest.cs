#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 playtest: "PA Helmet light doesn't seem to work". Wearing a power armor helmet gives a light toggle that
/// turns its light on and off.
/// </summary>
public sealed class PowerArmorHelmetLightTest : GameTest
{
    [TestCase("F14ClothingHeadHelmetPowerArmorT45")]
    [TestCase("F14ClothingHeadHelmetPowerArmorT51")]
    [TestCase("F14ClothingHeadHelmetPowerArmorX01")]
    public async Task HelmetLightToggles(string helmetId)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        EntityUid human = default, helmet = default;
        await server.WaitPost(() =>
        {
            human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            helmet = entMan.SpawnEntity(helmetId, map.GridCoords);
            Assert.That(entMan.System<InventorySystem>().TryEquip(human, helmet, "head", silent: true, force: true));
        });
        await Pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var light = entMan.GetComponent<HandheldLightComponent>(helmet);
            Assert.That(light.ToggleActionEntity, Is.Not.Null, "no light toggle action");
            var actions = entMan.System<SharedActionsSystem>();
            Assert.That(actions.GetActions(human).Any(a => a.Owner == light.ToggleActionEntity), "the wearer didn't get the toggle");
            actions.PerformAction(human, (light.ToggleActionEntity!.Value, entMan.GetComponent<ActionComponent>(light.ToggleActionEntity!.Value)));
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<HandheldLightComponent>(helmet).Activated, "the toggle didn't switch the light on");
            Assert.That(entMan.System<SharedPointLightSystem>().TryGetLight(helmet, out var pointLight), "no point light");
            Assert.That(pointLight!.Enabled, "the point light stayed off");
        });
    }
}

/// <summary>The same toggle, checked on the client (what the player sees).</summary>
public sealed class PowerArmorHelmetLightClientTest : Content.IntegrationTests.Tests.Interaction.InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task HelmetLightShowsOnClient()
    {
        EntityUid helmet = default;
        await Server.WaitPost(() =>
        {
            var player = ToServer(Player);
            helmet = SEntMan.SpawnEntity("F14ClothingHeadHelmetPowerArmorT45", SEntMan.GetComponent<TransformComponent>(player).Coordinates);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(player, helmet, "head", silent: true, force: true));
        });
        await RunTicks(10);
        await Server.WaitPost(() =>
        {
            var light = SEntMan.GetComponent<HandheldLightComponent>(helmet);
            SEntMan.System<SharedActionsSystem>().PerformAction(ToServer(Player),
                (light.ToggleActionEntity!.Value, SEntMan.GetComponent<ActionComponent>(light.ToggleActionEntity!.Value)));
        });
        await RunTicks(15);
        await Client.WaitAssertion(() =>
        {
            var cHelmet = CEntMan.GetEntity(SEntMan.GetNetEntity(helmet));
            Assert.That(CEntMan.GetComponent<HandheldLightComponent>(cHelmet).Activated, "client doesn't know the light is on");
            Assert.That(CEntMan.System<SharedPointLightSystem>().TryGetLight(cHelmet, out var pointLight), "no client point light");
            Assert.That(pointLight!.Enabled, "the client's point light stayed off");
        });
    }
}
