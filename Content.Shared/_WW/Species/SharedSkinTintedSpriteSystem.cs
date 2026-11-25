using Content.Shared.Body;

namespace Content.Shared._WW.Species;

/// <summary>
/// WW: takes the skin colour from the character profile when it's applied (spawn and the lobby preview).
/// </summary>
public abstract partial class SharedSkinTintedSpriteSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SkinTintedSpriteComponent, ApplyOrganProfileDataEvent>(OnApplyProfile);
    }

    private void OnApplyProfile(Entity<SkinTintedSpriteComponent> ent, ref ApplyOrganProfileDataEvent args)
    {
        if (args.Base is { } profile)
            SetColor(ent.AsNullable(), profile.SkinColor);
    }

    public void SetColor(Entity<SkinTintedSpriteComponent?> ent, Color color)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Color == color)
            return;

        ent.Comp.Color = color;
        Dirty(ent);
        UpdateSprite((ent, ent.Comp));
    }

    protected virtual void UpdateSprite(Entity<SkinTintedSpriteComponent> ent)
    {
    }
}
