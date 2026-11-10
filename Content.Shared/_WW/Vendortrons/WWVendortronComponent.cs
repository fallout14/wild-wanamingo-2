using Robust.Shared.GameStates;

namespace Content.Shared._WW.Vendortrons;

/// <summary>
/// A vendortron: a robot shopkeeper (MS13) that sells through the trade store UI. It can't be dragged around and
/// says one of its lines when someone opens its store.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WWVendortronComponent : Component
{
    /// <summary>
    /// Lines it says when someone opens its store; one is picked at random.
    /// </summary>
    [DataField]
    public List<LocId> Lines = new();

    /// <summary>
    /// Minimum time between two lines, so it doesn't spam chat.
    /// </summary>
    [DataField]
    public TimeSpan LineCooldown = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public TimeSpan NextLine;
}
