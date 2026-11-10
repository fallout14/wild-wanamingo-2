using Content.Shared._Forge.Trade;
using Content.Shared.Chat;
using Content.Shared.Pulling.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._WW.Vendortrons;

public sealed partial class WWVendortronSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WWVendortronComponent, BeingPulledAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<WWVendortronComponent, BoundUIOpenedEvent>(OnStoreOpened);
    }

    private void OnPullAttempt(EntityUid uid, WWVendortronComponent comp, BeingPulledAttemptEvent args)
    {
        // Bolted-down shop robot (MS13 has them resist all movement).
        args.Cancel();
    }

    private void OnStoreOpened(EntityUid uid, WWVendortronComponent comp, BoundUIOpenedEvent args)
    {
        if (_net.IsClient || !Equals(args.UiKey, StoreUiKey.Key) || comp.Lines.Count == 0)
            return;

        if (_timing.CurTime < comp.NextLine)
            return;

        comp.NextLine = _timing.CurTime + comp.LineCooldown;
        _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(comp.Lines)), InGameICChatType.Speak, hideChat: false, hideLog: true);
    }
}
