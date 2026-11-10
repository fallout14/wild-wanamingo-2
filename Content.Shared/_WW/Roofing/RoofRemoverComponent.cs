using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._WW.Roofing;

/// <summary>
/// A tool that takes the roof off a tile: use it on a roofed floor tile. Roofs are built from the construction menu
/// (1 wood plank). No wood comes back, so tearing down map roofs can't be farmed for planks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RoofRemoverComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);

    [DataField]
    public ProtoId<ToolQualityPrototype> Quality = "Sawing";
}

[Serializable, NetSerializable]
public sealed partial class RemoveRoofDoAfterEvent : DoAfterEvent
{
    public NetEntity Grid;
    public Vector2i Tile;

    public RemoveRoofDoAfterEvent(NetEntity grid, Vector2i tile)
    {
        Grid = grid;
        Tile = tile;
    }

    public override DoAfterEvent Clone() => this;

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is RemoveRoofDoAfterEvent roof && roof.Grid == Grid && roof.Tile == Tile;
    }
}
