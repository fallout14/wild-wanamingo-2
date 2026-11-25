#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._Forge.Barks;
using Content.Shared._WW.Species;
using Content.Shared._WW.Traits;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
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
/// 2026-11-23 character creation changes: lobby species, special roles, bark voice / body size / vocal style on the
/// profile, the talent tree rules and the Protectron module choice.
/// </summary>
public sealed class CharacterCreationTest : GameTest
{
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
        await server.WaitPost(() => bot = entMan.SpawnEntity("F14PlayerSyntheticProtectronUnassigned", map.GridCoords));
        await Pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var module = entMan.GetComponent<ProtectronModuleComponent>(bot);
            Assert.That(module.GrantedActions, Has.Count.EqualTo(4));

            var ev = new ProtectronModuleActionEvent { Polymorph = "F14ProtectronModuleFire", Performer = bot };
            entMan.EventBus.RaiseLocalEvent(bot, (object) ev);
            Assert.That(ev.Handled);
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
        });
    }
}
