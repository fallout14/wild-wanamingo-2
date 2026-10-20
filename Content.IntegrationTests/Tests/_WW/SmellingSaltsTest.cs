#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._Misfits.Medical;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Smelling salts revive like a single-use defibrillator.
/// </summary>
[TestOf(typeof(SmellingSaltsSystem))]
public sealed class SmellingSaltsTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    private static readonly EntProtoId Salts = "F14SmellingSalts";
    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";

    [Test]
    public async Task RevivesSuffocatedBodyAndIsUsedUp()
    {
        var damageable = SEntMan.System<DamageableSystem>();
        var thresholds = SEntMan.System<MobThresholdSystem>();

        await AddAtmosphere();
        await SpawnTarget("MobHuman");
        var state = Comp<MobStateComponent>();

        // Dead from suffocation, exactly at the death threshold (the salts heal 40 asphyxiation).
        var dead = thresholds.GetThresholdForState(STarget!.Value, MobState.Dead);
        await Server.WaitPost(() => damageable.SetDamage((STarget.Value, Comp<DamageableComponent>()),
            new DamageSpecifier(ProtoMan.Index(Asphyxiation), dead)));
        await RunTicks(3);
        Assert.That(state.CurrentState, Is.EqualTo(MobState.Dead));

        var salts = await PlaceInHands(Salts);
        await Interact();

        Assert.Multiple(() =>
        {
            Assert.That(state.CurrentState, Is.EqualTo(MobState.Critical), "Smelling salts didn't revive the body");
            Assert.That(SEntMan.Deleted(ToServer(salts)), "Smelling salts weren't used up");
        });
    }
}
