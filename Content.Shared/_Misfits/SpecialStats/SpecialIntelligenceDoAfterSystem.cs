// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): the Intelligence medical speed from
// Content.Server/Medical/HealingSystem.cs, DefibrillatorSystem.cs, HealthAnalyzerSystem.cs, StethoscopeSystem.cs and
// Content.Shared/Chemistry/EntitySystems/SharedInjectorSystem.cs.
// WW: Misfits edited each system; here one handler scales those do-afters through ModifyDoAfterDelayEvent.

using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._WW.DoAfter;
using Content.Shared.Chemistry.Events;
using Content.Shared.Medical;
using Content.Shared.MedicalScanner;

namespace Content.Shared._Misfits.SpecialStats;

/// <summary>
/// Smart characters treat patients faster: Intelligence scales the time of medical do-afters.
/// </summary>
public sealed partial class SpecialIntelligenceDoAfterSystem : EntitySystem
{
    [Dependency] private SharedSpecialSystem _special = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpecialComponent, ModifyDoAfterDelayEvent>(OnModifyDelay);
    }

    private void OnModifyDelay(Entity<SpecialComponent> ent, ref ModifyDoAfterDelayEvent args)
    {
        if (!IsMedical(args.Args.Event))
            return;

        args.Args.Delay = _special.GetIntelligenceMedicalActionDelay(ent.Owner, args.Args.Delay, ent.Comp);
    }

    public static bool IsMedical(object ev)
    {
        return ev is HealingDoAfterEvent
            or DefibrillatorZapDoAfterEvent
            or HealthAnalyzerDoAfterEvent
            or StethoscopeDoAfterEvent
            or InjectorDoAfterEvent;
    }
}
