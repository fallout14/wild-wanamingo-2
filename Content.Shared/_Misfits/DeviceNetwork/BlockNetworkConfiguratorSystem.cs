// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Server/DeviceNetwork/Systems/BlockNetworkConfiguratorSystem.cs
// WW Change - shared, on Wizden's by-ref LinkAttemptEvent.

using Content.Shared.DeviceLinking.Events;

namespace Content.Shared._Misfits.DeviceNetwork;

public sealed partial class BlockNetworkConfiguratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockNetworkConfiguratorComponent, LinkAttemptEvent>(OnLinkAttempt);
    }

    private void OnLinkAttempt(Entity<BlockNetworkConfiguratorComponent> ent, ref LinkAttemptEvent args)
    {
        // Links made by the game itself (no user) are always fine.
        if (args.User == null || IsAllowedSource(ent.Comp, args.Source))
            return;

        args.Cancelled = true;
    }

    public bool IsAllowedSource(BlockNetworkConfiguratorComponent component, EntityUid source)
    {
        return Prototype(source) is { } prototype && component.AllowedSourcePrototypes.Contains(prototype.ID);
    }
}
