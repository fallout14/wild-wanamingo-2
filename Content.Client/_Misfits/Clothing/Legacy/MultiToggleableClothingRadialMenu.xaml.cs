// Ported from Misfits (https://github.com/Misfit-Sanctuary/nuclear-14): Content.Client/_Goobstation/Clothing/ToggleableClothingRadialMenu.xaml.cs
// (Misfits' copy of a Goobstation/Space Station 14 file.) Renamed Multi* so it lives next to Wizden's
// single-piece ToggleableClothing: power armor deploys several pieces from one suit.

using Content.Shared._Misfits.Clothing.Legacy;
using Content.Client.UserInterface.Controls;
using Content.Shared.Clothing.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._Misfits.Clothing.Legacy;

public sealed partial class MultiToggleableClothingRadialMenu : RadialMenu
{
    [Dependency] private EntityManager _entityManager = default!;

    public event Action<EntityUid>? SendToggleClothingMessageAction;

    public EntityUid Entity { get; set; }

    public MultiToggleableClothingRadialMenu()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);
    }

    public void SetEntity(EntityUid uid)
    {
        Entity = uid;
        RefreshUI();
    }

    public void RefreshUI()
    {
        // Even EmotesMenu has to call this, I'm assuming it's essential.
        var main = FindControl<RadialContainer>("Main");

        if (!_entityManager.TryGetComponent<MultiToggleableClothingComponent>(Entity, out var clothing)
            || clothing.Container is not { } clothingContainer)
            return;

        foreach (var attached in clothing.ClothingUids)
        {
            // Change tooltip text if attached clothing is toggle/untoggled
            var tooltipText = Loc.GetString(clothing.UnattachTooltip);

            if (clothingContainer.Contains(attached.Key))
                tooltipText = Loc.GetString(clothing.AttachTooltip);

            var button = new MultiToggleableClothingRadialMenuButton()
            {
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(64, 64),
                ToolTip = tooltipText,
                AttachedClothingId = attached.Key
            };

            // WW Change start - draw the piece from its prototype. The live piece draws nothing once it is deployed
            // (worn items are hidden), which left an empty, unfindable button for every piece you had out.
            // Stowed pieces are dimmed so you can tell at a glance which ones are deployed.
            var spriteView = new EntityPrototypeView()
            {
                SetSize = new Vector2(48, 48),
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Stretch = SpriteView.StretchMode.Fill
            };

            spriteView.SetPrototype(_entityManager.GetComponentOrNull<MetaDataComponent>(attached.Key)?.EntityPrototype?.ID);
            if (clothingContainer.Contains(attached.Key))
                spriteView.Modulate = Color.White.WithAlpha(0.5f);
            // WW Change end

            button.AddChild(spriteView);
            main.AddChild(button);
        }

        AddToggleableClothingMenuButtonOnClickAction(main);
    }

    private void AddToggleableClothingMenuButtonOnClickAction(Control control)
    {
        if (control is not RadialContainer mainControl)
            return;

        foreach (var child in mainControl.Children)
        {
            if (child is not MultiToggleableClothingRadialMenuButton castChild)
                continue;

            castChild.OnButtonDown += _ =>
            {
                SendToggleClothingMessageAction?.Invoke(castChild.AttachedClothingId);
                mainControl.DisposeAllChildren();
                RefreshUI();
            };
        }
    }
}

public sealed class MultiToggleableClothingRadialMenuButton : RadialMenuButton
{
    public EntityUid AttachedClothingId { get; set; }
}
