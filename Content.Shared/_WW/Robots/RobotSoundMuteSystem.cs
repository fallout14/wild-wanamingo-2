using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Shared._WW.Robots;

/// <summary>
/// WW: the robots' "silence my sounds" toggle. See <see cref="RobotSoundMuteComponent"/>.
/// </summary>
public sealed partial class RobotSoundMuteSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RobotSoundMuteComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RobotSoundMuteComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RobotSoundMuteComponent, ToggleRobotSoundsEvent>(OnToggle);
    }

    private void OnMapInit(Entity<RobotSoundMuteComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    private void OnShutdown(Entity<RobotSoundMuteComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnToggle(Entity<RobotSoundMuteComponent> ent, ref ToggleRobotSoundsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Muted = !ent.Comp.Muted;
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Muted);
        _popup.PopupClient(Loc.GetString(ent.Comp.Muted ? "robot-sounds-muted" : "robot-sounds-unmuted"), ent, ent);
    }

    /// <summary>
    /// True if this entity is a robot that switched its sounds off.
    /// </summary>
    public bool IsMuted(EntityUid uid)
    {
        return TryComp<RobotSoundMuteComponent>(uid, out var mute) && mute.Muted;
    }
}
