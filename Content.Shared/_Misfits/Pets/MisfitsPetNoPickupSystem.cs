// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Pets/MisfitsPetNoPickupSystem.cs

using Content.Shared.Item;

namespace Content.Shared._Misfits.Pets;

/// <summary>
/// Prevents pet mobs with <see cref="MisfitsPetNoPickupComponent"/> from picking up items.
/// They keep their hands (for dragging/interaction) but cannot hold items.
/// </summary>
public sealed partial class MisfitsPetNoPickupSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MisfitsPetNoPickupComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, MisfitsPetNoPickupComponent component, PickupAttemptEvent args)
    {
        args.Cancel();
    }
}
