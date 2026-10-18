// Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) flatpack extras, moved out of their edit to Wizden's
// FlatpackComponent (ExtraEntities) and SharedFlatpackSystem.

using Content.Server.Construction;
using Content.Shared._Misfits.Construction;
using Content.Shared.Interaction;

namespace Content.Server._Misfits.Construction;

public sealed partial class FlatpackExtrasSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlatpackExtrasComponent, InteractUsingEvent>(OnInteractUsing, after: [typeof(FlatpackSystem)]);
    }

    private void OnInteractUsing(Entity<FlatpackExtrasComponent> ent, ref InteractUsingEvent args)
    {
        // Wizden's flatpack handles the interaction and deletes itself once it has unpacked.
        if (!args.Handled || !EntityManager.IsQueuedForDeletion(ent))
            return;

        var coords = Transform(ent).Coordinates;
        foreach (var extra in ent.Comp.Entities)
        {
            Spawn(extra, coords);
        }
    }
}
