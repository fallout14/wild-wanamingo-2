// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) / Corvax Forge: Content.Client/_NC/Barks/BarkSystem.cs
// Changed for Wild Wanamingo: WW cvars; plays through the normal audio path instead of loading the file each time.

using Content.Shared._Forge.Barks;
using Content.Shared._WW.CCVar;
using Content.Shared.Chat;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Forge.Barks;

/// <summary>
/// Plays the barks of characters talking nearby: a few blips spread over how long the message is.
/// </summary>
public sealed partial class BarkSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float MinimalVolume = -10f;
    private const float WhisperFade = 4f;
    private const int MaxBlips = 12;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayBarkEvent>(OnPlayBark);
    }

    public void RequestPreviewBark(string barkVoiceId)
    {
        RaiseNetworkEvent(new RequestPreviewBarkEvent(barkVoiceId));
    }

    private void OnPlayBark(PlayBarkEvent ev)
    {
        var source = GetEntity(ev.SourceUid);
        if (!Exists(source))
            return;

        if (_player.LocalEntity is { } local &&
            _transform.GetMapCoordinates(source) is var sourcePos &&
            _transform.GetMapCoordinates(local) is var localPos &&
            (sourcePos.MapId != localPos.MapId ||
             (sourcePos.Position - localPos.Position).Length() > SharedChatSystem.VoiceRange))
        {
            return;
        }

        var gain = _cfg.GetCVar(WWCVars.BarksVolume);
        if (gain <= 0f)
            return;

        var volume = MinimalVolume + SharedAudioSystem.GainToVolume(gain * 3f);
        if (ev.Obfuscated)
            volume -= WhisperFade;

        var audioParams = AudioParams.Default.WithVolume(volume).WithVariation(0.125f);
        var sound = new SoundPathSpecifier(ev.SoundPath);

        var interval = 0.15f / Math.Max(ev.PlaybackSpeed, 0.1f);
        var count = Math.Clamp((int) (ev.Message.Length * 0.05f / interval), 1, MaxBlips);

        for (var i = 0; i < count; i++)
        {
            Timer.Spawn(TimeSpan.FromSeconds(i * interval), () =>
            {
                if (!Exists(source))
                    return;

                _audio.PlayEntity(sound, Filter.Local(), source, false, audioParams);
            });
        }
    }
}
