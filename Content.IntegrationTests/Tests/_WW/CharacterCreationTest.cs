#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._Forge.Barks;
using Content.Shared._Misfits.Silicon;
using Content.Shared._WW.Species;
using Content.Shared._WW.Traits;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Containers;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Weapons.Ranged.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// 2026-11-23/26 character creation and robot changes: lobby species, special roles, bark voice / body size / vocal style on the
/// profile, the talent tree rules and the Protectron module choice.
/// </summary>
public sealed class CharacterCreationTest : GameTest
{
    private static readonly ProtoId<SpeciesPrototype> HumanSpecies = "Human";
    private static readonly ProtoId<SpeciesPrototype> MrHandySpecies = "F14RobotMrHandy";

    [Test]
    public void LobbySpecies()
    {
        var open = Pair.Server.ProtoMan.EnumeratePrototypes<SpeciesPrototype>()
            .Where(s => s.RoundStart && !s.Whitelisted)
            .Select(s => s.ID)
            .ToHashSet();

        Assert.That(open, Is.EquivalentTo(new[] { "Human", "F14Ghoul", "F14RobotMrHandy", "F14RobotProtectron" }));
    }

    [Test]
    public void SpecialRoles()
    {
        var roles = Pair.Server.ProtoMan.EnumeratePrototypes<AntagPrototype>()
            .Where(a => a.SetPreference && a.Tags.Contains("F14SpecialRole"))
            .Select(a => a.ID);

        Assert.That(roles, Is.EquivalentTo(new[]
            { "F14Frumentarii", "F14EnclavePatrol", "F14CircleOfSteelAgent", "F14OSIScientist" }));
    }

    [Test]
    public void BarkSoundsExist()
    {
        var res = Pair.Server.ResolveDependency<IResourceManager>();
        var barks = Pair.Server.ProtoMan.EnumeratePrototypes<BarkPrototype>().ToList();
        Assert.That(barks.Any(b => b.ID == BarkPrototype.DefaultBarkVoice));
        Assert.Multiple(() =>
        {
            foreach (var bark in barks)
            {
                Assert.That(bark.SoundFiles, Is.Not.Empty, bark.ID);
                foreach (var file in bark.SoundFiles)
                {
                    Assert.That(res.ContentFileExists(new ResPath(file)), $"{bark.ID}: missing {file}");
                }
            }
        });
    }

    [Test]
    public async Task ExtrasSaveAndLoad()
    {
        var server = Pair.Server;
        var cfg = server.ResolveDependency<IConfigurationManager>();
        var serialization = server.ResolveDependency<ISerializationManager>();
        var opsLog = server.ResolveDependency<ILogManager>().GetSawmill("db.ops");
        var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        var builder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        builder.UseSqlite(conn);
        var db = new ServerDbSqlite(() => builder.Options, true, cfg, true, opsLog, serialization, false);
        var preferences = (ServerPreferencesManager) server.ResolveDependency<IServerPreferencesManager>();
        var user = new NetUserId(new Guid("5d0c3e3a-2b8e-4f2a-9a55-1d7a3b9e0c42"));

        var profile = new HumanoidCharacterProfile
        {
            Name = "Courier Six",
            Species = "Human",
            Age = 30,
            Appearance = new(Color.Azure, Color.Beige, new()),
        }
            .WithBarkVoice("BarksUndertaleSans")
            .WithHeight(1.1f)
            .WithWidth(0.9f)
            .WithSpeechVerb("F14VocalRasp");

        await db.InitPrefsAsync(user, profile);
        var prefs = await db.GetPlayerPreferencesAsync(user);
        var loaded = preferences.ConvertProfiles(prefs!.Profiles.Find(p => p.Slot == 0)!);

        Assert.That(loaded.BarkVoice, Is.EqualTo("BarksUndertaleSans"));
        Assert.That(loaded.Height, Is.EqualTo(1.1f).Within(0.001f));
        Assert.That(loaded.Width, Is.EqualTo(0.9f).Within(0.001f));
        Assert.That(loaded.SpeechVerb.Id, Is.EqualTo("F14VocalRasp"));
        Assert.That(loaded.MemberwiseEquals(profile));
    }

