// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/VaultDoorConsole/SharedVaultDoorConsoleSystem.cs

using Robust.Shared.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.UserInterface;

namespace Content.Shared._Misfits.VaultDoorConsole;

public sealed partial class SharedVaultDoorConsoleSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VaultDoorConsoleGateComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<VaultDoorConsoleGateComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasPipBoy(args.User))
        {
            _popup.PopupClient("You need a Pip-Boy to interface with this terminal.", ent, args.User);
            args.Cancel();
            return;
        }

        if (!ent.Comp.BypassRaidRequirement && !ent.Comp.RaidActive)
        {
            _popup.PopupClient(ent.Comp.LockoutMessage, ent, args.User);
            args.Cancel();
        }
    }

    private static readonly ProtoId<TagPrototype> PipBoyTag = "MisfitsPipBoy";

    private bool HasPipBoy(EntityUid user)
    {
        return _inventory.TryGetSlotEntity(user, "id", out var idUid) &&
               _tag.HasTag(idUid.Value, PipBoyTag);
    }
}
