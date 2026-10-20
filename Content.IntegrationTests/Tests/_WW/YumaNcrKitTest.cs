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
        ("F14KitNCRBaseCommander", new[] { "F14WeaponPistol12mm" }),
        ("F14KitNCRPlatoonLeader", new[] { "F14WeaponRifle556Marksman", "F14WeaponPistol9mm" }),
        ("F14KitNCRDoctor", new[] { "F14WeaponPistol10mm", "F14MedkitFilled" }),
        ("F14KitNCRQuartermaster", new[] { "F14WeaponPistol10mm" }),
        ("F14KitNCRSquadLeader", new[] { "F14WeaponRifle5mmCarbine", "F14WeaponPistol10mm" }),
        ("F14KitNCRWSMarksman", new[] { "F14WeaponRifle308Battle", "F14WeaponPistol9mm" }),
        ("F14KitNCRWSGrenadier", new[] { "F14WeaponRifleGrenade", "F14WeaponPistol9mm" }),
        ("F14KitNCRWSGunner", new[] { "F14WeaponLMG", "F14WeaponPistol9mm" }),
        ("F14KitNCRWSHeavyTrooper", new[] { "F14WeaponFlamer", "F14WeaponPistol9mm" }),
        ("F14KitNCRFieldMedic", new[] { "F14WeaponRifle556Service", "F14WeaponPistol9mm" }),
        ("F14KitNCRTrooper", new[] { "F14WeaponRifle556Service", "F14FirearmBayonet" }),
        ("F14KitNCRGarrisonTechnician", new[] { "F14WeaponPistol9mm", "F14ToolboxMechanical" }),
        ("F14KitNCRGarrisonInfantry", new[] { "F14WeaponRifle10mmM1Carbine" }),
        ("F14KitNCRGarrisonMedic", new[] { "F14WeaponPistol9mm", "F14MedkitFilled" }),
        ("F14KitRangerVeteranDemolitions", new[] { "F14WeaponLauncherGrenade" }),
        ("F14KitRangerVeteranSniper", new[] { "F14WeaponSniper50AntiMateriel" }),
        ("F14KitRangerVeteranAssault", new[] { "F14WeaponRifle556Marksman", "F14GrenadeFragDefensive" }),
        ("F14KitRangerPatrol", new[] { "F14WeaponRifle5mmCarbine" }),
        ("F14KitLegionCenturionSledge", new[] { "F14Ripper", "F14Dynamite", "F14SuperSledgeHammer" }),
        ("F14KitLegionCenturionLance", new[] { "F14Ripper", "F14WeaponThermospear" }),
        ("F14KitLegionCenturionAMR", new[] { "F14Ripper", "F14WeaponSniper50AntiMateriel" }),
        ("F14KitLegionCenturionLauncher", new[] { "F14Ripper", "F14WeaponLauncherGrenade" }),
        ("F14KitLegionVexillariusSniper", new[] { "F14Ripper", "F14WeaponRifle308SniperRifle" }),
        ("F14KitLegionVexillariusSMG", new[] { "F14Ripper", "F14WeaponSMG12mm" }),
        ("F14KitLegionExplorer", new[] { "F14WeaponSniperHunting", "F14WeaponSMG10mm", "F14MacheteGladius", "F14BaseThrowingSpear" }),
        ("F14KitLegionHoundmaster", new[] { "F14MacheteGladius", "F14WeaponRevolver44Magnum" }),
        ("F14KitLegionForgemaster", new[] { "F14MacheteGladius" }),
        ("F14KitLegionDecanusLMG", new[] { "F14Machete", "F14WeaponPistol12mm", "F14WeaponLMG" }),
        ("F14KitLegionDecanusMarksman", new[] { "F14WeaponRifle556Marksman" }),
        ("F14KitLegionDecanusSMG", new[] { "F14WeaponSMG12mm" }),
        ("F14KitLegionVeteranFist", new[] { "F14WeaponPistol12mm", "F14ClothingHandsPowerFistGoliath" }),
        ("F14KitLegionVeteranSMG", new[] { "F14WeaponSMG45Thompson" }),
        ("F14KitLegionVeteranBrushGun", new[] { "F14WeaponSniper4570BrushGun" }),
        ("F14KitLegionPrimeAxe", new[] { "F14WeaponPistol10mm", "F14FireAxe" }),
        ("F14KitLegionPrimeGreaseGun", new[] { "F14WeaponSMG45GreaseGun" }),
        ("F14KitLegionPrimeRepeater", new[] { "F14WeaponSniperCowboyRepeater" }),
        ("F14KitLegionRecruit", new[] { "F14Machete", "F14BaseThrowingSpear" }),
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
