#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// The nail gun fires without errors (2026-11-21 playtest: "Nail Gun fires an error"), spends a nail and hurts.
/// Runs with a client so client-side visual errors fail the test too.
/// </summary>
public sealed class NailgunTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId MobHuman = "MobHuman";
    private static readonly EntProtoId Nailgun = "F14WeaponNailgun";

    [Test]
    public async Task NailgunFires()
    {
        var gunSystem = SEntMan.System<SharedGunSystem>();
        var damageSystem = SEntMan.System<DamageableSystem>();

        await AddAtmosphere();
        var urist = await SpawnTarget(MobHuman);
        var gunNet = await PlaceInHands(Nailgun);
        var gun = ToServer(gunNet);
        await Pair.RunSeconds(2f);

        var start = gunSystem.GetAmmoCount(gun);
        Assert.That(start, Is.GreaterThan(0), "nail gun spawned empty");

        await UseInHand(); // wield
        await AttemptShoot(urist);
        await Pair.RunSeconds(1f);

        Assert.That(gunSystem.GetAmmoCount(gun), Is.LessThan(start), "no nail was fired");
        Assert.That(damageSystem.GetTotalDamage(ToServer(urist)), Is.GreaterThan(FixedPoint2.Zero), "the nail did no damage");
    }
}
