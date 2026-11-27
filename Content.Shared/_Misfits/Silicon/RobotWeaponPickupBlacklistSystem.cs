// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Silicon/SharedRobotWeaponPickupBlacklistSystem.cs
// Changed for Wild Wanamingo: can be switched off per chassis (Robobrains) and also stops the robot firing a gun it
// got hold of some other way. Guns built into its modules (Unremoveable) still work.

using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;

namespace Content.Shared._Misfits.Silicon;

public sealed partial class RobotWeaponPickupBlacklistSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RobotWeaponPickupBlacklistComponent, PickupAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RobotWeaponPickupBlacklistComponent, ShotAttemptedEvent>(OnShotAttempt); // WW Change
    }

    private void OnPickupAttempt(EntityUid uid, RobotWeaponPickupBlacklistComponent component, PickupAttemptEvent args)
    {
        if (component.Enabled && _whitelist.IsWhitelistPass(component.Blacklist, args.Item))
            args.Cancel();
    }

    // WW Change - a robot can't fire a loose gun it was handed; its own module weapons are Unremoveable.
    private void OnShotAttempt(Entity<RobotWeaponPickupBlacklistComponent> ent, ref ShotAttemptedEvent args)
    {
        var gun = args.Used.Owner;
        if (!ent.Comp.Enabled || gun == ent.Owner || HasComp<UnremoveableComponent>(gun))
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, gun))
            args.Cancel();
    }
}
