using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared._WW.Weapons.Ranged;

/// <summary>
/// WW: folding stocks (see <see cref="FoldingStockComponent"/>). The sprite swap is client side, in
/// Content.Client/_WW/Weapons/Ranged/FoldingStockVisualizerSystem.
/// </summary>
public sealed partial class FoldingStockSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoldingStockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FoldingStockComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<FoldingStockComponent, ToggleFoldingStockEvent>(OnToggleAction);
        SubscribeLocalEvent<FoldingStockComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<FoldingStockComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<FoldingStockComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<FoldingStockComponent> ent, ref MapInitEvent args)
    {
        Apply(ent);
    }

    private void OnGetItemActions(Entity<FoldingStockComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    private void OnToggleAction(Entity<FoldingStockComponent> ent, ref ToggleFoldingStockEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySetFolded(ent, !ent.Comp.Folded, args.Performer);
    }

    private void OnGetVerbs(Entity<FoldingStockComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(ent.Comp.Folded ? "ww-folding-stock-verb-unfold" : "ww-folding-stock-verb-fold"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),
            Act = () => TrySetFolded(ent, !ent.Comp.Folded, user),
        });
    }

    private void OnRefreshModifiers(Entity<FoldingStockComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!ent.Comp.Folded)
            return;

        args.MinAngle *= ent.Comp.FoldedSpreadMultiplier;
        args.MaxAngle *= ent.Comp.FoldedSpreadMultiplier;
        args.AngleIncrease *= ent.Comp.FoldedSpreadMultiplier;
        args.CameraRecoilScalar *= ent.Comp.FoldedRecoilMultiplier;
    }

    private void OnExamined(Entity<FoldingStockComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Folded ? "ww-folding-stock-examine-folded" : "ww-folding-stock-examine-unfolded"));
    }

    /// <summary>
    /// Folds or extends the stock. Fails if it's already that way, or the gun is stowed (in a bag, on a belt or your
    /// back) rather than held or lying on the ground: changing its size in there could overfill the bag.
    /// </summary>
    public bool TrySetFolded(Entity<FoldingStockComponent> ent, bool folded, EntityUid? user = null)
    {
        if (ent.Comp.Folded == folded)
            return false;

        if (_container.TryGetContainingContainer(ent.Owner, out var container) &&
            !_hands.IsHolding(container.Owner, ent.Owner, out _))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("ww-folding-stock-popup-stowed", ("gun", ent.Owner)), ent, user.Value);
            return false;
        }

        ent.Comp.Folded = folded;
        Dirty(ent);
        Apply(ent);

        if (user != null)
        {
            _audio.PlayPredicted(folded ? ent.Comp.FoldSound : ent.Comp.UnfoldSound, ent, user);
            _popup.PopupClient(Loc.GetString(folded ? "ww-folding-stock-popup-folded" : "ww-folding-stock-popup-unfolded", ("gun", ent.Owner)), ent, user.Value);
        }

        return true;
    }

    private void Apply(Entity<FoldingStockComponent> ent)
    {
        _item.SetSize(ent, ent.Comp.Folded ? ent.Comp.FoldedSize : ent.Comp.UnfoldedSize);
        _appearance.SetData(ent, FoldingStockVisuals.Folded, ent.Comp.Folded);
        if (HasComp<GunComponent>(ent))
            _gun.RefreshModifiers(ent.Owner);
    }
}
