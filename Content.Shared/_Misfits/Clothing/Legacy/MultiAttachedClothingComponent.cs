// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/Clothing/Components/AttachedClothingComponent.cs
// (Misfits' copy of a Goobstation/Space Station 14 file.) Renamed Multi* so it lives next to Wizden's
// single-piece ToggleableClothing: power armor deploys several pieces from one suit.

using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared._Misfits.Clothing.Legacy;

/// <summary>
///     This component indicates that this clothing is attached to some other entity with a <see
///     cref="MultiToggleableClothingComponent"/>. When unequipped, this entity should be returned to the entity that it is
///     attached to, rather than being dumped on the floor or something like that. Intended for use with hardsuits and
///     hardsuit helmets.
/// </summary>
[Access(typeof(MultiToggleableClothingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiAttachedClothingComponent : Component
{
    // Goobstation - Modsuits changes this system entirely
    public const string DefaultClothingContainerId = "replaced-clothing";

    /// <summary>
    ///     The Id of the piece of clothing that this entity belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedUid;

    /// <summary>
    ///     Container ID for clothing that will be replaced with this one
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ClothingContainerId = DefaultClothingContainerId;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ClothingContainer;
}
