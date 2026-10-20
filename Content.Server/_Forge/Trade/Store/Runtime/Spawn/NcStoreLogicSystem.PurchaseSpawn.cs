// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_NC/Trade/Store/Runtime/Spawn/NcStoreLogicSystem.PurchaseSpawn.cs

using Robust.Shared.Prototypes;

namespace Content.Server._Forge.Trade;

public sealed partial class NcStoreLogicSystem
{

    private int SpawnPurchasedProduct(
        EntityUid user,
        string productEntity,
        EntityPrototype productProto,
        int amount,
        int unitPrice,
        string currency
    ) =>
        _spawnService.SpawnPurchasedProduct(user, productEntity, productProto, amount, unitPrice, currency);
}
