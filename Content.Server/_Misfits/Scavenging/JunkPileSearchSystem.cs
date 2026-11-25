// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/Scavenging/JunkPileSearchSystem.cs
// Changed for Wild Wanamingo: loot comes from the component's own list; the Scrounger perk's extra ammo is new.

using Content.Server._Misfits.SpecialStats;
using Content.Shared._Misfits.Talents.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared._Misfits.Scavenging;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.Scavenging;

/// <summary>
/// Gives mapped junk piles a deliberate, shared search interaction and a cooldown that begins on a successful search.
/// </summary>
public sealed partial class JunkPileSearchSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpecialLuckSystem _luck = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<JunkPileSearchableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JunkPileSearchableComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<JunkPileSearchableComponent, JunkPileSearchDoAfterEvent>(OnSearchComplete);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<JunkPileSearchableComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.CooldownEnd == TimeSpan.Zero || component.CooldownEnd > _timing.CurTime)
                continue;

            component.CooldownEnd = TimeSpan.Zero;
            UpdateAppearance((uid, component), false);
        }
    }

    private void OnStartup(Entity<JunkPileSearchableComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent, false);
    }

    private void OnInteractHand(Entity<JunkPileSearchableComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var finderSearch = false;
        if (ent.Comp.CooldownEnd > _timing.CurTime)
        {
            // Junker Finder (WW Change): one extra search of a depleted pile per refresh.
            finderSearch = HasComp<TraitJunkerFinderComponent>(args.User) && !ent.Comp.FinderSearchUsed;
            if (!finderSearch)
            {
                var remaining = (int) Math.Ceiling((ent.Comp.CooldownEnd - _timing.CurTime).TotalMinutes);
                _popup.PopupEntity(Loc.GetString("junk-pile-search-cooldown", ("minutes", remaining)), ent, args.User,
                    PopupType.SmallCaution);
                return;
            }
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.SearchDuration,
            new JunkPileSearchDoAfterEvent { FinderSearch = finderSearch }, ent, ent)
        {
            BlockDuplicate = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2f,
        });
    }

    private void OnSearchComplete(Entity<JunkPileSearchableComponent> ent, ref JunkPileSearchDoAfterEvent args)
    {
        if (args.Cancelled || ent.Comp.Contents.Count == 0)
            return;

        var onCooldown = ent.Comp.CooldownEnd > _timing.CurTime;
        if (onCooldown && (!args.FinderSearch || ent.Comp.FinderSearchUsed))
            return;

        if (onCooldown)
        {
            ent.Comp.FinderSearchUsed = true;
        }
        else
        {
            ent.Comp.CooldownEnd = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.CooldownSeconds);
            ent.Comp.FinderSearchUsed = false;
            UpdateAppearance(ent, true);
        }

        var coordinates = Transform(ent).Coordinates;
        var scrounger = HasComp<TraitScroungerComponent>(args.User);
        foreach (var prototype in EntitySpawnCollection.GetSpawns(ent.Comp.Contents, _random))
        {
            var spawned = Spawn(prototype, coordinates);

            // Scrounger: ammunition turns up 40% more often.
            if (scrounger && IsAmmo(spawned) && _random.Prob(ScroungerExtraAmmoChance))
                Spawn(prototype, coordinates);
        }

        if (TryComp<LuckJunkBonusComponent>(ent, out var luck))
            _luck.TryGrantJunkBonus((ent, luck), args.User);

        _popup.PopupEntity(Loc.GetString(args.FinderSearch
            ? "junk-pile-search-finder-complete"
            : "junk-pile-search-complete"), ent, args.User);
    }

    private const float ScroungerExtraAmmoChance = 0.4f;

    private bool IsAmmo(EntityUid uid)
    {
        return HasComp<CartridgeAmmoComponent>(uid) || HasComp<BallisticAmmoProviderComponent>(uid) && !HasComp<GunComponent>(uid);
    }

    private void UpdateAppearance(Entity<JunkPileSearchableComponent> ent, bool depleted)
    {
        _appearance.SetData(ent, JunkPileVisuals.Depleted, depleted);
    }
}
