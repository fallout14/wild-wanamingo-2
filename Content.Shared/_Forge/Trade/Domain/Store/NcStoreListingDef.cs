// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Trade/Domain/Store/NcStoreListingDef.cs

namespace Content.Shared._Forge.Trade;

public sealed class NcStoreListingDef
{
    public string Id = string.Empty;

    public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;
    public StoreMode Mode = StoreMode.Buy;

    public string ProductEntity = string.Empty;

    public Dictionary<string, int> Cost { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public List<ListingConditionPrototype> Conditions { get; set; } = new();

    public int UnitsPerPurchase { get; set; } = 1;

    public int RemainingCount { get; set; } = -1;
}
