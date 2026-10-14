// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Server/_Misfits/Holotape/HolotapeSystem.cs
// WW: trimmed to holotape playback and terminal entries (no notes, faction database, link or Overwatch tabs).

using Content.Shared._Misfits.Holotape;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Misfits.Holotape;

/// <summary>
/// Holotapes need a reader (terminal or Pip-Boy) to be read. Terminals show a random pre-war/post-war entry when used.
/// </summary>
public sealed partial class HolotapeSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Terminal entries already handed out this round, so no two terminals show the same one.
    /// </summary>
    private readonly HashSet<string> _usedEntries = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<HolotapeDataComponent, MapInitEvent>(OnHolotapeDataMapInit);
        SubscribeLocalEvent<TerminalRandomContentComponent, MapInitEvent>(OnTerminalRandomMapInit);
        SubscribeLocalEvent<HolotapeReaderComponent, InteractUsingEvent>(OnReaderInteractUsing);
        SubscribeLocalEvent<HolotapeDataComponent, AfterActivatableUIOpenEvent>(OnTerminalUIOpen);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _usedEntries.Clear();
    }

    private void OnHolotapeDataMapInit(Entity<HolotapeDataComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Localized)
            return;

        if (!string.IsNullOrEmpty(ent.Comp.Title))
            ent.Comp.Title = Loc.GetString(ent.Comp.Title);

        if (!string.IsNullOrEmpty(ent.Comp.Content))
            ent.Comp.Content = Loc.GetString(ent.Comp.Content);

        ent.Comp.Localized = true;
    }

    private void OnTerminalRandomMapInit(Entity<TerminalRandomContentComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HolotapeDataComponent>(ent, out var data) || ent.Comp.ContentPool.Count == 0)
            return;

        if (!_prototype.Resolve(_random.Pick(ent.Comp.ContentPool), out var dataset) || dataset.Values.Count == 0)
            return;

        var available = new List<string>();
        foreach (var val in dataset.Values)
        {
            if (!_usedEntries.Contains(val))
                available.Add(val);
        }

        // More terminals than entries: allow repeats.
        if (available.Count == 0)
            available.AddRange(dataset.Values);

        var entryKey = _random.Pick(available);
        _usedEntries.Add(entryKey);

        // The data's own MapInit may already have run, so localize here.
        data.Title = Loc.GetString(entryKey + "-title");
        data.Content = Loc.GetString(entryKey + "-content");
        data.Localized = true;
    }

    /// <summary>
    /// Using a holotape on a reader opens the holotape's own viewer (the reader may already have its own UI).
    /// </summary>
    private void OnReaderInteractUsing(Entity<HolotapeReaderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<HolotapeDataComponent>(args.Used, out var holotape))
            return;

        if (!_ui.HasUi(args.Used, HolotapeUiKey.Key))
            return;

        _ui.OpenUi(args.Used, HolotapeUiKey.Key, args.User);
        _ui.SetUiState(args.Used, HolotapeUiKey.Key,
            new HolotapeBoundUserInterfaceState(holotape.Title, holotape.Content, isHolotapeItem: true));

        args.Handled = true;
    }

    /// <summary>
    /// A terminal opened directly shows its own entry.
    /// </summary>
    private void OnTerminalUIOpen(Entity<HolotapeDataComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!_ui.HasUi(ent.Owner, HolotapeUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, HolotapeUiKey.Key,
            new HolotapeBoundUserInterfaceState(ent.Comp.Title, ent.Comp.Content, isHolotapeItem: false));
    }
}
