using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._WW.Robots;

/// <summary>
/// WW: gives a robot an action to switch its own sounds off and on: speech beeps and emote sounds
/// (SpeechNoiseSystem and SharedChatSystem.TryPlayEmoteSound skip a muted robot).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RobotSoundMuteComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionF14RobotToggleSounds";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool Muted;
}

public sealed partial class ToggleRobotSoundsEvent : InstantActionEvent;

/// <summary>
/// WW: gives a Sentry Bot the action to blow its own fusion core (it starts the chassis' self-destruct fuse, the
/// same one that runs when it dies). Needs a fusion core in its power cell slot.
/// </summary>
[RegisterComponent]
public sealed partial class FusionCoreDetonatorComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionF14DetonateFusionCore";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string Slot = "cell_slot";

    /// <summary>
    /// Trigger key that starts the self-destruct timer (TimerTrigger keysIn).
    /// </summary>
    [DataField]
    public string TriggerKey = "prime";
}

public sealed partial class DetonateFusionCoreEvent : InstantActionEvent;
