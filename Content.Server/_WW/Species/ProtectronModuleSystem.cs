using Content.Server.Polymorph.Systems;
using Content.Shared._WW.Species;
using Content.Shared.Actions;
using Content.Shared.Polymorph;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._WW.Species;

/// <summary>
/// WW: lets a fresh Protectron pick its module from a chassis-selection window and turns it into that model. The
/// window opens as soon as a player takes the body over; the select-module action reopens it if they close it.
/// </summary>
public sealed partial class ProtectronModuleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SkinTintedSpriteSystem _tint = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProtectronModuleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProtectronModuleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ProtectronModuleComponent, ProtectronSelectModuleEvent>(OnSelectAction);
        SubscribeLocalEvent<ProtectronModuleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ProtectronModuleComponent, ProtectronModuleSelectMessage>(OnSelect);
    }

    private void OnMapInit(Entity<ProtectronModuleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.SelectActionEntity, ent.Comp.SelectAction);
    }

    private void OnShutdown(Entity<ProtectronModuleComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.SelectActionEntity);
    }

    private void OnSelectAction(Entity<ProtectronModuleComponent> ent, ref ProtectronSelectModuleEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        _ui.TryToggleUi(ent.Owner, ProtectronModuleUiKey.Key, actor.PlayerSession);
    }

    private void OnPlayerAttached(Entity<ProtectronModuleComponent> ent, ref PlayerAttachedEvent args)
    {
        _ui.OpenUi(ent.Owner, ProtectronModuleUiKey.Key, args.Entity);
    }

    private void OnSelect(Entity<ProtectronModuleComponent> ent, ref ProtectronModuleSelectMessage args)
    {
        if (args.Actor != ent.Owner || args.Index < 0 || args.Index >= ent.Comp.Modules.Count)
            return;

        _ui.CloseUi(ent.Owner, ProtectronModuleUiKey.Key);
        Install(ent, ent.Comp.Modules[args.Index].Polymorph);
    }

    private void Install(EntityUid uid, ProtoId<PolymorphPrototype> polymorph)
    {
        // Keep the paint job (read before the old body goes away).
        Color? paint = TryComp<SkinTintedSpriteComponent>(uid, out var tint) ? tint.Color : null;
        var newBody = _polymorph.PolymorphEntity(uid, polymorph);
        if (newBody != null && paint != null)
            _tint.SetColor(newBody.Value, paint.Value);
    }
}
