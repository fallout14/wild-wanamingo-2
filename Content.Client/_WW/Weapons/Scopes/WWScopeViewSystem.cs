using Content.Client.Movement.Components;
using Content.Client.Movement.Systems;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared.Camera;
using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Client._WW.Weapons.Scopes;

/// <summary>
/// Moves the view toward the cursor while a scoped gun is wielded, using Wizden's binocular/Hristov cursor offset
/// with the reach set by the scope's magnification.
/// </summary>
public sealed partial class WWScopeViewSystem : EntitySystem
{
    [Dependency] private EyeCursorOffsetSystem _eyeOffset = default!;
    [Dependency] private WWScopeSystem _scope = default!;

    // Per-gun pan state (current/target position), so the view eases toward the cursor instead of jumping.
    private readonly Dictionary<EntityUid, EyeCursorOffsetComponent> _pan = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunComponent, HeldRelayedEvent<GetEyeOffsetRelayedEvent>>(OnGetEyeOffset);
        SubscribeLocalEvent<GunComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetEyeOffset(Entity<GunComponent> ent, ref HeldRelayedEvent<GetEyeOffsetRelayedEvent> args)
    {
        if (!_scope.TryGetActiveScope(ent, out var scope))
        {
            _pan.Remove(ent);
            return;
        }

        if (!_pan.TryGetValue(ent, out var pan))
            _pan[ent] = pan = new EyeCursorOffsetComponent();
        pan.MaxOffset = scope.Value.Comp.MaxOffset;

        if (_eyeOffset.OffsetAfterMouse(ent, pan) is { } offset)
            args.Args.Offset += offset;
    }

    private void OnShutdown(Entity<GunComponent> ent, ref ComponentShutdown args)
    {
        _pan.Remove(ent);
    }
}
