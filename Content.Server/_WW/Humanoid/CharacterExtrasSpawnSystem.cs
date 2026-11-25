using System.Numerics;
using Content.Shared._Forge.Barks;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Speech;
using Content.Shared.Sprite;

namespace Content.Server._WW.Humanoid;

/// <summary>
/// WW: applies the Misfits character extras from the profile to a freshly spawned character: bark voice, vocal style
/// (speech verb) and body size (sprite scale).
/// </summary>
public sealed partial class CharacterExtrasSpawnSystem : EntitySystem
{
    [Dependency] private SharedScaleVisualsSystem _scale = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        Apply(args.Mob, args.Profile);
    }

    public void Apply(EntityUid mob, HumanoidCharacterProfile profile)
    {
        var bark = EnsureComp<SpeechSynthesisComponent>(mob);
        bark.VoicePrototypeId = profile.BarkVoice;

        if (profile.SpeechVerb != HumanoidCharacterProfile.DefaultSpeechVerb && TryComp<SpeechComponent>(mob, out var speech))
        {
            speech.SpeechVerb = profile.SpeechVerb;
            Dirty(mob, speech);
        }

        if (!MathHelper.CloseTo(profile.Height, 1f) || !MathHelper.CloseTo(profile.Width, 1f))
            _scale.SetSpriteScale(mob, new Vector2(profile.Width, profile.Height));
    }
}
