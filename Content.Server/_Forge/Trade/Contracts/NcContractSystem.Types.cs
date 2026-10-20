// Ported from Corvax Forge content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_NC/Trade/Contracts/NcContractSystem.Types.cs

using Content.Shared._Forge.Trade;


namespace Content.Server._Forge.Trade;


public sealed partial class NcContractSystem : EntitySystem
{
    private readonly record struct ClaimTakeEntry(EntityUid Root, EntityUid Entity, int Amount, bool IsStack);

    private enum ClaimFailureReason : byte
    {
        None = 0,
        StoreMissing,
        ContractMissing,
        NoValidTargets,
        InvalidTarget,
        NotEnoughItems,
        MissingCrate,
        ExecutionFailed,
    }

    private readonly record struct ClaimAttemptResult(bool Success, ClaimFailureReason Reason, string? Details)
    {
        public static ClaimAttemptResult Ok() => new(true, ClaimFailureReason.None, null);
        public static ClaimAttemptResult Fail(ClaimFailureReason reason, string? details = null) => new(false, reason, details);
    }

    private readonly record struct PoolEntry(ContractRewardDef Def, string Key);

    private enum QuasiKeyKind : byte
    {
        Req,
        Tc,
        TReq,
        RAmount
    }

    private readonly record struct QuasiKey(QuasiKeyKind Kind, EntityUid Store, string ProtoId, string? Extra);
}