    [Test]
    public void TalentTreeRules()
    {
        var protos = Pair.Server.ProtoMan;
        var profile = new HumanoidCharacterProfile();

        // Needs the prerequisite.
        var noPrereq = profile.WithTraitPreference("F14PerkScrounger", protos);
        Assert.That(noPrereq.TraitPreferences, Does.Not.Contain(new ProtoId<TraitPrototype>("F14PerkScrounger")));

        // Swift Learner (2) + Scrounger (3) + Educated (4) = 9 of 10 points.
        var chain = profile
            .WithTraitPreference("F14PerkSwiftLearner", protos)
            .WithTraitPreference("F14PerkScrounger", protos)
            .WithTraitPreference("F14PerkEducated", protos);
        Assert.That(chain.TraitPreferences, Has.Count.EqualTo(3));
        Assert.That(TraitTree.SpentPoints(chain.TraitPreferences, protos), Is.EqualTo(9));

        // Junker Finder (3) doesn't fit.
        var over = chain.WithTraitPreference("F14PerkJunkerFinder", protos);
        Assert.That(over.TraitPreferences, Does.Not.Contain(new ProtoId<TraitPrototype>("F14PerkJunkerFinder")));

        // Dropping the root drops the whole branch.
        var dropped = chain.WithoutTraitPreference("F14PerkSwiftLearner", protos);
        Assert.That(dropped.TraitPreferences, Is.Empty);

        // Validation prunes orphans (e.g. an imported profile).
        var pruned = TraitTree.Prune(new ProtoId<TraitPrototype>[] { "F14PerkEducated", "F14PerkSwiftLearner" }, protos);
        Assert.That(pruned, Is.EquivalentTo(new ProtoId<TraitPrototype>[] { "F14PerkSwiftLearner" }));
    }

    [Test]
    public async Task ProtectronPicksAModule()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        EntityUid bot = default;
        await server.WaitPost(() =>
        {
            bot = entMan.SpawnEntity("F14PlayerSyntheticProtectronUnassigned", map.GridCoords);
            entMan.System<SharedSkinTintedSpriteSystem>().SetColor(bot, Color.Red);
        });
        await Pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var module = entMan.GetComponent<ProtectronModuleComponent>(bot);
            Assert.That(module.Modules, Has.Count.EqualTo(4));
            Assert.That(module.SelectActionEntity, Is.Not.Null, "it should get the select-module action");

