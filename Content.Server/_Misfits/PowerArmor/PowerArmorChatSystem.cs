// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/PowerArmor/PowerArmorChatSystem.cs
// Changed for Wild Wanamingo: brace-stance emotes only (the suit open/close emote hooked an event Misfits patched into
// Wizden's toggleable clothing); a simple per-wearer cooldown replaces Misfits' emote throttle system.

using Content.Server.Chat.Systems;
using Content.Shared._Misfits.PowerArmor;
using Content.Shared.Chat;
using Robust.Shared.Timing;

namespace Content.Server._Misfits.PowerArmor;

/// <summary>
/// Sends a nearby emote when the wearer activates or deactivates the power armor brace stance.
/// </summary>
public sealed partial class PowerArmorChatSystem : EntitySystem
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _lastEmote = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerArmorBraceComponent, PowerArmorBraceActivatedEvent>(OnBraceActivated);
        SubscribeLocalEvent<PowerArmorBraceComponent, PowerArmorBraceDeactivatedEvent>(OnBraceDeactivated);
    }

    private void OnBraceActivated(EntityUid uid, PowerArmorBraceComponent component, PowerArmorBraceActivatedEvent args)
    {
        Emote(args.User, args.Armor, "misfits-chat-power-armor-brace-activate");
    }

    private void OnBraceDeactivated(EntityUid uid, PowerArmorBraceComponent component, PowerArmorBraceDeactivatedEvent args)
    {
        Emote(args.User, args.Armor, "misfits-chat-power-armor-brace-deactivate");
    }

    private void Emote(EntityUid user, EntityUid armor, string message)
    {
        if (TerminatingOrDeleted(user))
            return;

        var now = _timing.CurTime;
        if (_lastEmote.TryGetValue(user, out var last) && now - last < Cooldown)
            return;

        _lastEmote[user] = now;
        var armorName = Exists(armor) ? Name(armor) : "power armor";
        _chat.TrySendInGameICMessage(user, Loc.GetString(message, ("armor", armorName)),
            InGameICChatType.Emote, ChatTransmitRange.Normal);
    }
}
