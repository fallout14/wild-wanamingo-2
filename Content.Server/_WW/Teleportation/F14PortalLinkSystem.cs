using System.Linq;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Server._WW.Teleportation;

/// <summary>
/// Pairs Wizden portals for ladders, manholes and bunker hatches without the mapper linking them by hand:
/// portals sharing a <see cref="LinkId"/> are linked at round start (or whenever one spawns later).
/// With <see cref="Side"/> set, a portal only links to the other side's ends (surface hatches to tunnel doors).
/// </summary>
[RegisterComponent]
public sealed partial class F14PortalLinkComponent : Component
{
    /// <summary>
    /// Ends with the same id lead to each other. Empty: not paired.
    /// </summary>
    [DataField]
    public string LinkId = string.Empty;

    /// <summary>
    /// Optional: only link to ends whose side differs (e.g. "surface" and "tunnel"). Empty links to every other end.
    /// </summary>
    [DataField]
    public string Side = string.Empty;
}

public sealed partial class F14PortalLinkSystem : EntitySystem
{
    [Dependency] private LinkedEntitySystem _link = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<F14PortalLinkComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<F14PortalLinkComponent> ent, ref MapInitEvent args)
    {
        LinkMatches(ent);
    }

    /// <summary>
    /// Re-pairs a portal under a new link id (admin command <c>setwarpid</c>).
    /// </summary>
    public void SetLinkId(Entity<F14PortalLinkComponent> ent, string linkId)
    {
        if (TryComp<LinkedEntityComponent>(ent, out var links))
        {
            foreach (var other in links.LinkedEntities.ToList())
            {
                _link.TryUnlink(ent, other);
            }
        }

        ent.Comp.LinkId = linkId;
        LinkMatches(ent);
    }

    private void LinkMatches(Entity<F14PortalLinkComponent> ent)
    {
        if (ent.Comp.LinkId == string.Empty)
            return;

        var query = EntityQueryEnumerator<F14PortalLinkComponent>();
        while (query.MoveNext(out var other, out var otherLink))
        {
            if (other == ent.Owner || otherLink.LinkId != ent.Comp.LinkId)
                continue;

            if (ent.Comp.Side != string.Empty && otherLink.Side == ent.Comp.Side)
                continue;

            _link.TryLink(ent, other);
        }
    }
}
