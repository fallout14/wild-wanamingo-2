using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._WW.Weapons.Ranged;
using Robust.Client.GameObjects;

namespace Content.Client._WW.Weapons.Ranged;

/// <summary>
/// WW: shows a folded stock. Runs after the gun's own visuals have set the base (bolt) and magazine states, then
/// swaps each for its folded version ("base" -> "folded-base", "mag-0" -> "folded-mag-0") where the RSI has one,
/// and back again when the stock is extended. A gun without folded art just keeps its normal sprite.
/// </summary>
public sealed partial class FoldingStockVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly Enum[] Layers =
    {
        GunVisualLayers.Base,
        GunVisualLayers.BaseUnshaded,
        GunVisualLayers.Mag,
        GunVisualLayers.MagUnshaded,
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoldingStockComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(GunSystem) });
    }

    private void OnAppearanceChange(Entity<FoldingStockComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        var folded = args.AppearanceData.TryGetValue(FoldingStockVisuals.Folded, out var value) && value is true;
        var prefix = ent.Comp.FoldedStatePrefix;

        foreach (var key in Layers)
        {
            if (!_sprite.LayerMapTryGet((ent, sprite), key, out var layer, false))
                continue;

            var state = _sprite.LayerGetRsiState((ent, sprite), layer).Name;
            if (state == null)
                continue;

            var isFoldedState = state.StartsWith(prefix, StringComparison.Ordinal);
            string wanted;
            if (folded && !isFoldedState)
                wanted = prefix + state;
            else if (!folded && isFoldedState)
                wanted = state[prefix.Length..];
            else
                continue;

            if (_sprite.LayerGetEffectiveRsi((ent, sprite), layer) is { } rsi && rsi.TryGetState(wanted, out _))
                _sprite.LayerSetRsiState((ent, sprite), layer, wanted);
        }
    }
}
