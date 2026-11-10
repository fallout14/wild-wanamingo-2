using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._WW.Traps;

/// <summary>
/// Arming, disarming and springing <see cref="WWTrapComponent"/> traps. Perception/Agility checks come later.
/// </summary>
public sealed partial class WWTrapSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedEnsnareableSystem _ensnare = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WWTrapComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WWTrapComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<WWTrapComponent, WWTrapDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<WWTrapComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WWTrapComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<WWTrapComponent, StepTriggerAttemptEvent>(OnStepAttempt);
        SubscribeLocalEvent<WWTrapComponent, StepTriggeredOnEvent>(OnStepped);
    }

    private void OnMapInit(Entity<WWTrapComponent> ent, ref MapInitEvent args)
    {
        // Traps mapped as armed start anchored.
        if (ent.Comp.Armed)
            SetArmed(ent, true);
        else
            _appearance.SetData(ent, WWTrapVisuals.Armed, false);
    }

    private void OnGetVerbs(Entity<WWTrapComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        if (ent.Comp.Armed)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("ww-trap-verb-disarm"),
                Act = () => StartArming(ent, user, false),
                Priority = 2,
            });
        }
        else if (CanArm(ent, out _))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("ww-trap-verb-arm"),
                Act = () => StartArming(ent, user, true),
                Priority = 2,
            });
        }
    }

    private bool CanArm(Entity<WWTrapComponent> ent, out string? reason)
    {
        reason = null;
        if (ent.Comp.Spent)
        {
            reason = "ww-trap-spent";
            return false;
        }

        // Has to be set down on the ground, not held or in a bag.
        if (_container.IsEntityInContainer(ent) || Transform(ent).GridUid == null)
        {
            reason = "ww-trap-not-on-floor";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Starts the arm (true) or disarm (false) do-after.
    /// </summary>
    public bool StartArming(Entity<WWTrapComponent> ent, EntityUid user, bool arm)
    {
        if (arm && !CanArm(ent, out var reason))
        {
            if (reason != null)
                _popup.PopupClient(Loc.GetString(reason, ("trap", ent.Owner)), ent, user);
            return false;
        }

        var args = new DoAfterArgs(EntityManager, user, arm ? ent.Comp.ArmDelay : ent.Comp.DisarmDelay,
            new WWTrapDoAfterEvent(arm), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        _popup.PopupClient(Loc.GetString(arm ? "ww-trap-arming" : "ww-trap-disarming", ("trap", ent.Owner)), ent, user);
        return true;
    }

    private void OnDoAfter(Entity<WWTrapComponent> ent, ref WWTrapDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || ent.Comp.Armed == args.Arm)
            return;

        if (args.Arm && !CanArm(ent, out _))
            return;

        args.Handled = true;
        SetArmed(ent, args.Arm);
        _audio.PlayPredicted(ent.Comp.ArmSound, ent, args.User);
        _popup.PopupClient(Loc.GetString(args.Arm ? "ww-trap-armed" : "ww-trap-disarmed", ("trap", ent.Owner)), ent, args.User);
        _adminLog.Add(LogType.Action, args.Arm ? LogImpact.Medium : LogImpact.Low,
            $"{ToPrettyString(args.User):player} {(args.Arm ? "armed" : "disarmed")} {ToPrettyString(ent):trap}");
    }

    /// <summary>
    /// Arms or disarms the trap now. An armed trap is anchored where it lies.
    /// </summary>
    public void SetArmed(Entity<WWTrapComponent> ent, bool armed)
    {
        ent.Comp.Armed = armed;
        Dirty(ent);
        _appearance.SetData(ent, WWTrapVisuals.Armed, armed);

        var xform = Transform(ent);
        if (armed && !xform.Anchored)
            _xform.AnchorEntity((ent, xform));
        else if (!armed && xform.Anchored)
            _xform.Unanchor(ent, xform);
    }

    private void OnExamined(Entity<WWTrapComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // TODO: Perception decides whether you notice an armed trap.
        args.PushMarkup(Loc.GetString(ent.Comp.Spent ? "ww-trap-examine-spent"
            : ent.Comp.Armed ? "ww-trap-examine-armed" : "ww-trap-examine-safe"));
    }

    private void OnPickupAttempt(Entity<WWTrapComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (!ent.Comp.Armed)
            return;

        args.Cancel();
        if (args.ShowPopup)
            _popup.PopupClient(Loc.GetString("ww-trap-pickup-armed", ("trap", ent.Owner)), ent, args.User);
    }

    private void OnStepAttempt(Entity<WWTrapComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = ent.Comp.Armed;
        if (HasComp<LightStepComponent>(args.Tripper))
            args.Cancelled = true;
    }

    private void OnStepped(Entity<WWTrapComponent> ent, ref StepTriggeredOnEvent args)
    {
        if (!ent.Comp.Armed)
            return;

        Spring(ent, args.Tripper);
    }

    /// <summary>
    /// Sets the trap off on <paramref name="victim"/>: disarms it (or marks a single-use trap spent), runs its
    /// trigger effects with the victim as the user, and clamps them if it's a bear trap.
    /// </summary>
    public void Spring(Entity<WWTrapComponent> ent, EntityUid victim)
    {
        SetArmed(ent, false);
        if (!ent.Comp.Rearmable)
        {
            ent.Comp.Spent = true;
            Dirty(ent);
        }

        _adminLog.Add(LogType.Trigger, LogImpact.Medium,
            $"{ToPrettyString(victim):player} set off {ToPrettyString(ent):trap}");

        _trigger.Trigger(ent, victim);

        if (ent.Comp.EnsnareVictim && !TerminatingOrDeleted(ent) && TryComp<EnsnaringComponent>(ent, out var ensnaring))
            _ensnare.TryEnsnare(victim, ent, ensnaring);
    }
}
