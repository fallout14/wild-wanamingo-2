// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Weapons/TwinTube/TwinTubeAmmoProviderSystem.cs
// Changed for Wild Wanamingo: the chamber (fired first, loaded once the selected tube is full), alt-click to switch
// tubes, and loading through Wizden's TryBallisticInsert instead of re-raising InteractUsing on the tube.

using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Misfits.Weapons.TwinTube;

/// <summary>
/// Routes ammunition operations to the chamber and one of two contained ballistic providers.
/// </summary>
public sealed partial class TwinTubeAmmoProviderSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, GetAmmoCountEvent>(OnAmmoCount);
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<TwinTubeAmmoProviderComponent, ExaminedEvent>(OnExamine);
    }

    public EntityUid? GetTube(EntityUid uid, bool right)
    {
        return GetSlot(uid, right ? TwinTubeAmmoProviderComponent.RightTube : TwinTubeAmmoProviderComponent.LeftTube);
    }

    public EntityUid? GetChamber(EntityUid uid)
    {
        return GetSlot(uid, TwinTubeAmmoProviderComponent.ChamberTube);
    }

    private EntityUid? GetSlot(EntityUid uid, string id)
    {
        return _containers.TryGetContainer(uid, id, out var container) && container is ContainerSlot slot
            ? slot.ContainedEntity
            : null;
    }

    public void SelectTube(Entity<TwinTubeAmmoProviderComponent> ent, bool right)
    {
        if (ent.Comp.RightSelected == right)
            return;

        ent.Comp.RightSelected = right;
        Dirty(ent);
        _gun.RefreshAmmoCounter(ent);
    }

    private void OnTakeAmmo(Entity<TwinTubeAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            // The chambered shell goes first, then the selected tube (or the other one once it runs dry).
            EntityUid? source = GetCount(GetChamber(ent)).Count > 0 ? GetChamber(ent) : null;
            if (source == null)
            {
                SelectLoadedTube(ent);
                source = GetTube(ent, ent.Comp.RightSelected);
            }

            if (source is not { } tube)
                break;

            var before = args.Ammo.Count;
            var shot = new TakeAmmoEvent(1, args.Ammo, args.Coordinates, args.User);
            RaiseLocalEvent(tube, shot);
            args.Reason = shot.Reason;
            if (args.Ammo.Count == before)
                break;
        }

        SelectLoadedTube(ent);
        _gun.RefreshAmmoCounter(ent);
    }

    private void SelectLoadedTube(Entity<TwinTubeAmmoProviderComponent> ent)
    {
        if (GetCount(GetTube(ent, ent.Comp.RightSelected)).Count == 0 &&
            GetCount(GetTube(ent, !ent.Comp.RightSelected)).Count > 0)
        {
            SelectTube(ent, !ent.Comp.RightSelected);
        }
    }

    private GetAmmoCountEvent GetCount(EntityUid? uid)
    {
        var count = new GetAmmoCountEvent();
        if (uid != null)
            RaiseLocalEvent(uid.Value, ref count);
        return count;
    }

    private void OnAmmoCount(Entity<TwinTubeAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        foreach (var part in new[] { GetTube(ent, false), GetTube(ent, true), GetChamber(ent) })
        {
            var count = GetCount(part);
            args.Count += count.Count;
            args.Capacity += count.Capacity;
        }
    }

    private void OnInteractUsing(Entity<TwinTubeAmmoProviderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // The selected tube first; once it is full, the chamber.
        foreach (var target in new[] { GetTube(ent, ent.Comp.RightSelected), GetChamber(ent) })
        {
            if (target is not { } part || !TryComp<BallisticAmmoProviderComponent>(part, out var provider))
                continue;

            if (_gun.TryBallisticInsert((part, provider), args.Used, args.User))
            {
                args.Handled = true;
                break;
            }
        }

        _gun.RefreshAmmoCounter(ent);
    }

    private void OnGetVerbs(Entity<TwinTubeAmmoProviderComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("gun-twin-tube-select-left"),
            Disabled = !ent.Comp.RightSelected,
            Act = () => SelectTube(ent, false),
        });
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("gun-twin-tube-select-right"),
            Disabled = ent.Comp.RightSelected,
            Act = () => SelectTube(ent, true),
        });
    }

    private void OnGetAltVerbs(Entity<TwinTubeAmmoProviderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("gun-twin-tube-switch"),
            Priority = 1,
            Act = () =>
            {
                SelectTube(ent, !ent.Comp.RightSelected);
                _popup.PopupClient(Loc.GetString("gun-twin-tube-switched",
                    ("tube", Loc.GetString(ent.Comp.RightSelected ? "gun-twin-tube-right" : "gun-twin-tube-left"))),
                    ent, user);
            },
        });
    }

    private void OnExamine(Entity<TwinTubeAmmoProviderComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-twin-tube-examine",
            ("left", GetCount(GetTube(ent, false)).Count),
            ("right", GetCount(GetTube(ent, true)).Count),
            ("chamber", GetCount(GetChamber(ent)).Count),
            ("selected", Loc.GetString(ent.Comp.RightSelected ? "gun-twin-tube-right" : "gun-twin-tube-left"))));
    }
}
