// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/DeltaV/CartridgeLoader/Cartridges/NanoChatCartridgeComponent.cs (Delta-V code carried by Misfits)

using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._DeltaV.CartridgeLoader.Cartridges;

[RegisterComponent, Access(typeof(NanoChatCartridgeSystem))]
public sealed partial class NanoChatCartridgeComponent : Component
{
    /// <summary>
    ///     Station entity to keep track of.
    /// </summary>
    [DataField]
    public EntityUid? Station;

    /// <summary>
    ///     The NanoChat card to keep track of.
    /// </summary>
    [DataField]
    public EntityUid? Card;

    /// <summary>
    ///     The <see cref="RadioChannelPrototype" /> required to send or receive messages.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Common";
}
