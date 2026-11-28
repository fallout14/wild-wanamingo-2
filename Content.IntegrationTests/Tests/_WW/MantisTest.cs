#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>2026-11-22: the sprite team's giant mantis spawns alive, survives a radroach-killing hit and dies at 60.</summary>
public sealed class MantisTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";

    [Test]
    public async Task MantisLivesAndDies()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid mantis = default;
        await server.WaitPost(() => mantis = entMan.SpawnEntity("F14MobGiantMantis", map.GridCoords));
        await Pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var mobState = entMan.System<MobStateSystem>();
            Assert.That(mobState.IsAlive(mantis));
            var damage = entMan.System<DamageableSystem>();
            damage.ChangeDamage(mantis, new DamageSpecifier(server.ProtoMan.Index(Slash), 20), ignoreResistances: true);
            Assert.That(mobState.IsAlive(mantis), "died to a hit that only kills a radroach");
            damage.ChangeDamage(mantis, new DamageSpecifier(server.ProtoMan.Index(Slash), 45), ignoreResistances: true);
            Assert.That(mobState.IsDead(mantis), "survived 65 damage");
        });
    }
}
