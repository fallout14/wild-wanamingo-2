#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22 playtest: "PA welding needs to be added back in". A welder repairs a damaged power armor suit, both
/// on the floor and while someone else is wearing it.
/// </summary>
public sealed class PowerArmorWeldTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId PowerArmor = "F14ClothingOuterPowerArmorT45";
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [Test]
    public async Task WeldingRepairsSuitOnTheFloor()
    {
        var damage = SEntMan.System<DamageableSystem>();
        var suit = await SpawnTarget(PowerArmor);
        var before = await DamageSuit(ToServer(suit));

        await InteractUsing(Weld);
        await RunSeconds(1f);

        Assert.That(damage.GetTotalDamage(ToServer(suit)), Is.LessThan(before), "welding didn't repair the suit");
    }

    [Test]
    public async Task WeldingRepairsSuitSomeoneIsWearing()
    {
        var damage = SEntMan.System<DamageableSystem>();
        var wearer = await SpawnTarget(MobHuman);
        EntityUid suit = default;
        await Server.WaitPost(() =>
        {
            suit = SEntMan.SpawnEntity(PowerArmor, SEntMan.GetComponent<TransformComponent>(ToServer(wearer)).Coordinates);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(ToServer(wearer), suit, "outerClothing", silent: true, force: true));
        });
        var before = await DamageSuit(suit);

        await InteractUsing(Weld);
        await RunSeconds(1f);

        Assert.That(damage.GetTotalDamage(suit), Is.LessThan(before), "welding the wearer didn't repair their suit");
    }

    private async Task<FixedPoint2> DamageSuit(EntityUid suit)
    {
        var damage = SEntMan.System<DamageableSystem>();
        FixedPoint2 total = default;
        await Server.WaitPost(() =>
        {
            damage.ChangeDamage(suit, new DamageSpecifier(ProtoMan.Index(Blunt), 100), true);
            total = damage.GetTotalDamage(suit);
        });
        Assert.That(total, Is.GreaterThan(FixedPoint2.Zero), "the suit took no damage");
        return total;
    }
}
