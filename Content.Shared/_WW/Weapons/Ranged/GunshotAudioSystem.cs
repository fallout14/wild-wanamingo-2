using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._WW.Weapons.Ranged;

/// <summary>
/// Plays gunshots, keeping at most <see cref="MaxOverlap"/> of one gun's shots playing at once by stopping the
/// oldest. Fast guns (miniguns) firing long shot sounds used up the engine's 16-copies-per-file limit, so shots
/// went silent, and then used up the sound card's audio sources: Release clients froze logging the failures and
/// Debug clients crashed on an assert (2026-11-23 playtest). The newest shot always plays; the cut tails are
/// covered by the shots after them.
/// </summary>
public sealed partial class GunshotAudioSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// Most shots from one gun that play at the same time.
    /// </summary>
    public const int MaxOverlap = 6;

    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(30);

    private readonly Dictionary<EntityUid, List<EntityUid>> _playing = new();
    private TimeSpan _nextPrune;

    public void PlayGunshot(EntityUid gun, SoundSpecifier? sound, EntityUid? user)
    {
        if (_audio.PlayPredicted(sound, gun, user) is not { } played)
            return;

        if (!_playing.TryGetValue(gun, out var shots))
            _playing[gun] = shots = new List<EntityUid>();

        shots.RemoveAll(e => TerminatingOrDeleted(e));
        shots.Add(played.Entity);

        while (shots.Count > MaxOverlap)
        {
            _audio.Stop(shots[0]);
            shots.RemoveAt(0);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextPrune)
            return;

        _nextPrune = _timing.CurTime + PruneInterval;
        List<EntityUid>? gone = null;
        foreach (var (gun, shots) in _playing)
        {
            shots.RemoveAll(e => TerminatingOrDeleted(e));
            if (shots.Count == 0 || TerminatingOrDeleted(gun))
                (gone ??= new List<EntityUid>()).Add(gun);
        }

        if (gone == null)
            return;

        foreach (var gun in gone)
        {
            _playing.Remove(gun);
        }
    }
}
