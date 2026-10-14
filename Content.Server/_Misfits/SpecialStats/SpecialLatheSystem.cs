// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): the Intelligence workbench rules from
// Content.Server/Lathe/LatheSystem.cs.
// WW: Misfits edited Wizden's lathe and tracked who queued each job; here a separate system remembers the last person
// to queue something and scales the job when it starts.

using Content.Server.Lathe;
using Content.Server.Lathe.Components;
using Content.Shared._Misfits.Special;
using Content.Shared.Lathe;
using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Intelligence 3 or lower can't work a workbench; above 5, jobs finish faster (below, slower).
/// </summary>
public sealed partial class SpecialLatheSystem : EntitySystem
{
    [Dependency] private SharedSpecialSystem _special = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LatheComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        // Wizden's lathe already listens on LatheComponent; ours rides on the operator component added at UI open.
        SubscribeLocalEvent<SpecialLatheOperatorComponent, LatheQueueRecipeMessage>(OnQueue, before: [typeof(LatheSystem)]);
        SubscribeLocalEvent<SpecialLatheOperatorComponent, LatheStartPrintingEvent>(OnStartPrinting);
    }

    private void OnOpenAttempt(Entity<LatheComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (_special.CanUseLatheWithIntelligence(args.User))
        {
            EnsureComp<SpecialLatheOperatorComponent>(ent);
            return;
        }

        args.Cancel();
        if (!args.Silent)
            _popup.PopupEntity(Loc.GetString("construction-system-construct-too-low-intelligence"), ent, args.User);
    }

    private void OnQueue(Entity<SpecialLatheOperatorComponent> ent, ref LatheQueueRecipeMessage args)
    {
        ent.Comp.Operator = args.Actor;
    }

    private void OnStartPrinting(Entity<SpecialLatheOperatorComponent> ent, ref LatheStartPrintingEvent args)
    {
        if (ent.Comp.Operator is not { } user || Deleted(user))
            return;

        if (!TryComp<LatheProducingComponent>(ent, out var producing))
            return;

        producing.ProductionLength = _special.GetIntelligenceLatheProductionTime(user, producing.ProductionLength);
    }
}

/// <summary>
/// The last character to queue a job on this workbench; their Intelligence sets how long jobs take.
/// </summary>
[RegisterComponent]
public sealed partial class SpecialLatheOperatorComponent : Component
{
    [ViewVariables]
    public EntityUid? Operator;
}
