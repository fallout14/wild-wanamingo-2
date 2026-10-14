// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/SpecialStats/LuckJunkBonusComponent.cs
// Changed for Wild Wanamingo: items not ported yet are left out of the default list (a holotape, four printed
// magazines and the wastelander Pip-Boy).

using Robust.Shared.Prototypes;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Marks a storage entity as eligible for the Luck S.P.E.C.I.A.L. bonus.
/// </summary>
[RegisterComponent]
public sealed partial class LuckJunkBonusComponent : Component
{
    [DataField]
    public List<LuckyLootEntry> LuckyItems = new()
    {
        new("N14CurrencyCap", LuckyLootRarity.Common),
        new("N14CurrencyCap10", LuckyLootRarity.Common),
        new("N14Stimpak", LuckyLootRarity.Common),
        new("N14MagazinePistol22lr", LuckyLootRarity.Common),
        new("N14MagazinePistol9mm", LuckyLootRarity.Common),
        new("N14MagazinePistol10mm", LuckyLootRarity.Common),
        new("N14MagazineBox22", LuckyLootRarity.Common),
        new("N14MagazineBox9mm", LuckyLootRarity.Common),
        new("N14MagazineBox10mm", LuckyLootRarity.Common),
        new("N14MagazineBox20gauge", LuckyLootRarity.Common),
        new("N14MagazineShotgun20", LuckyLootRarity.Common),
        new("N14SpeedLoader9", LuckyLootRarity.Common),
        new("N14SpeedLoader10", LuckyLootRarity.Common),

        new("N14CurrencyCap50", LuckyLootRarity.Uncommon),
        new("N14RadXPill", LuckyLootRarity.Uncommon),
        new("N14Jet", LuckyLootRarity.Uncommon),
        new("N14Turbo", LuckyLootRarity.Uncommon),
        new("N14RadAwayBloodbag", LuckyLootRarity.Uncommon),
        new("N14JunkToyNukaTruck", LuckyLootRarity.Uncommon),
        new("N14DrinkNukaColaQuartz", LuckyLootRarity.Uncommon),
        new("N14DrinkNukaColaVictory", LuckyLootRarity.Uncommon),
        new("MisfitsMagazineAmerican180Drum", LuckyLootRarity.Uncommon),
        new("N14MagazineSMG9mm", LuckyLootRarity.Uncommon),
        new("N14MagazineSMG10mm", LuckyLootRarity.Uncommon),
        new("N14Magazine45SubMachineGun", LuckyLootRarity.Uncommon),
        new("N14MagazinePistol12mm", LuckyLootRarity.Uncommon),
        new("N14MagazineSMG12mm", LuckyLootRarity.Uncommon),
        new("N14MagazineShotgun12", LuckyLootRarity.Uncommon),
        new("N14MagazineBox12gauge", LuckyLootRarity.Uncommon),
        new("N14MagazineBox12", LuckyLootRarity.Uncommon),
        new("N14MagazineBox44", LuckyLootRarity.Uncommon),
        new("N14MagazineBox45", LuckyLootRarity.Uncommon),
        new("N14MagazinePistol45", LuckyLootRarity.Uncommon),
        new("N14MagazinePistol44", LuckyLootRarity.Uncommon),
        new("N14MagazineBox556", LuckyLootRarity.Uncommon),
        new("N14Magazine556Rifle", LuckyLootRarity.Uncommon),
        new("N14LongMagazine556Rifle", LuckyLootRarity.Uncommon),
        new("N14MagazineBox762", LuckyLootRarity.Uncommon),
        new("N14Magazine762Rifle", LuckyLootRarity.Uncommon),
        new("N14Magazine762AmmoShort", LuckyLootRarity.Uncommon),
        new("N14SpeedLoader44", LuckyLootRarity.Uncommon),

        new("N14DrinkNukaColaQuantum", LuckyLootRarity.Rare),
        new("N14HandheldHealthAnalyzer", LuckyLootRarity.Rare),
        new("MisfitsMagazineSMG9mmDrum", LuckyLootRarity.Rare),
        new("N14LMGMagazine556Rifle", LuckyLootRarity.Rare),
        new("N14Magazine762AmmoBelt", LuckyLootRarity.Rare),
        new("N14MagazineBox308", LuckyLootRarity.Rare),
        new("N14Magazine308Rifle", LuckyLootRarity.Rare),
        new("N14Magazine308RifleLong", LuckyLootRarity.Rare),
        new("N14ClipMagazine308Rifle", LuckyLootRarity.Rare),
        new("MisfitsMagazine308RifleSniper", LuckyLootRarity.Rare),
        new("N14MagazineBox45-70", LuckyLootRarity.Rare),
        new("N14SpeedLoader45-70", LuckyLootRarity.Rare),
        new("N14SpeedLoader45-70Tube", LuckyLootRarity.Rare),
        new("N14MagazineBox50", LuckyLootRarity.Rare),
        new("N14Magazine50AMR", LuckyLootRarity.Rare),
        new("N14MagazineMinigun5mm", LuckyLootRarity.Rare),
        new("N1440mmGrenadeFrag", LuckyLootRarity.Rare),
        new("N1440mmGrenadeFire", LuckyLootRarity.Rare),
        new("N14PowerCellSmall", LuckyLootRarity.Rare),
        new("MisfitsMicrofusionCell", LuckyLootRarity.Rare),
        new("MisfitsElectronChargePack", LuckyLootRarity.Rare),

        new("N14SuperStimpak", LuckyLootRarity.VeryRare),
        new("N14PlasmaCartridge", LuckyLootRarity.VeryRare),
        new("N14FusionCore", LuckyLootRarity.VeryRare),
        new("MisfitsPlasmaCartridgeMultiplas", LuckyLootRarity.VeryRare),
        new("N14PlasmaShell", LuckyLootRarity.VeryRare),

    };

    /// <summary>
    /// Additional roll-success probability per Luck point above the default of 5.
    /// </summary>
    [DataField]
    public float ChancePerLuckPoint = 0.06666667f;
}

public enum LuckyLootRarity : byte
{
    Common,
    Uncommon,
    Rare,
    VeryRare,
    Legendary,
}

[DataDefinition]
public sealed partial class LuckyLootEntry
{
    public LuckyLootEntry()
    {
    }

    public LuckyLootEntry(EntProtoId id, LuckyLootRarity rarity)
    {
        Id = id;
        Rarity = rarity;
    }

    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public LuckyLootRarity Rarity = LuckyLootRarity.Common;
}
