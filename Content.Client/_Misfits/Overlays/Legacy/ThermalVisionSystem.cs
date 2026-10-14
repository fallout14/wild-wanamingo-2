// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/Overlays/Switchable/ThermalVisionSystem.cs
// (Misfits' copy of Goobstation's switchable overlays.)
// WW: no full-screen tint pass (Wizden's night vision shader is different); power armor helmets also carry Wizden's
// night vision for that.

using Content.Client.Overlays;
using Content.Shared._Misfits.Overlays.Legacy;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._Misfits.Overlays.Legacy;

public sealed partial class ThermalVisionSystem : EquipmentHudSystem<ThermalVisionComponent>
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ThermalVisionOverlay _thermalOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, SwitchableOverlayToggledEvent>(OnToggle);

        _thermalOverlay = new ThermalVisionOverlay();
    }

    protected override void OnRefreshComponentHud(Entity<ThermalVisionComponent> ent,
        ref RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        if (ent.Comp.IsEquipment)
            return;

        base.OnRefreshComponentHud(ent, ref args);
    }

    protected override void OnRefreshEquipmentHud(Entity<ThermalVisionComponent> ent,
        ref InventoryRelayedEvent<RefreshEquipmentHudEvent<ThermalVisionComponent>> args)
    {
        if (!ent.Comp.IsEquipment)
            return;

        base.OnRefreshEquipmentHud(ent, ref args);
    }

    private void OnToggle(Entity<ThermalVisionComponent> ent, ref SwitchableOverlayToggledEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ThermalVisionComponent> args)
    {
        base.UpdateInternal(args);
        ThermalVisionComponent? tvComp = null;
        var lightRadius = 0f;
        foreach (var comp in args.Components)
        {
            if (!comp.IsActive && (comp.PulseTime <= 0f || comp.PulseAccumulator >= comp.PulseTime))
                continue;

            // A steady source wins over a pulse.
            if (tvComp == null || tvComp.PulseTime > 0f && comp.PulseTime <= 0f)
                tvComp = comp;

            lightRadius = MathF.Max(lightRadius, comp.LightRadius);
        }

        UpdateThermalOverlay(tvComp, lightRadius);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _thermalOverlay.ResetLight(false);
        UpdateThermalOverlay(null, 0f);
    }

    private void UpdateThermalOverlay(ThermalVisionComponent? comp, float lightRadius)
    {
        _thermalOverlay.LightRadius = lightRadius;
        _thermalOverlay.Comp = comp;

        switch (comp)
        {
            case not null when !_overlayMan.HasOverlay<ThermalVisionOverlay>():
                _overlayMan.AddOverlay(_thermalOverlay);
                break;
            case null:
                _overlayMan.RemoveOverlay(_thermalOverlay);
                _thermalOverlay.ResetLight();
                break;
        }
    }
}
