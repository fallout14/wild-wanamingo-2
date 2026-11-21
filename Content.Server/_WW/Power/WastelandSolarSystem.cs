using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._WW.Power;

/// <summary>
/// Sets each <see cref="WastelandSolarPanelComponent"/>'s output from the sun on its map.
/// </summary>
public sealed partial class WastelandSolarSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private Content.Shared.GameTicking.GameTicker _ticker = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedRoofSystem _roofs = default!;

    private const float UpdateInterval = 2f;
    private float _accumulator = UpdateInterval;

    public override void Initialize()
    {
        SubscribeLocalEvent<WastelandSolarPanelComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<WastelandSolarPanelComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("wasteland-solar-examine", ("percent", (int) MathF.Round(ent.Comp.Sunlight * 100f))));
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        _accumulator = 0f;

        var query = EntityQueryEnumerator<WastelandSolarPanelComponent, PowerSupplierComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var panel, out var supplier, out var xform))
        {
            panel.Sunlight = xform.Anchored ? GetSunlight(xform) : 0f;
            supplier.MaxSupply = panel.MaxSupply * panel.Sunlight;
        }
    }

    /// <summary>
    /// 0 to 1: how much sun reaches this spot right now.
    /// </summary>
    public float GetSunlight(TransformComponent xform)
    {
        if (xform.GridUid is { } gridUid
            && TryComp(gridUid, out MapGridComponent? grid)
            && TryComp(gridUid, out RoofComponent? roof)
            && _roofs.IsRooved((gridUid, grid, roof), _maps.LocalToTile(gridUid, grid, xform.Coordinates)))
            return 0f;

        if (xform.MapUid is not { } mapUid || !TryComp(mapUid, out LightCycleComponent? cycle) || !cycle.Enabled)
            return 1f;

        // Same clock as the client's LightCycleSystem.
        var time = (float) _timing.CurTime
            .Add(cycle.Offset)
            .Subtract(_ticker.RoundStartTimeSpan)
            .Subtract(_metaData.GetPauseTime(mapUid))
            .TotalSeconds;

        return Daylight(cycle, time);
    }

    /// <summary>
    /// The light cycle's sun curve without its night floor (<see cref="LightCycleComponent.MinLightLevel"/>, moonlight),
    /// scaled so the stretch of the day that looks fully lit gives full output.
    /// </summary>
    public static float Daylight(LightCycleComponent cycle, float time)
    {
        var waveLength = MathF.Max(1f, (float) cycle.Duration.TotalSeconds);
        var sun = SharedLightCycleSystem.CalculateCurve(time, waveLength, 1f, 0f, 6);
        var clip = MathF.Max(0.01f, cycle.ClipLight);
        return Math.Clamp(sun * MathF.Max(0f, cycle.MaxLightLevel) / clip, 0f, 1f);
    }
}
