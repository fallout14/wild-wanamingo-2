// Ported from Goobstation content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Client/_Goobstation/Vehicles/VehicleSystem.cs
// WW Change - reads Wizden's vehicle appearance (CanRun, HasOperator) instead of Goob's vehicle state.

using Content.Shared._Goobstation.Vehicles;
using Content.Shared.Vehicle.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using DrawDepthTag = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Goobstation.Vehicles;

/// <summary>
/// The engine layer animates only while the vehicle can run, the vehicle draws over its rider when facing one of its
/// render-over directions, and a burning motorbike flashes red.
/// </summary>
public sealed partial class VehicleDrawOverSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleDrawOverComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<VehicleDrawOverComponent, MoveEvent>(OnMove);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var burning = EntityQueryEnumerator<MotorbikeComponent, SpriteComponent>();
        while (burning.MoveNext(out var uid, out var motorbike, out var sprite))
        {
            if (!motorbike.Burning)
                continue;

            var flashRed = (int) (_timing.CurTime.TotalSeconds * 5) % 2 == 0;
            _sprite.LayerSetColor((uid, sprite), 0, flashRed ? Color.Red : Color.White);
        }
    }

    private void OnAppearanceChange(Entity<VehicleDrawOverComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        _appearance.TryGetData(ent, VehicleVisuals.CanRun, out bool canRun, args.Component);
        if (_sprite.LayerMapTryGet((ent, sprite), VehicleVisualLayers.AutoAnimate, out var layer, false))
            _sprite.LayerSetAutoAnimated((ent, sprite), layer, canRun);

        UpdateDrawDepth(ent, sprite);
    }

    private void OnMove(Entity<VehicleDrawOverComponent> ent, ref MoveEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            UpdateDrawDepth(ent, sprite);
    }

    private void UpdateDrawDepth(Entity<VehicleDrawOverComponent> ent, SpriteComponent sprite)
    {
        var depth = DrawDepthTag.Objects;
        if (ent.Comp.RenderOver != VehicleRenderOver.None &&
            _appearance.TryGetData(ent, VehicleVisuals.HasOperator, out bool ridden) && ridden)
        {
            var dir = (Transform(ent).LocalRotation + _eye.CurrentEye.Rotation).GetCardinalDir();
            var flag = dir switch
            {
                Direction.North => VehicleRenderOver.North,
                Direction.NorthEast => VehicleRenderOver.NorthEast,
                Direction.East => VehicleRenderOver.East,
                Direction.SouthEast => VehicleRenderOver.SouthEast,
                Direction.South => VehicleRenderOver.South,
                Direction.SouthWest => VehicleRenderOver.SouthWest,
                Direction.West => VehicleRenderOver.West,
                Direction.NorthWest => VehicleRenderOver.NorthWest,
                _ => VehicleRenderOver.None,
            };

            if ((ent.Comp.RenderOver & flag) == flag)
                depth = DrawDepthTag.OverMobs;
        }

        _sprite.SetDrawDepth((ent, sprite), (int) depth);
    }
}
