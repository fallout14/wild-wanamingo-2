// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/_Misfits/Holotape/HolotapeBoundUserInterface.cs
// WW: trimmed to the reader (no notes, database, link or Overwatch tabs).

using Content.Shared._Misfits.Holotape;
using Robust.Client.UserInterface;

namespace Content.Client._Misfits.Holotape;

public sealed partial class HolotapeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private HolotapeWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<HolotapeWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not HolotapeBoundUserInterfaceState cast)
            return;

        _window.SetHolotapeMode(cast.IsHolotapeItem);
        _window.UpdateContent(cast.Title, cast.Content);
    }
}
