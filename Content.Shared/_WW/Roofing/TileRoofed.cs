using Content.Shared.Construction;
using Content.Shared.Construction.Conditions;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._WW.Roofing;

/// <summary>
/// Construction condition on a tile's roof: the grid must track roofs (has <see cref="RoofComponent"/>, like Yuma)
/// and the tile must be roofed or not roofed as set. Walls count as roofed.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class TileRoofed : IConstructionCondition
{
    /// <summary>
    /// True: the tile must already have a roof. False: it must be open to the sky.
    /// </summary>
    [DataField]
    public bool Roofed;

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var xforms = entMan.System<SharedTransformSystem>();
        var maps = entMan.System<SharedMapSystem>();

        if (xforms.GetGrid(location) is not { } gridUid
            || !entMan.TryGetComponent(gridUid, out MapGridComponent? grid)
            || !entMan.TryGetComponent(gridUid, out RoofComponent? roof))
            return false;

        var index = maps.LocalToTile(gridUid, grid, location);
        return entMan.System<SharedRoofSystem>().IsRooved((gridUid, grid, roof), index) == Roofed;
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = Roofed
                ? "construction-step-condition-tile-roofed"
                : "construction-step-condition-tile-not-roofed",
        };
    }
}
