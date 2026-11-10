using Content.Server._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._WW.Weapons;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// A weapon's CriticalHitBonus adds to the Luck crit chance (at any Luck) and can set its own crit multiplier.
/// </summary>
[TestFixture]
public sealed class CriticalHitBonusTest
{
    [TestCase(5)]  // neutral Luck: no crits without a bonus
    [TestCase(8)]
    [TestCase(1)]  // low Luck: the bonus still applies
    public async Task WeaponBonusAddsToLuckChance(int luck)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var combat = entMan.System<SpecialCombatSystem>();
            var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var special = entMan.EnsureComponent<SpecialComponent>(user);
            special.BaseLuck = luck;

            var plain = entMan.SpawnEntity("F14WeaponSniper556VarmintRifle", map.GridCoords);
            var tuned = entMan.SpawnEntity("F14WeaponSniper556VarmintRifle", map.GridCoords);
            var bonus = entMan.AddComponent<CriticalHitBonusComponent>(tuned);
            bonus.Chance = 0.1f;
            bonus.DamageMultiplier = 2.5f;

            var baseChance = combat.GetLuckCriticalChance(user, special, plain);
            if (luck == 5)
                Assert.That(baseChance, Is.Zero);

            Assert.That(combat.GetLuckCriticalChance(user, special, tuned),
                Is.EqualTo(baseChance + 0.1f).Within(0.0001f));
            Assert.That(combat.GetCriticalDamageMultiplier(tuned), Is.EqualTo(2.5f));
            Assert.That(combat.GetCriticalDamageMultiplier(plain), Is.EqualTo(1.5f).Within(0.0001f));
        });

        await pair.CleanReturnAsync();
    }
}
