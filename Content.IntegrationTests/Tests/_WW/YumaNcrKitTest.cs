#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Yuma's NCR, Ranger and Legion issue kits hand out the weapons from the server's roster when opened.
/// </summary>
public sealed class YumaNcrKitTest : GameTest
{
    private static readonly (string Kit, string[] Weapons)[] Kits =
    {
        ("F14KitNCRBaseCommander", new[] { "N14WeaponPistol12mm" }),
        ("F14KitNCRPlatoonLeader", new[] { "N14WeaponRifle556Marksman", "N14WeaponPistol9mm" }),
        ("F14KitNCRDoctor", new[] { "N14WeaponPistol10mm", "N14MedkitFilled" }),
        ("F14KitNCRQuartermaster", new[] { "N14WeaponPistol10mm" }),
        ("F14KitNCRSquadLeader", new[] { "N14WeaponRifle5mmCarbine", "N14WeaponPistol10mm" }),
        ("F14KitNCRWSMarksman", new[] { "N14WeaponRifle308Battle", "N14WeaponPistol9mm" }),
        ("F14KitNCRWSGrenadier", new[] { "N14WeaponRifleGrenade", "N14WeaponPistol9mm" }),
        ("F14KitNCRWSGunner", new[] { "N14WeaponLMG", "N14WeaponPistol9mm" }),
        ("F14KitNCRWSHeavyTrooper", new[] { "N14WeaponFlamer", "N14WeaponPistol9mm" }),
        ("F14KitNCRFieldMedic", new[] { "N14WeaponRifle556Service", "N14WeaponPistol9mm" }),
        ("F14KitNCRTrooper", new[] { "N14WeaponRifle556Service", "N14FirearmBayonet" }),
        ("F14KitNCRGarrisonTechnician", new[] { "N14WeaponPistol9mm", "N14ToolboxMechanical" }),
        ("F14KitNCRGarrisonInfantry", new[] { "N14WeaponRifle10mmM1Carbine" }),
        ("F14KitNCRGarrisonMedic", new[] { "N14WeaponPistol9mm", "N14MedkitFilled" }),
        ("F14KitRangerVeteranDemolitions", new[] { "N14WeaponLauncherGrenade" }),
        ("F14KitRangerVeteranSniper", new[] { "N14WeaponSniper50AntiMateriel" }),
        ("F14KitRangerVeteranAssault", new[] { "N14WeaponRifle556Marksman", "MisfitsGrenadeFragDefensive" }),
        ("F14KitRangerPatrol", new[] { "N14WeaponRifle5mmCarbine" }),
        ("F14KitLegionCenturionSledge", new[] { "N14ripper", "N14Dynamite", "N14SuperSledgeHammer" }),
        ("F14KitLegionCenturionLance", new[] { "N14ripper", "N14WeaponThermospear" }),
        ("F14KitLegionCenturionAMR", new[] { "N14ripper", "N14WeaponSniper50AntiMateriel" }),
        ("F14KitLegionCenturionLauncher", new[] { "N14ripper", "N14WeaponLauncherGrenade" }),
        ("F14KitLegionVexillariusSniper", new[] { "N14ripper", "N14WeaponRifle308SniperRifle" }),
        ("F14KitLegionVexillariusSMG", new[] { "N14ripper", "N14WeaponSMG12mm" }),
        ("F14KitLegionExplorer", new[] { "N14WeaponSniperHunting", "N14WeaponSMG10mm", "F14MacheteGladius", "baseThrowingSpear" }),
        ("F14KitLegionHoundmaster", new[] { "F14MacheteGladius", "N14WeaponRevolver44Magnum" }),
        ("F14KitLegionForgemaster", new[] { "F14MacheteGladius" }),
        ("F14KitLegionDecanusLMG", new[] { "N14Machete", "N14WeaponPistol12mm", "N14WeaponLMG" }),
        ("F14KitLegionDecanusMarksman", new[] { "N14WeaponRifle556Marksman" }),
        ("F14KitLegionDecanusSMG", new[] { "N14WeaponSMG12mm" }),
        ("F14KitLegionVeteranFist", new[] { "N14WeaponPistol12mm", "N14ClothingHandsPowerFistGoliath" }),
        ("F14KitLegionVeteranSMG", new[] { "N14WeaponSMG45Thompson" }),
        ("F14KitLegionVeteranBrushGun", new[] { "N14WeaponSniper45-70BrushGun" }),
        ("F14KitLegionPrimeAxe", new[] { "N14WeaponPistol10mm", "N14FireAxe" }),
        ("F14KitLegionPrimeGreaseGun", new[] { "N14WeaponSMG45GreaseGun" }),
        ("F14KitLegionPrimeRepeater", new[] { "N14WeaponSniperCowboyRepeater" }),
        ("F14KitLegionRecruit", new[] { "N14Machete", "baseThrowingSpear" }),
    };

    [Test]
    public async Task KitsGiveTheirWeapons()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        foreach (var (kit, weapons) in Kits)
        {
            var before = weapons.ToDictionary(w => w, w => Count(entMan, w));
            await server.WaitPost(() =>
            {
                var user = entMan.SpawnEntity("MobHuman", map.GridCoords);
                var box = entMan.SpawnEntity(kit, map.GridCoords);
                entMan.System<SharedHandsSystem>().TryPickupAnyHand(user, box);
                entMan.EventBus.RaiseLocalEvent(box, new UseInHandEvent(user));
            });
            await server.WaitRunTicks(2);
            await server.WaitAssertion(() =>
            {
                foreach (var weapon in weapons)
                    Assert.That(Count(entMan, weapon), Is.GreaterThan(before[weapon]), $"{kit} should give {weapon}");
            });
        }
    }

    private static int Count(IEntityManager entMan, string proto)
    {
        return entMan.AllEntities<MetaDataComponent>().Count(e => e.Comp.EntityPrototype?.ID == proto);
    }
}
