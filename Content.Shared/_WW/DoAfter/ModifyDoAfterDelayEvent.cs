using Content.Shared.DoAfter;

namespace Content.Shared._WW.DoAfter;

/// <summary>
/// Raised on a do-after's user just before it starts. Handlers may change <see cref="DoAfterArgs.Delay"/>.
/// </summary>
[ByRefEvent]
public readonly record struct ModifyDoAfterDelayEvent(DoAfterArgs Args);
