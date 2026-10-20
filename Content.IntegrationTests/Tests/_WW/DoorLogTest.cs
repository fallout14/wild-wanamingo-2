using Content.IntegrationTests.Fixtures;
using Content.Server._Misfits.Administration.DoorLogs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Misfits: breaking a door records which door it was and who broke it.
/// </summary>
[TestFixture, TestOf(typeof(DoorLogSystem))]
public sealed class DoorLogTest : GameTest
{
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [Test]
    public async Task BrokenDoorIsLogged()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var mapSys = server.System<SharedMapSystem>();
        var damageSys = server.System<DamageableSystem>();
        var doorLogs = server.System<DoorLogSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var map);
            var door = entMan.SpawnEntity("F14DoorWoodRoom", new MapCoordinates(0, 0, map));
            var attacker = entMan.SpawnEntity("MobHuman", new MapCoordinates(1, 0, map));
            var before = doorLogs.GetEntries().Count;

            var brute = new DamageSpecifier(protoMan.Index(Blunt), FixedPoint2.New(5000));
            damageSys.TryChangeDamage(door, brute, ignoreResistances: true, origin: attacker);

            var entries = doorLogs.GetEntries();
            Assert.That(entries, Has.Count.EqualTo(before + 1));
            Assert.That(entries[0].DoorPrototype, Is.EqualTo("F14DoorWoodRoom"));
            Assert.That(entries[0].DestroyedBy, Does.Contain(entMan.GetComponent<MetaDataComponent>(attacker).EntityName));

            entMan.DeleteEntity(mapSys.GetMap(map));
        });
    }
}
