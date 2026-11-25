// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14) / Corvax Forge: Content.Server/_NC/Barks/BarkSystem.cs
// Changed for Wild Wanamingo: WW cvars; the voice comes from the character profile at spawn (BarkProfileSystem).

using Content.Shared._Forge.Barks;
using Content.Shared._WW.CCVar;
using Content.Shared.Chat;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Forge.Barks;

public sealed partial class BarkSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeechSynthesisComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeNetworkEvent<RequestPreviewBarkEvent>(OnRequestPreviewBark);
    }

    private void OnEntitySpoke(Entity<SpeechSynthesisComponent> ent, ref EntitySpokeEvent args)
    {
        if (ent.Comp.VoicePrototypeId is not { } voice ||
            !_cfg.GetCVar(WWCVars.BarksEnabled) ||
            !_prototype.TryIndex(voice, out var bark) ||
            bark.SoundFiles.Count == 0)
        {
            return;
        }

        RaiseNetworkEvent(new PlayBarkEvent(_random.Pick(bark.SoundFiles),
            GetNetEntity(ent),
            args.Message,
            ent.Comp.PlaybackSpeed,
            args.ObfuscatedMessage != null));
    }

    private void OnRequestPreviewBark(RequestPreviewBarkEvent ev, EntitySessionEventArgs args)
    {
        if (!_cfg.GetCVar(WWCVars.BarksEnabled) ||
            !_prototype.TryIndex<BarkPrototype>(ev.BarkVoiceId, out var bark) ||
            bark.SoundFiles.Count == 0)
        {
            return;
        }

        _audio.PlayGlobal(new SoundPathSpecifier(_random.Pick(bark.SoundFiles)),
            args.SenderSession,
            AudioParams.Default.WithVolume(4f).WithVariation(0.125f));
    }
}
