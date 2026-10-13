// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/_Goobstation/Clothing/ToggleableClothingBoundUserInterface.cs
// (Misfits' copy of a Goobstation/Space Station 14 file.) Renamed Multi* so it lives next to Wizden's
// single-piece ToggleableClothing: power armor deploys several pieces from one suit.

using Content.Shared._Misfits.Clothing.Legacy;
using Content.Shared.Clothing.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client._Misfits.Clothing.Legacy;

public sealed partial class MultiToggleableClothingBoundUserInterface : BoundUserInterface
{
    [Dependency] private IClyde _displayManager = default!;
    [Dependency] private IInputManager _inputManager = default!;

    private IEntityManager _entityManager;
    private MultiToggleableClothingRadialMenu? _menu;

    public MultiToggleableClothingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _entityManager = IoCManager.Resolve<IEntityManager>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MultiToggleableClothingRadialMenu>();
        _menu.SetEntity(Owner);
        _menu.SendToggleClothingMessageAction += SendToggleableClothingMessage;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    private void SendToggleableClothingMessage(EntityUid uid)
    {
        var message = new MultiToggleableClothingUiMessage(_entityManager.GetNetEntity(uid));
        SendPredictedMessage(message);
    }
}
