using Content.Shared._WW.Species;
using Robust.Client.GameObjects;

namespace Content.Client._WW.Species;

public sealed partial class SkinTintedSpriteSystem : SharedSkinTintedSpriteSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SkinTintedSpriteComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SkinTintedSpriteComponent, AfterAutoHandleStateEvent>(OnAfterState);
    }

    private void OnStartup(Entity<SkinTintedSpriteComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    private void OnAfterState(Entity<SkinTintedSpriteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite(ent);
    }

    protected override void UpdateSprite(Entity<SkinTintedSpriteComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.Layers.Count == 0)
        {
            _sprite.SetColor((ent, sprite), ent.Comp.Color);
            return;
        }

        foreach (var key in ent.Comp.Layers)
        {
            if (_sprite.LayerMapTryGet((ent, sprite), key, out var layer, false))
                _sprite.LayerSetColor((ent, sprite), layer, ent.Comp.Color);
        }
    }
}
