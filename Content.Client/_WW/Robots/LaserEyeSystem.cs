using Content.Shared._WW.Robots;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._WW.Robots;

/// <summary>
/// WW: while the local player's laser eye beam fires, keep telling the server where the cursor is so the beam
/// follows it (no need to hold the fire button).
/// </summary>
public sealed partial class LaserEyeSystem : SharedLaserEyeSystem
{
    private static readonly TimeSpan AimInterval = TimeSpan.FromSeconds(0.05);

    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private TimeSpan _nextAim;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } user || _timing.RealTime < _nextAim)
            return;

        if (_hands.GetActiveItem(user) is not { } gun
            || !TryComp<LaserEyeComponent>(gun, out var eye)
            || eye.State != LaserEyeState.Firing)
        {
            return;
        }

        var mouse = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mouse.MapId == MapId.Nullspace)
            return;

        _nextAim = _timing.RealTime + AimInterval;
        RaiseNetworkEvent(new LaserEyeAimEvent
        {
            Gun = GetNetEntity(gun),
            Coordinates = GetNetCoordinates(_transform.ToCoordinates(user, mouse)),
        });
    }
}
