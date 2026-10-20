// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Trade/Domain/Contracts/ContractRewards.cs

using Robust.Shared.Serialization;


namespace Content.Shared._Forge.Trade;



[Serializable, NetSerializable]
public enum StoreRewardType : byte
{
    Item,
    Currency,
    Pool
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ContractRewardDef
{
    [DataField("type")]
    public StoreRewardType Type { get; set; } = StoreRewardType.Item;

    [DataField("id")]
    public string Id { get; set; } = string.Empty;

    [DataField("amount")]
    public IntRange Amount { get; set; } = IntRange.Fixed(1);

    [DataField("prob")]
    public float Probability { get; set; } = 1.0f;

    [DataField("weight")]
    public int Weight { get; set; } = 1;

    [DataField("max")]
    public int MaxRepeats { get; set; } = 0;

    [DataField("options")]
    public List<ContractRewardDef>? Options { get; set; }
}

[Serializable, NetSerializable]
public readonly record struct ContractRewardData(StoreRewardType Type, string Id, int Amount);
