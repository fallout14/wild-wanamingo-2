using Content.Shared._Misfits.Talents.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._WW.Talents;

/// <summary>
/// WW: effects of the Misfits talent perks that Misfits only described. The others hook in where their effect lives:
/// Swift Learner and Fortune's Favor's luck boost in SharedSpecialSystem, Scrounger and Junker Finder in
/// JunkPileSearchSystem, Educated in the trade stores, Nerd Rage!'s damage in SpecialCombatSystem and its attack
/// speed in SpecialAgilitySystem.
/// </summary>
public sealed partial class TalentPerkSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    /// <summary>Nerd Rage! kicks in below this fraction of health.</summary>
    public const float NerdRageHealthFraction = 0.25f;
    public const float NerdRageDamageMultiplier = 1.2f;
    public const float NerdRageSpeedMultiplier = 1.15f;

    /// <summary>Fortune's Favor: chance that a kill readies your next melee strike at once.</summary>
    public const float FortunesFavorBonusStrikeChance = 0.25f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TraitNerdRageComponent, DamageChangedEvent>(OnNerdRageDamageChanged);
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    public static bool IsEnraged(IEntityManager entMan, EntityUid uid)
    {
        return entMan.TryGetComponent<TraitNerdRageComponent>(uid, out var rage) && rage.Enraged;
    }

    private void OnNerdRageDamageChanged(Entity<TraitNerdRageComponent> ent, ref DamageChangedEvent args)
    {
        if (_net.IsClient)
            return;

        var enraged = false;
        if (!_mobState.IsDead(ent) &&
            _thresholds.TryGetIncapThreshold(ent, out var incap) &&
            incap.Value > 0)
        {
            var health = 1f - (float) (_damageable.GetTotalDamage(ent.Owner) / incap.Value);
            enraged = health < NerdRageHealthFraction;
        }

        if (enraged == ent.Comp.Enraged)
            return;

        ent.Comp.Enraged = enraged;
        Dirty(ent);

        if (enraged)
            _popup.PopupEntity(Loc.GetString("talent-nerd-rage-start"), ent, ent, PopupType.MediumCaution);

        foreach (var held in _hands.EnumerateHeld(ent.Owner))
        {
            if (HasComp<GunComponent>(held))
                _gun.RefreshModifiers(held);
        }
    }

    private void OnGunRefreshModifiers(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (_containers.TryGetContainingContainer(ent.Owner, out var container) &&
            IsEnraged(EntityManager, container.Owner))
        {
            args.FireRate *= NerdRageSpeedMultiplier;
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (_net.IsClient ||
            args.NewMobState != MobState.Dead ||
            args.Origin is not { } killer ||
            killer == args.Target ||
            !HasComp<TraitFortunesFavorComponent>(killer) ||
            !_random.Prob(FortunesFavorBonusStrikeChance) ||
            !_melee.TryGetWeapon(killer, out var weaponUid, out var melee))
        {
            return;
        }

        melee.NextAttack = _timing.CurTime;
        Dirty(weaponUid, melee);
        _popup.PopupEntity(Loc.GetString("talent-fortunes-favor-bonus-strike"), killer, killer, PopupType.Medium);
    }
}
