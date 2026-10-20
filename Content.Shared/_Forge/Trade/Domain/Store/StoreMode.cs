// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Trade/Domain/Store/StoreMode.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Trade;

[Serializable, NetSerializable]
public enum StoreMode
{
    Buy,
    Sell,
    Exchange
}
