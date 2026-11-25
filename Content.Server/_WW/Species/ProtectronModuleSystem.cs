using Content.Server.Polymorph.Systems;
using Content.Shared._WW.Species;
using Content.Shared.Actions;

namespace Content.Server._WW.Species;

/// <summary>
/// WW: grants a fresh Protectron its module choices and turns it into the chosen model.
/// </summary>
public sealed partial class ProtectronModuleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProtectronModuleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ProtectronModuleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ProtectronModuleComponent, ProtectronModuleActionEvent>(OnModule);
    }

    private void OnMapInit(Entity<ProtectronModuleComponent> ent, ref MapInitEvent args)
    {
        foreach (var proto in ent.Comp.Actions)
        {
            EntityUid? action = null;
            if (_actions.AddAction(ent.Owner, ref action, proto))
                ent.Comp.GrantedActions.Add(action.Value);
        }
    }

    private void OnShutdown(Entity<ProtectronModuleComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.GrantedActions)
        {
            _actions.RemoveAction(ent.Owner, action);
        }
        ent.Comp.GrantedActions.Clear();
    }

    private void OnModule(Entity<ProtectronModuleComponent> ent, ref ProtectronModuleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _polymorph.PolymorphEntity(ent, args.Polymorph);
    }
}
