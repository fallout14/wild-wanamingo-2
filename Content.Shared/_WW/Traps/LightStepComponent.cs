using Robust.Shared.GameStates;

namespace Content.Shared._WW.Traps;

/// <summary>
/// Light Step (character trait): this mob never sets off traps or landmines by stepping on them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LightStepComponent : Component;
