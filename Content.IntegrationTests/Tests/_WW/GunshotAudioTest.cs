#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Misfits.Weapons.Ranged;
using Content.Shared._WW.Weapons.Ranged;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-23 playtest: a minigun's shots piled up until shots went silent and clients ran out of audio sources
/// (crash on Debug, freeze on Release). Only <see cref="GunshotAudioSystem.MaxOverlap"/> shots of one gun play at once.
/// </summary>
public sealed class GunshotAudioTest : GameTest
{
    private const string ShotFile = "/Audio/WildWanamingo/DesertRose/Weapons/assaultrifle_fire.ogg";

    [Test]
    public async Task MinigunShotsDontPileUp()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();
        var guns = entMan.System<SharedGunSystem>();

        EntityUid human = default, gun = default;
        await server.WaitPost(() =>
        {
            human = entMan.SpawnEntity("MobHuman", map.GridCoords);
            gun = entMan.SpawnEntity("F14WeaponMinigun", map.GridCoords);
            Assert.That(entMan.System<SharedHandsSystem>().TryPickupAnyHand(human, gun));
        });
        await Pair.RunTicksSync(5);
        await server.WaitPost(() => entMan.System<SharedInteractionSystem>().UseInHandInteraction(human, gun));
        await Pair.RunTicksSync(5);

        var target = new EntityCoordinates(map.Grid.Owner, 5, 0);
        var most = 0;
        var shotFile = "";
        float startAmmo = 0;
        await server.WaitPost(() => startAmmo = guns.GetAmmoCount(gun));
        for (var i = 0; i < 90; i++) // 3 seconds of fire, 15 shots a second
        {
            await server.WaitPost(() =>
            {
                guns.AttemptShoot(human, (gun, entMan.GetComponent<GunComponent>(gun)), target);
                shotFile = ((Robust.Shared.Audio.SoundCollectionSpecifier) entMan.GetComponent<GunComponent>(gun).SoundGunshotModified!).Collection!;
                var shots = 0;
                var q = entMan.EntityQueryEnumerator<AudioComponent, TransformComponent>();
                while (q.MoveNext(out var audio, out var a, out var xform))
                {
                    if (xform.ParentUid == gun && !entMan.IsQueuedForDeletion(audio) && a.FileName == ShotFile)
                        shots++;
                }
                most = Math.Max(most, shots);
            });
            await Pair.RunTicksSync(1);
        }

        float endAmmo = 0;
        await server.WaitPost(() => endAmmo = guns.GetAmmoCount(gun));
        Assert.That(startAmmo - endAmmo, Is.GreaterThan(20), "the minigun should have kept firing");
        Assert.That(most, Is.InRange(2, GunshotAudioSystem.MaxOverlap), "shots playing at once");
        Assert.That(shotFile, Is.EqualTo("WWDRGunshotAssaultRifle"), "the minigun's shot sound changed; update the file this test counts");
    }
}

/// <summary>
/// 2026-11-23 playtest: cycling a magazine bolt-action fast and firing played no shot for the shooter. The client
/// couldn't predict a round out of the magazine's unspawned count, saw an empty chamber and clicked, while the
/// server fired (and leaves the shot sound to the shooter's client). Cycle and fire on the client before the server
/// replies: the client must predict the shot.
/// </summary>
public sealed class ManualActionPredictionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [TestCase("F14WeaponSniperRatslayer")]
    [TestCase("F14WeaponSniper50AntiMateriel")]
    [TestCase("F14WeaponShotgunLever")]
    public async Task CycleThenShootBeforeServerReplies(string gunId)
    {
        var netGun = await PlaceInHands(gunId);
        var sGun = ToServer(netGun);
        await Server.WaitPost(() =>
        {
            if (SEntMan.HasComponent<WieldableComponent>(sGun))
                Assert.That(SEntMan.System<SharedWieldableSystem>().TryWield(sGun, SPlayer));
        });
        await RunTicks(5);
        await Server.WaitPost(() => SEntMan.System<ManualActionSystem>().TryCycle(SPlayer, sGun));
        await RunTicks(60);

        await Server.WaitPost(() =>
        {
            var shot = SEntMan.System<SharedGunSystem>().AttemptShoot(SPlayer, (sGun, SEntMan.GetComponent<GunComponent>(sGun)),
                new EntityCoordinates(MapData.Grid.Owner, 5, 0));
            Assert.That(shot, "first shot didn't fire");
        });
        await RunTicks(150); // past the fire delay

        // Cycle on the client only: the server hasn't seen it yet.
        await SetKey(ContentKeyFunctions.UseItemInHand, BoundKeyState.Down);
        await Client.WaitRunTicks(1);
        await SetKey(ContentKeyFunctions.UseItemInHand, BoundKeyState.Up);
        await Client.WaitRunTicks(1);

        var cGun = CEntMan.GetEntity(netGun);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.GetComponent<ManualActionComponent>(cGun).NeedsCycle, Is.False, "client didn't predict the cycle");
            var guns = CEntMan.System<SharedGunSystem>();
            var shot = guns.AttemptShoot(CPlayer, (cGun, CEntMan.GetComponent<GunComponent>(cGun)),
                CEntMan.GetComponent<TransformComponent>(CPlayer).Coordinates.Offset(new System.Numerics.Vector2(5, 0)));
            Assert.That(shot, "client didn't predict the shot, so the shooter hears no gunshot");
        });
    }
}
