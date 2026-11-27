using Content.Shared._WW.Species;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._WW.Species;

/// <summary>
/// WW: opens <see cref="ProtectronModuleMenu"/> for a fresh Protectron and sends the confirmed module.
/// </summary>
[UsedImplicitly]
public sealed class ProtectronModuleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ProtectronModuleMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ProtectronModuleMenu>();
        if (EntMan.TryGetComponent<ProtectronModuleComponent>(Owner, out var modules))
            _menu.Populate(modules.Modules);
        _menu.Confirmed += index => SendMessage(new ProtectronModuleSelectMessage(index));
    }
}
