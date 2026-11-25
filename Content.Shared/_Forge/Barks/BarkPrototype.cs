// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) / Corvax Forge: Content.Shared/_NC/Barks/
// Changed for Wild Wanamingo: namespace, default voice constant moved here.

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Forge.Barks;

/// <summary>
/// A bark voice: short sounds played while a character talks.
/// </summary>
[Prototype]
public sealed partial class BarkPrototype : IPrototype
{
    public const string DefaultBarkVoice = "BarksGoonSpeak1";

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Display name (plain text, not a locale key).
    /// </summary>
    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Sounds picked from at random while speaking.
    /// </summary>
    [DataField(required: true)]
    public List<string> SoundFiles { get; private set; } = new();

    /// <summary>
    /// Whether players can pick it in the character editor.
    /// </summary>
    [DataField]
    public bool RoundStart { get; private set; } = true;
}

/// <summary>
/// Lets an entity bark when it speaks.
/// </summary>
[RegisterComponent]
public sealed partial class SpeechSynthesisComponent : Component
{
    [DataField("voice")]
    public ProtoId<BarkPrototype>? VoicePrototypeId;

    [DataField]
    public float PlaybackSpeed = 1.0f;
}

[Serializable, NetSerializable]
public sealed class RequestPreviewBarkEvent(string barkVoiceId) : EntityEventArgs
{
    public string BarkVoiceId { get; } = barkVoiceId;
}

[Serializable, NetSerializable]
public sealed class PlayBarkEvent(string soundPath, NetEntity sourceUid, string message, float playbackSpeed, bool obfuscated)
    : EntityEventArgs
{
    public string SoundPath { get; } = soundPath;
    public NetEntity SourceUid { get; } = sourceUid;
    public string Message { get; } = message;
    public float PlaybackSpeed { get; } = playbackSpeed;
    public bool Obfuscated { get; } = obfuscated;
}
