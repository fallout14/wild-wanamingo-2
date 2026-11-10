using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._WW.Roofing;

/// <summary>
/// Lets a <see cref="RoofRemoverComponent"/> tool take a player-buildable roof off a tile.
/// Only the grid's roof flags are touched; walls (which count as roof) are left alone.
/// </summary>
public sealed partial class RoofRemoverSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRoofSystem _roof = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoofRemoverComponent, AfterInteractEvent>(OnAfterInteract);
        // Tool use without a target entity raises the finished do-after as a broadcast event.
        SubscribeLocalEvent<RemoveRoofDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<RoofRemoverComponent> ent, ref AfterInteractEvent args)
    {
        // Only a click on bare floor, so the saw still works normally on things.
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (!_maps.TryFindGridAt(_xform.ToMapCoordinates(args.ClickLocation), out var gridUid, out var grid)
            || !TryComp<RoofComponent>(gridUid, out var roof))
            return;

        var tile = _maps.LocalToTile(gridUid, grid, args.ClickLocation);
        if (!HasRoofFlag(roof, tile))
            return;

        var coords = _maps.GridTileToLocal(gridUid, grid, tile);
        if (!_interaction.InRangeUnobstructed(args.User, coords, popup: false))
            return;

        _popup.PopupClient(Loc.GetString("roof-remover-start"), args.User, args.User);
        args.Handled = _tool.UseTool(ent,
            args.User,
            null,
            ent.Comp.Delay,
            [ent.Comp.Quality],
            new RemoveRoofDoAfterEvent(GetNetEntity(gridUid), tile),
            out _);
    }

    private void OnDoAfter(RemoveRoofDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !HasComp<RoofRemoverComponent>(args.Used))
            return;

        var gridUid = GetEntity(args.Grid);
        if (!TryComp<MapGridComponent>(gridUid, out var grid) || !TryComp<RoofComponent>(gridUid, out var roof))
            return;

        args.Handled = true;
        _roof.SetRoof((gridUid, grid, roof), args.Tile, false);
        _adminLog.Add(LogType.Construction,
            LogImpact.Low,
            $"{ToPrettyString(args.User):player} removed the roof at {args.Tile} on {ToPrettyString(gridUid)} with {ToPrettyString(args.Used)}");
    }

    /// <summary>
    /// Whether the tile has the grid roof flag (built or mapped roof), ignoring walls.
    /// </summary>
    public static bool HasRoofFlag(RoofComponent roof, Vector2i tile)
    {
        var chunk = SharedMapSystem.GetChunkIndices(tile, RoofComponent.ChunkSize);
        if (!roof.Data.TryGetValue(chunk, out var mask))
            return false;

        var rel = SharedMapSystem.GetChunkRelative(tile, RoofComponent.ChunkSize);
        var bit = 1UL << (rel.X + rel.Y * RoofComponent.ChunkSize);
        return (mask & bit) != 0;
    }
}
