// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): the Strength slowdown relief from
// Content.Shared/Clothing/ClothingSpeedModifierSystem.cs and Content.Shared/Item/HeldSpeedModifierSystem.cs.
// WW: Misfits edited Wizden's systems; here a separate system cancels the part of the slowdown a strong carrier ignores.

using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._Misfits.SpecialStats.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Containers;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Strong characters (Strength at or above <see cref="StrengthIgnoreClothingSlowdownComponent.MinimumStrength"/>)
/// carry heavy bags with less (or no) slowdown, worn or held.
/// </summary>
public sealed partial class SpecialStrengthSlowdownSystem : EntitySystem
{
    [Dependency] private SharedSpecialSystem _special = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private HeldSpeedModifierSystem _heldSpeed = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrengthIgnoreClothingSlowdownComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnWornRefresh);
        SubscribeLocalEvent<StrengthIgnoreClothingSlowdownComponent, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnHeldRefresh);
    }

    private void OnWornRefresh(EntityUid uid, StrengthIgnoreClothingSlowdownComponent component, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        // Mirror ClothingSpeedModifierSystem: only relieve slowdown that is actually being applied.
        if (!TryComp<ClothingSpeedModifierComponent>(uid, out var speed))
            return;

        if (speed.RequireActivated && !_toggle.IsActivated(uid))
            return;

        if (speed.Standing != null && !_standing.IsMatchingState(args.Owner, speed.Standing.Value))
            return;

        Compensate(args.Owner, component, speed.WalkModifier, speed.SprintModifier, args.Args);
    }

    private void OnHeldRefresh(EntityUid uid, StrengthIgnoreClothingSlowdownComponent component, HeldRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!TryComp<HeldSpeedModifierComponent>(uid, out var held) ||
            !_container.TryGetContainingContainer((uid, null, null), out var hand))
        {
            return;
        }

        var (walk, sprint) = _heldSpeed.GetHeldMovementSpeedModifiers(uid, held);
        Compensate(hand.Owner, component, walk, sprint, args.Args);
    }

    private void Compensate(EntityUid carrier,
        StrengthIgnoreClothingSlowdownComponent component,
        float walk,
        float sprint,
        RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<SpecialComponent>(carrier, out var special) ||
            _special.GetEffective(carrier, SpecialStat.Strength, special) < component.MinimumStrength)
        {
            return;
        }

        args.ModifySpeed(Relief(walk, component.ApplyStrengthModifier(walk, sprint: false)),
            Relief(sprint, component.ApplyStrengthModifier(sprint, sprint: true)));
    }

    /// <summary>
    /// The multiplier that turns the applied modifier into the Strength floor.
    /// </summary>
    private static float Relief(float applied, float floored)
    {
        return applied > 0f ? floored / applied : 1f;
    }
}
