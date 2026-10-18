// Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) flatpack extras, moved out of their edit to Wizden's
// FlatpackComponent (ExtraEntities) and SharedFlatpackSystem.

using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Construction;

/// <summary>
/// Extra items a flatpack drops next to what it unpacks, e.g. a motorbike's keys.
/// </summary>
[RegisterComponent]
public sealed partial class FlatpackExtrasComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Entities = new();
}
