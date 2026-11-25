using Robust.Shared.GameStates;

namespace Content.Shared._WW.Species;

/// <summary>
/// WW: tints sprite layers with the character's skin colour. For species drawn as one plain sprite (robots) instead of
/// Wizden body-part layers, which is where skin colour normally goes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SkinTintedSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// Sprite layer map keys to tint. Empty tints the whole sprite.
    /// </summary>
    [DataField]
    public List<string> Layers = new();
}
