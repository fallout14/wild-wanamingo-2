// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/Overlays/Switchable/ThermalVisionOverlay.cs
// (Misfits' copy of Goobstation's switchable overlays.)
// WW: warm bodies are living mobs (Wizden's body has no thermal-visibility flag); tinted through the draw handle
// instead of changing each sprite's colour.

using System.Numerics;
using Content.Client.Stealth;
using Content.Shared._Misfits.Overlays.Legacy;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Misfits.Overlays.Legacy;

/// <summary>
/// Draws living creatures on top of everything, so they show through walls.
/// </summary>
public sealed partial class ThermalVisionOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly TransformSystem _transform;
    private readonly StealthSystem _stealth;
    private readonly ContainerSystem _container;
    private readonly SharedPointLightSystem _light;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<EntityUid> _drawn = new();

    private EntityUid? _lightEntity;

    public float LightRadius;

    public ThermalVisionComponent? Comp;

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
        _stealth = _entity.System<StealthSystem>();
        _light = _entity.System<SharedPointLightSystem>();
        _sprite = _entity.System<SpriteSystem>();

        ZIndex = -1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Comp is null || args.Viewport.Eye is not { } eye)
            return;

        if (!_entity.TryGetComponent(_player.LocalEntity, out TransformComponent? playerXform))
            return;

        var accumulator = Math.Clamp(Comp.PulseAccumulator, 0f, Comp.PulseTime);
        var alpha = Comp.PulseTime <= 0f ? 1f : float.Lerp(1f, 0f, accumulator / Comp.PulseTime);

        // Thermal vision grants some night vision (client-side light).
        if (LightRadius > 0)
        {
            _lightEntity ??= _entity.SpawnAttachedTo(null, playerXform.Coordinates);
            _transform.SetParent(_lightEntity.Value, _player.LocalEntity!.Value);
            var light = _entity.EnsureComponent<PointLightComponent>(_lightEntity.Value);
            _light.SetRadius(_lightEntity.Value, LightRadius, light);
            _light.SetEnergy(_lightEntity.Value, alpha, light);
            _light.SetColor(_lightEntity.Value, Comp.Color, light);
        }
        else
        {
            ResetLight();
        }

        var handle = args.WorldHandle;
        var mapId = eye.Position.MapId;
        var eyeRot = eye.Rotation;
        var oldModulate = handle.Modulate;
        handle.Modulate = Comp.Color.WithAlpha(alpha);

        _drawn.Clear();
        var query = _entity.EntityQueryEnumerator<MobStateComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mob, out var sprite, out var xform))
        {
            if (mob.CurrentState == MobState.Dead || !CanSee(uid, sprite))
                continue;

            // Something carried or inside a locker shows as its container.
            if (_container.TryGetOuterContainer(uid, xform, out var container)
                && _entity.TryGetComponent<SpriteComponent>(container.Owner, out var ownerSprite)
                && _entity.TryGetComponent<TransformComponent>(container.Owner, out var ownerXform))
            {
                uid = container.Owner;
                sprite = ownerSprite;
                xform = ownerXform;
            }

            if (xform.MapID != mapId || !CanSee(uid, sprite) || !_drawn.Add(uid))
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            _sprite.RenderSprite((uid, sprite), handle, eyeRot, rotation, position);
        }

        handle.Modulate = oldModulate;
        handle.SetTransform(Matrix3x2.Identity);
    }

    private bool CanSee(EntityUid uid, SpriteComponent sprite)
    {
        return sprite.Visible && (!_entity.TryGetComponent(uid, out StealthComponent? stealth) ||
                                  _stealth.GetVisibility(uid, stealth) > 0.5f);
    }

    public void ResetLight(bool checkFirstTimePredicted = true)
    {
        if (_lightEntity == null || checkFirstTimePredicted && !_timing.IsFirstTimePredicted)
            return;

        _entity.DeleteEntity(_lightEntity);
        _lightEntity = null;
    }
}
