// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_NC/Trade/Domain/Contracts/ContractTargetClientData.cs

using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Trade;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ContractTargetClientData
{
    [DataField("match")]
    public PrototypeMatchMode MatchMode = PrototypeMatchMode.Exact;

    public ContractTargetClientData() { }

    public ContractTargetClientData(string targetItem, int required, int progress)
    {
        TargetItem = targetItem;
        Required = required;
        Progress = progress;
    }

    public string TargetItem { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Progress { get; set; }
}
