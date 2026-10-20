#nullable enable
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server._Misfits.Scavenging;
using Content.Shared._Misfits.Scavenging;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Searching a junk pile turns up its loot once, then the pile shrinks and waits out its cooldown.
/// </summary>
[TestOf(typeof(JunkPileSearchSystem))]
public sealed class JunkPileSearchTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task SearchFindsLootThenCoolsDown()
    {
        await SpawnTarget("F14JunkPile1");
        var pile = STarget!.Value;

        // A guaranteed find, so the test doesn't depend on the pile's loot odds.
        await Server.WaitPost(() =>
        {
            var comp = SEntMan.GetComponent<JunkPileSearchableComponent>(pile);
            comp.Contents = [new EntitySpawnEntry { PrototypeId = "F14Paper1" }];
        });

        var before = await CountPaper();
        await Interact();
        Assert.That(await CountPaper(), Is.EqualTo(before + 1), "searching should turn up the pile's loot");

        var comp = SEntMan.GetComponent<JunkPileSearchableComponent>(pile);
        Assert.That(comp.CooldownEnd, Is.GreaterThan(TimeSpan.Zero), "the pile should be on cooldown");

        // Searching again during the cooldown finds nothing.
        await Interact();
        Assert.That(await CountPaper(), Is.EqualTo(before + 1));
    }

    private async Task<int> CountPaper()
    {
        var count = 0;
        await Server.WaitPost(() =>
        {
            var query = SEntMan.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out _, out var meta))
            {
                if (meta.EntityPrototype?.ID == "F14Paper1")
                    count++;
            }
        });
        return count;
    }
}
