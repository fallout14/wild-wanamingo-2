using Content.Shared._WW.Power;
using JetBrains.Annotations;

namespace Content.Client._WW.Power;

[UsedImplicitly]
public sealed class RobcoPowerTerminalBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RobcoPowerTerminalWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = new RobcoPowerTerminalWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is RobcoPowerTerminalState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
