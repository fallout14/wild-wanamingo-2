#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-22: suppressed guns use Desert Rose's suppressed shots. The suppressor's default is the regular one;
/// the Infiltrator, commando carbine and Ratslayer use the heavier rifle one, as in Desert Rose.
/// </summary>
public sealed class SuppressedSoundTest : GameTest
{
    private const string Regular = "/Audio/WildWanamingo/DesertRose/Weapons/silenced.ogg";
    private const string Large = "/Audio/WildWanamingo/DesertRose/Weapons/silenced_large.ogg";

    [TestCase("F14WeaponSMGMP5SD", Regular)]
    [TestCase("F14WeaponRifle556Infiltrator", Large)]
    [TestCase("F14WeaponRifle45Commando", Large)]
    [TestCase("F14WeaponSniperRatslayer", Large)]
    public async Task SuppressedReport(string gunId, string expected)
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        EntityUid gun = default;
        await server.WaitPost(() => gun = entMan.SpawnEntity(gunId, map.GridCoords));
        // Attachment modifiers refresh a frame after the suppressor goes in.
        await Pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var sound = entMan.GetComponent<GunComponent>(gun).SoundGunshotModified;
            Assert.That(sound, Is.InstanceOf<SoundPathSpecifier>(), $"{gunId}: {sound}");
            Assert.That(((SoundPathSpecifier) sound!).Path.ToString(), Is.EqualTo(expected), gunId);
        });
    }
}
