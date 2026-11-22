using Content.Client._Misfits.Overlays.Legacy;
using Content.Client.NightVision;
using Content.Shared._Misfits.Overlays.Legacy;
using Content.Shared._WW.Weapons.Scopes;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.NightVision;
using Content.Shared.Overlays;
using Robust.Client.Player;

namespace Content.Client._WW.Weapons.Scopes;

/// <summary>
/// Night vision and thermal rifle scopes: while you are scoped in through one (gun wielded, scope action used), its
/// <see cref="NightVisionComponent"/> or <see cref="ThermalVisionComponent"/> is added to your view, the same way
/// worn goggles add theirs. Scoping out, lowering the gun or dropping it takes it away again.
/// </summary>
public sealed partial class WWScopeVisionSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private WWScopeSystem _scope = default!;
    [Dependency] private NightVisionSystem _nightVision = default!;
    [Dependency] private ThermalVisionSystem _thermal = default!;

    // What the local player was looking through last frame, so the overlays are only refreshed on a change.
    private EntityUid? _lastViewer;
    private EntityUid? _lastNightVision;
    private EntityUid? _lastThermal;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandsComponent, RefreshNightVisionEvent>(OnRefreshNightVision);
        SubscribeLocalEvent<HandsComponent, RefreshEquipmentHudEvent<ThermalVisionComponent>>(OnRefreshThermal);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var viewer = _player.LocalEntity;
        EntityUid? nightVision = null;
        EntityUid? thermal = null;
        if (viewer != null)
        {
            nightVision = GetActiveScope<NightVisionComponent>(viewer.Value);
            thermal = GetActiveScope<ThermalVisionComponent>(viewer.Value);
        }

        if (viewer == _lastViewer && nightVision == _lastNightVision && thermal == _lastThermal)
            return;

        var nightVisionChanged = nightVision != _lastNightVision || viewer != _lastViewer;
        var thermalChanged = thermal != _lastThermal || viewer != _lastViewer;
        _lastViewer = viewer;
        _lastNightVision = nightVision;
        _lastThermal = thermal;

        if (viewer == null)
            return;
        if (nightVisionChanged)
            _nightVision.RefreshViewer(viewer.Value);
        if (thermalChanged)
            _thermal.Refresh();
    }

    /// <summary>The scope with a <typeparamref name="T"/> that this player is scoped in through, if any.</summary>
    private EntityUid? GetActiveScope<T>(EntityUid user) where T : IComponent
    {
        if (!TryComp<HandsComponent>(user, out var hands))
            return null;

        foreach (var held in _hands.EnumerateHeld((user, hands)))
        {
            if (_scope.TryGetActiveScope(held, out var scope) && HasComp<T>(scope.Value))
                return scope.Value.Owner;
        }

        return null;
    }

    private void OnRefreshNightVision(Entity<HandsComponent> ent, ref RefreshNightVisionEvent args)
    {
        if (GetActiveScope<NightVisionComponent>(ent) is { } scope &&
            TryComp<NightVisionComponent>(scope, out var nightVision) && nightVision.Enabled)
        {
            args.Entities.Add((scope, nightVision));
        }
    }

    private void OnRefreshThermal(Entity<HandsComponent> ent, ref RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        if (GetActiveScope<ThermalVisionComponent>(ent) is not { } scope ||
            !TryComp<ThermalVisionComponent>(scope, out var thermal))
        {
            return;
        }

        args.Active = true;
        args.Components.Add(thermal);
    }
}
