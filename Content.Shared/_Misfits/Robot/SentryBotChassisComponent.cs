// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Shared/_Misfits/Robot/SentryBotChassisComponent.cs,
// SentryBotMissileLaunchEvent.cs

using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Misfits.Robot;

/// <summary>
/// Player sentry bot: the built-in gun overheats when its ammo runs dry, and a missile launcher action fires at a
/// marked spot after a warning delay.
/// </summary>
[RegisterComponent]
public sealed partial class SentryBotChassisComponent : Component
{
    /// <summary>
    /// Seconds the gun is down after running dry, before its ammo refills.
    /// </summary>
    [DataField]
    public float OverheatDuration = 5f;

    /// <summary>
    /// Seconds between marking a missile target and the launch.
    /// </summary>
    [DataField]
    public float TargetingDelay = 4f;

    [DataField]
    public EntProtoId MissileAction = "MisfitsActionSentryBotMissileLaunch";

    [DataField]
    public EntProtoId MissilePrototype = "N14ProjectileMissile";

    [DataField]
    public float MissileSpeed = 12f;

    /// <summary>
    /// Everyone this close to the target gets a missile lock warning.
    /// </summary>
    [DataField]
    public float WarningRange = 6f;

    [DataField]
    public EntityUid? MissileLaunchActionEntity;

    [ViewVariables]
    public bool Overheating;

    [ViewVariables]
    public TimeSpan OverheatEndTime;

    [ViewVariables]
    public bool IsTargeting;

    [ViewVariables]
    public EntityCoordinates TargetCoords;

    [ViewVariables]
    public TimeSpan MissileLaunchTime;
}

/// <summary>
/// The sentry bot's missile launcher action.
/// </summary>
public sealed partial class SentryBotMissileLaunchEvent : WorldTargetActionEvent;
