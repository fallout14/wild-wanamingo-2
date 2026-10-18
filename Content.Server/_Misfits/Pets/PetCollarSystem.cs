// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/Pets/PetCollarSystem.cs
// WW: the default-collar helper (perk pets) and the ID console rename hook (a Misfits event) aren't ported.

using Content.Server.Administration;
using Content.Shared._Misfits.Pets;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server._Misfits.Pets;

/// <summary>
/// A pet's collar is an ID card in its id slot: the pet takes the collar's name, and anyone can rename it.
/// </summary>
public sealed partial class PetCollarSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private SharedIdCardSystem _idCards = default!;

    private const string Slot = "id";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PetCollarHolderComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PetCollarHolderComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<PetCollarHolderComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
    }

    private void OnGetVerbs(Entity<PetCollarHolderComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_inventory.TryGetSlotEntity(ent, Slot, out var collar) || !HasComp<IdCardComponent>(collar))
            return;

        var user = args.User;
        var title = Loc.GetString("pet-collar-rename-verb");
        args.Verbs.Add(new Verb
        {
            Text = title,
            Act = () =>
            {
                _quickDialog.OpenDialog(actor.PlayerSession, title, Loc.GetString("pet-collar-rename-prompt"), (string name) =>
                {
                    // The collar may have been swapped while the dialog was open.
                    if (!Exists(ent) || !_inventory.TryGetSlotEntity(ent, Slot, out var current) || current != collar)
                        return;

                    if (_idCards.TryChangeFullName(collar.Value, name, player: user))
                        RefreshPetName(ent);
                });
            },
        });
    }

    private void OnSlotChanged(EntityUid uid, PetCollarHolderComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == Slot)
            RefreshPetName(uid);
    }

    private void RefreshPetName(EntityUid pet)
    {
        if (_inventory.TryGetSlotEntity(pet, Slot, out var collar) &&
            TryComp<IdCardComponent>(collar, out var card) &&
            !string.IsNullOrWhiteSpace(card.FullName))
        {
            _meta.SetEntityName(pet, card.FullName);
            return;
        }

        if (MetaData(pet).EntityPrototype is { } proto)
            _meta.SetEntityName(pet, proto.Name);
    }
}
