// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/_NC/Trade/NcStoreMenu.ListingsDiff.cs

using Content.Shared._Forge.Trade;


namespace Content.Client._Forge.Trade;


public sealed partial class NcStoreMenu
{
    private void RefreshListingsDynamicOnly()
    {
        if (_disposed)
            return;

        BuyView.UpdateDynamicOnly(GetBalanceForCurrency);
        SellView.UpdateDynamicOnly(static _ => int.MaxValue);
    }
}
