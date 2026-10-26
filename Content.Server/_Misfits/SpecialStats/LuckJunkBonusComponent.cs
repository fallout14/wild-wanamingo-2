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
        new("F14CurrencyCap", LuckyLootRarity.Common),
        new("F14CurrencyCap10", LuckyLootRarity.Common),
        new("F14Stimpak", LuckyLootRarity.Common),
        new("F14MagazinePistol22lr", LuckyLootRarity.Common),
        new("F14MagazinePistol9mm", LuckyLootRarity.Common),
        new("F14MagazinePistol10mm", LuckyLootRarity.Common),
        new("F14MagazineBox22", LuckyLootRarity.Common),
        new("F14MagazineBox9mm", LuckyLootRarity.Common),
        new("F14MagazineBox10mm", LuckyLootRarity.Common),
        new("F14MagazineBox20gauge", LuckyLootRarity.Common),
        new("F14MagazineShotgun20", LuckyLootRarity.Common),
        new("F14SpeedLoader9", LuckyLootRarity.Common),
        new("F14SpeedLoader10", LuckyLootRarity.Common),

        new("F14CurrencyCap50", LuckyLootRarity.Uncommon),
        new("F14RadXPill", LuckyLootRarity.Uncommon),
        new("F14Jet", LuckyLootRarity.Uncommon),
        new("F14Turbo", LuckyLootRarity.Uncommon),
        new("F14RadAwayBloodbag", LuckyLootRarity.Uncommon),
        new("F14JunkToyNukaTruck", LuckyLootRarity.Uncommon),
        new("F14DrinkNukaColaQuartz", LuckyLootRarity.Uncommon),
        new("F14DrinkNukaColaVictory", LuckyLootRarity.Uncommon),
        new("F14MagazineAmerican180Drum", LuckyLootRarity.Uncommon),
        new("F14MagazineSMG9mm", LuckyLootRarity.Uncommon),
        new("F14MagazineSMG10mm", LuckyLootRarity.Uncommon),
        new("F14Magazine45SubMachineGun", LuckyLootRarity.Uncommon),
        new("F14MagazinePistol12mm", LuckyLootRarity.Uncommon),
        new("F14MagazineSMG12mm", LuckyLootRarity.Uncommon),
        new("F14MagazineShotgun12", LuckyLootRarity.Uncommon),
        new("F14MagazineBox12gauge", LuckyLootRarity.Uncommon),
        new("F14MagazineBox12", LuckyLootRarity.Uncommon),
        new("F14MagazineBox44", LuckyLootRarity.Uncommon),
        new("F14MagazineBox45", LuckyLootRarity.Uncommon),
        new("F14MagazinePistol45", LuckyLootRarity.Uncommon),
        new("F14MagazinePistol44", LuckyLootRarity.Uncommon),
        new("F14MagazineBox556", LuckyLootRarity.Uncommon),
        new("F14Magazine556Rifle", LuckyLootRarity.Uncommon),
        new("F14LongMagazine556Rifle", LuckyLootRarity.Uncommon),
        new("F14MagazineBox762", LuckyLootRarity.Uncommon),
        new("F14Magazine762Rifle", LuckyLootRarity.Uncommon),
        new("F14Magazine762AmmoShort", LuckyLootRarity.Uncommon),
        new("F14SpeedLoader44", LuckyLootRarity.Uncommon),

        new("F14DrinkNukaColaQuantum", LuckyLootRarity.Rare),
        new("F14HandheldHealthAnalyzer", LuckyLootRarity.Rare),
        new("F14MagazineSMG9mmDrum", LuckyLootRarity.Rare),
        new("F14LMGMagazine556Rifle", LuckyLootRarity.Rare),
        new("F14Magazine762AmmoBelt", LuckyLootRarity.Rare),
        new("F14MagazineBox308", LuckyLootRarity.Rare),
        new("F14Magazine308Rifle", LuckyLootRarity.Rare),
        new("F14Magazine308RifleLong", LuckyLootRarity.Rare),
        new("F14ClipMagazine308Rifle", LuckyLootRarity.Rare),
        new("F14Magazine308RifleSniper", LuckyLootRarity.Rare),
        new("F14MagazineBox4570", LuckyLootRarity.Rare),
        new("F14SpeedLoader4570", LuckyLootRarity.Rare),
        new("F14SpeedLoader4570Tube", LuckyLootRarity.Rare),
        new("F14MagazineBox50", LuckyLootRarity.Rare),
        new("F14Magazine50AMR", LuckyLootRarity.Rare),
        new("F14MagazineMinigun5mm", LuckyLootRarity.Rare),
        new("F14Ammo40mmGrenadeFrag", LuckyLootRarity.Rare),
        new("F14Ammo40mmGrenadeFire", LuckyLootRarity.Rare),
        new("F14PowerCellSmall", LuckyLootRarity.Rare),
        new("F14MicrofusionCell", LuckyLootRarity.Rare),
        new("F14ElectronChargePack", LuckyLootRarity.Rare),

        new("F14SuperStimpak", LuckyLootRarity.VeryRare),
        new("F14PlasmaCartridge", LuckyLootRarity.VeryRare),
        new("F14FusionCore", LuckyLootRarity.VeryRare),
        new("F14PlasmaCartridgeMultiplas", LuckyLootRarity.VeryRare),
        new("F14PlasmaShell", LuckyLootRarity.VeryRare),

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