            // Option 2 is the fire module.
            var msg = new ProtectronModuleSelectMessage(2) { Actor = bot, UiKey = ProtectronModuleUiKey.Key };
            entMan.EventBus.RaiseLocalEvent(bot, (object) msg);
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var fire = new List<EntityUid>();
            var query = entMan.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var uid, out var meta))
            {
                if (meta.EntityPrototype?.ID == "F14PlayerSyntheticProtectronFire")
                    fire.Add(uid);
            }

            Assert.That(fire, Has.Count.EqualTo(1), "the Protectron should have become the fire model");
            Assert.That(entMan.HasComponent<ProtectronModuleComponent>(fire[0]), Is.False,
                "the new body shouldn't offer modules again");
            Assert.That(entMan.GetComponent<SkinTintedSpriteComponent>(fire[0]).Color, Is.EqualTo(Color.Red),
                "the paint job should carry over");
        });
    }

    [Test]
    public async Task ProtectronPicksFromTheMenu()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        EntityUid bot = default;
        await server.WaitPost(() => bot = entMan.SpawnEntity("F14PlayerSyntheticProtectronUnassigned", map.GridCoords));
        await Pair.RunTicksSync(5);

        // Option 1 is the builder module (menu order).
        await server.WaitPost(() =>
        {
            var msg = new ProtectronModuleSelectMessage(1) { Actor = bot, UiKey = ProtectronModuleUiKey.Key };
            entMan.EventBus.RaiseLocalEvent(bot, (object) msg);
        });
        await Pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var builders = 0;
            var query = entMan.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out _, out var meta))
            {
                if (meta.EntityPrototype?.ID == "F14PlayerSyntheticProtectronBuilder")
                    builders++;
            }

            Assert.That(builders, Is.EqualTo(1), "picking from the menu should install the builder module");
        });
    }

    [Test]
    public void JobSpeciesRequirements()
    {
        var protos = Pair.Server.ProtoMan;
        var roles = Pair.Server.EntMan.System<SharedRoleSystem>();

        HashSet<string>? Allowed(string job)
        {
            return roles.GetRoleRequirements(new ProtoId<JobPrototype>(job))?.OfType<SpeciesRequirement>()
                .SingleOrDefault()?.Species.Select(s => s.Id).ToHashSet();
        }

        Assert.Multiple(() =>
        {
            Assert.That(Allowed("F14BoSKnight"), Is.EquivalentTo(new[] { "Human" }));
            Assert.That(Allowed("F14NCRSoldier"), Is.EquivalentTo(new[] { "Human", "F14Ghoul" }));
            foreach (var job in new[] { "F14Townsperson", "F14Wastelander" })
            {
                Assert.That(Allowed(job), Does.Contain("F14RobotProtectron").And.Contain("F14RobotMrHandy").And.Contain("F14GhoulGlowing"), job);
            }

            // Every species requirement names real species.
            foreach (var job in protos.EnumeratePrototypes<JobPrototype>())
            {
                foreach (var req in roles.GetRoleRequirements(job)?.OfType<SpeciesRequirement>() ?? [])
                {
                    foreach (var species in req.Species)
                    {
                        Assert.That(protos.HasIndex(species), $"{job.ID}: unknown species {species}");
                    }
                }
            }
        });
    }

    [Test]
    public void RobotModuleWeapons()
    {
        var protos = Pair.Server.ProtoMan;
        var factory = Pair.Server.EntMan.ComponentFactory;

        // The guns a robot gets from its modules, as (ammo, fire rate); the chassis itself has no gun any more.
        HashSet<(string, float)> Guns(string robot)
        {
            var proto = protos.Index(robot);
            Assert.That(proto.TryGetComponent<GunComponent>(out _, factory), Is.False, $"{robot}: weapons belong in its modules");
            var guns = new HashSet<(string, float)>();
            if (!proto.TryGetComponent<ContainerFillComponent>(out var fill, factory)
                || !fill.Containers.TryGetValue("borg_module", out var modules))
                return guns;

            foreach (var module in modules)
            {
                if (!protos.Index(module).TryGetComponent<ItemBorgModuleComponent>(out var items, factory))
                    continue;

                foreach (var hand in items.Hands)
                {
                    if (hand.Item is not { } item)
                        continue;

                    var itemProto = protos.Index(item);
                    if (!itemProto.TryGetComponent<GunComponent>(out var gun, factory))
                        continue;

                    if (itemProto.TryGetComponent<BasicEntityAmmoProviderComponent>(out var ammo, factory))
                        guns.Add((ammo.Proto.Id, gun.FireRate));
                    else if (itemProto.TryGetComponent<BatteryAmmoProviderComponent>(out var battery, factory))
                        guns.Add((battery.Prototype.Id, gun.FireRate));
                }
            }

            return guns;
        }

        Assert.Multiple(() =>
        {
            var laser = ("F14MFLaserRed", 2f);
            foreach (var robot in new[] { "F14PlayerSyntheticProtectron", "F14PlayerSyntheticProtectronUnassigned",
                         "F14PlayerSyntheticProtectronFire", "F14PlayerSyntheticProtectronTribal",
                         "F14PlayerSyntheticSecuritronGray", "F14PlayerSyntheticRobobrain" })
            {
                Assert.That(Guns(robot), Does.Contain(laser), robot);
            }

            Assert.That(Guns("F14PlayerSyntheticProtectronPolice"), Does.Contain(("F14MFLaserRed", 1.5f)));
            Assert.That(Guns("F14PlayerSyntheticProtectronBuilder"), Does.Contain(("F14BulletNail", 6f)));
            Assert.That(Guns("F14PlayerSyntheticMrGutsy"), Does.Contain(("F14BulletPlasma", 2.5f)));
            Assert.That(Guns("F14PlayerSyntheticSentryBotMinigun"), Does.Contain(("F14Bullet5mm", 8f)).And.Contain(("F14ProjectileMissile", 0.5f)));
            Assert.That(Guns("F14PlayerSyntheticSentryBotLaser"), Does.Contain(("F14MFLaserRed", 8f)).And.Contain(("F14ProjectileMissile", 0.5f)));
            foreach (var robot in new[] { "F14PlayerSyntheticAssaultron", "F14PlayerSyntheticAssaultronInvader", "F14PlayerSyntheticAssaultronDominator" })
            {
                Assert.That(Guns(robot), Does.Contain(("F14AssaultronEyeBeam", 10f)), robot);
            }

            // Only Robobrains may pick up and fire other weapons.
            foreach (var robot in new[] { "F14PlayerSyntheticProtectron", "F14PlayerSyntheticMrGutsy", "F14PlayerSyntheticRobobrain" })
            {
                var blacklist = (RobotWeaponPickupBlacklistComponent) protos.Index(robot).Components["RobotWeaponPickupBlacklist"].Component;
                Assert.That(blacklist.Enabled, Is.EqualTo(robot != "F14PlayerSyntheticRobobrain"), robot);
            }

            // The old N14/Misfits borg chassis that duplicated the player robots are gone.
            foreach (var old in new[] { "F14RobotProtectron", "F14RobotProtectronBuilder", "F14RobotMrHandy", "F14RobotMrGutsy", "F14RobotSecuritron" })
            {
                Assert.That(protos.HasIndex<EntityPrototype>(old), Is.False, $"{old} duplicates a player robot");
            }

            // Every robot can mute itself; Sentry Bots can blow their fusion core.
            foreach (var robot in new[] { "F14PlayerSyntheticProtectron", "F14PlayerSyntheticSentryBotLaser", "F14MobPlayerRobobrain" })
            {
                Assert.That(protos.Index(robot).Components.ContainsKey("RobotSoundMute"), robot);
            }
            Assert.That(protos.Index(new EntProtoId("F14PlayerSyntheticSentryBotMinigun")).Components.ContainsKey("FusionCoreDetonator"));

            // Mr. Gutsy lost the botany module.
            foreach (var robot in new[] { "F14PlayerSyntheticMrGutsy" })
            {
                var fill = (ContainerFillComponent) protos.Index(robot).Components["ContainerFill"].Component;
                Assert.That(fill.Containers["borg_module"], Does.Not.Contain(new EntProtoId("F14BorgModuleBotany")), robot);
            }
        });
    }

    [Test]
    public async Task RobotSkinColour()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protos = server.ProtoMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var robot = protos.Index(MrHandySpecies);
            Assert.That(robot.HasEyeColor, Is.False);
            Assert.That(robot.Barks, Is.False);
            Assert.That(protos.Index(HumanSpecies).HasEyeColor);

            var uid = entMan.SpawnEntity("F14PlayerSyntheticMrHandy", map.GridCoords);
            var profile = new HumanoidCharacterProfile { Species = "F14RobotMrHandy" }
                .WithCharacterAppearance(new(Color.White, Color.Lime, new()));
            entMan.System<SharedVisualBodySystem>().ApplyProfileTo(uid, profile);
            Assert.That(entMan.GetComponent<SkinTintedSpriteComponent>(uid).Color, Is.EqualTo(Color.Lime));
            entMan.DeleteEntity(uid);

            var doll = entMan.SpawnEntity(robot.DollPrototype, map.GridCoords);
            entMan.System<SharedVisualBodySystem>().ApplyProfileTo(doll, profile);
            Assert.That(entMan.GetComponent<SkinTintedSpriteComponent>(doll).Color, Is.EqualTo(Color.Lime));
            entMan.DeleteEntity(doll);
        });
    }

    [Test]
    public void NoContrabandNotices()
    {
        Assert.That(Pair.Server.CfgMan.GetCVar(CCVars.ContrabandExamine), Is.False);
    }
}
