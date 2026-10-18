// Ported from Goobstation content carried by Misfits (https://github.com/Misfit-Sanctuary/nuclear-14):
// Content.Server/_Goobstation/Vehicles/MotorbikeSystem.cs
// WW Change - runs on Wizden's vehicles: "engine running" is the vehicle being able to run (key in, fuelled, not
// burning), which this system vetoes through VehicleCanRunEvent. Popups are localized.

using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Goobstation.Vehicles;
using Content.Shared.Atmos.Components;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Vehicle;
using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Goobstation.Vehicles;

public sealed partial class MotorbikeSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private VehicleSystem _vehicle = default!;

    private static readonly ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MotorbikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MotorbikeComponent, VehicleCanRunEvent>(OnCanRun);
        SubscribeLocalEvent<MotorbikeComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(FlammableSystem)]);
        SubscribeLocalEvent<MotorbikeComponent, SolutionTransferAttemptEvent>(OnSolutionTransferAttempt);
        SubscribeLocalEvent<MotorbikeComponent, MotorbikeRefuelDoAfterEvent>(OnRefuelDoAfter);
        SubscribeLocalEvent<MotorbikeComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MotorbikeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MotorbikeComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MotorbikeComponent, VehicleComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out var motorbike, out var vehicle, out var mover))
        {
            if (motorbike.Burning)
            {
                if (motorbike.ExplodeAt <= _timing.CurTime)
                    Explode(uid, motorbike, vehicle);

                continue;
            }

            // Fuel burns while someone rides it with the engine able to run.
            if (!mover.CanMove || vehicle.Operator == null)
                continue;

            var fuelToUse = motorbike.FuelUsePerSecond.Float() * frameTime + motorbike.FuelAccumulator;
            var wholeFuel = FixedPoint2.New(MathF.Floor(fuelToUse));
            motorbike.FuelAccumulator = fuelToUse - wholeFuel.Float();

            if (wholeFuel <= FixedPoint2.Zero)
                continue;

            if (!TryConsumeFuel(uid, motorbike, wholeFuel))
            {
                _vehicle.RefreshCanRun((uid, vehicle));
                _popup.PopupEntity(Loc.GetString("motorbike-out-of-fuel"), uid, vehicle.Operator.Value);
            }
        }
    }

    private void OnInit(Entity<MotorbikeComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent, MotorbikeVisuals.Burning, ent.Comp.Burning);
    }

    private void OnCanRun(Entity<MotorbikeComponent> ent, ref VehicleCanRunEvent args)
    {
        if (ent.Comp.Burning || !HasUsableFuel(ent, ent.Comp))
            args.CanRun = false;
    }

    // Lets riders name their motorbike.
    private void OnGetVerbs(Entity<MotorbikeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("motorbike-rename-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rename.svg.192dpi.png")),
            Act = () =>
            {
                _quickDialog.OpenDialog(player, Loc.GetString("motorbike-rename-verb"), Loc.GetString("motorbike-rename-prompt"),
                    (string newName) =>
                    {
                        if (!TerminatingOrDeleted(ent))
                            _meta.SetEntityName(ent.Owner, newName);
                    });
            },
            Impact = LogImpact.Low,
        });
    }

    private void OnInteractUsing(Entity<MotorbikeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.Burning)
            return;

        if (_tool.HasQuality(args.Used, WeldingQuality))
        {
            if (_vehicle.GetOperatorOrNull(ent.Owner) == args.User)
            {
                _popup.PopupEntity(Loc.GetString("motorbike-repair-while-riding"), ent, args.User);
                args.Handled = true;
            }

            return;
        }

        if (TryStartRefuelDoAfter(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnSolutionTransferAttempt(Entity<MotorbikeComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if (args.To != ent.Owner)
            return;

        args.Cancel(Loc.GetString("motorbike-refuel-directly"));
    }

    private void OnRefuelDoAfter(Entity<MotorbikeComponent> ent, ref MotorbikeRefuelDoAfterEvent args)
    {
        ent.Comp.RefuelDoAfter = null;

        if (args.Handled || args.Cancelled || args.Used is not { } source)
            return;

        if (!IsReadyForRefuel(ent, args.User, popup: true))
            return;

        args.Handled = TryRefuel(ent, source, args.User, ent.Comp.RefillAmount);
    }

    private void OnDamageChanged(Entity<MotorbikeComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.Burning || _damageable.GetTotalDamage((ent.Owner, args.Damageable)) < ent.Comp.MaxIntegrity)
            return;

        StartBurning(ent);
    }

    private void OnExamined(Entity<MotorbikeComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            !_solution.TryGetSolution(ent.Owner, ent.Comp.FuelSolution, out _, out var fuelSolution))
        {
            return;
        }

        var current = fuelSolution.GetTotalPrototypeQuantity(ent.Comp.FuelReagent);
        args.PushMarkup(Loc.GetString("motorbike-examine-fuel", ("current", current), ("max", fuelSolution.MaxVolume)));
    }

    /// <summary>
    /// Checks a fuel source, with a popup for anything wrong. Null if it's usable, else whether it was handled.
    /// </summary>
    private bool? CheckFuelSource(Entity<MotorbikeComponent> motorbike, EntityUid source, EntityUid user)
    {
        if (!_solution.TryGetDrainableSolution(source, out _, out var sourceSolution) ||
            !_solution.TryGetSolution(motorbike.Owner, motorbike.Comp.FuelSolution, out _, out var fuelSolution))
        {
            return false;
        }

        var availableFuel = sourceSolution.GetTotalPrototypeQuantity(motorbike.Comp.FuelReagent);
        string? problem = null;
        if (availableFuel <= FixedPoint2.Zero)
            problem = "motorbike-refuel-no-fuel";
        else if (sourceSolution.Volume != availableFuel)
            problem = "motorbike-refuel-contaminated";
        else if (fuelSolution.AvailableVolume <= FixedPoint2.Zero)
            problem = "motorbike-refuel-full";

        if (problem == null)
            return null;

        _popup.PopupEntity(Loc.GetString(problem), motorbike, user);
        return true;
    }

    private bool TryRefuel(Entity<MotorbikeComponent> motorbike, EntityUid source, EntityUid user, FixedPoint2 maxAmount)
    {
        if (CheckFuelSource(motorbike, source, user) is { } handled)
            return handled;

        if (!_solution.TryGetDrainableSolution(source, out var sourceSoln, out var sourceSolution) ||
            !_solution.TryGetSolution(motorbike.Owner, motorbike.Comp.FuelSolution, out var fuelSoln, out var fuelSolution))
        {
            return false;
        }

        var availableFuel = sourceSolution.GetTotalPrototypeQuantity(motorbike.Comp.FuelReagent);
        var amount = FixedPoint2.Min(maxAmount, FixedPoint2.Min(availableFuel, fuelSolution.AvailableVolume));
        if (amount <= FixedPoint2.Zero)
            return false;

        var removed = _solution.RemoveReagent(sourceSoln.Value, motorbike.Comp.FuelReagent, amount);
        if (removed <= FixedPoint2.Zero)
            return false;

        _solution.TryAddReagent(fuelSoln.Value, motorbike.Comp.FuelReagent, removed, out var accepted);
        if (accepted < removed)
            _solution.TryAddReagent(sourceSoln.Value, motorbike.Comp.FuelReagent, removed - accepted, out _);

        if (accepted <= FixedPoint2.Zero)
            return false;

        _audio.PlayPvs(motorbike.Comp.RefillSound, motorbike.Owner);
        _popup.PopupEntity(Loc.GetString("motorbike-refuel-added", ("amount", accepted)), motorbike, user);
        _vehicle.RefreshCanRun(motorbike.Owner);
        return true;
    }

    private bool TryStartRefuelDoAfter(Entity<MotorbikeComponent> motorbike, EntityUid source, EntityUid user)
    {
        if (CheckFuelSource(motorbike, source, user) is { } handled)
            return handled;

        if (!IsReadyForRefuel(motorbike, user, popup: true))
            return true;

        if (motorbike.Comp.RefuelDoAfter != null)
        {
            _popup.PopupEntity(Loc.GetString("motorbike-refuel-busy"), motorbike, user);
            return true;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            motorbike.Comp.RefillDelay,
            new MotorbikeRefuelDoAfterEvent(),
            motorbike.Owner,
            target: motorbike.Owner,
            used: source)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
            return false;

        motorbike.Comp.RefuelDoAfter = doAfterId;
        _popup.PopupEntity(Loc.GetString("motorbike-refuel-start"), motorbike, user);
        return true;
    }

    /// <summary>
    /// The key has to be out (engine off) to refuel.
    /// </summary>
    private bool IsReadyForRefuel(Entity<MotorbikeComponent> motorbike, EntityUid user, bool popup)
    {
        if (!TryComp<GenericKeyedVehicleComponent>(motorbike, out var keyed) ||
            _itemSlots.GetItemOrNull(motorbike.Owner, keyed.ContainerId) == null)
        {
            return true;
        }

        if (popup)
            _popup.PopupEntity(Loc.GetString("motorbike-refuel-key-in"), motorbike, user);

        return false;
    }

    /// <summary>
    /// Burns fuel; false once the tank runs dry.
    /// </summary>
    private bool TryConsumeFuel(EntityUid uid, MotorbikeComponent motorbike, FixedPoint2 amount)
    {
        if (!_solution.TryGetSolution(uid, motorbike.FuelSolution, out var fuelSoln, out var fuelSolution))
            return false;

        var available = fuelSolution.GetTotalPrototypeQuantity(motorbike.FuelReagent);
        if (available <= FixedPoint2.Zero)
            return false;

        _solution.RemoveReagent(fuelSoln.Value, motorbike.FuelReagent, FixedPoint2.Min(amount, available));
        return available > amount;
    }

    private bool HasUsableFuel(EntityUid uid, MotorbikeComponent motorbike)
    {
        return _solution.TryGetSolution(uid, motorbike.FuelSolution, out _, out var fuelSolution) &&
            fuelSolution.GetTotalPrototypeQuantity(motorbike.FuelReagent) > FixedPoint2.Zero;
    }

    private void StartBurning(Entity<MotorbikeComponent> ent)
    {
        ent.Comp.Burning = true;
        ent.Comp.ExplodeAt = _timing.CurTime + ent.Comp.ExplosionDelay;
        Dirty(ent);

        _appearance.SetData(ent, MotorbikeVisuals.Burning, true);
        _audio.PlayPvs(ent.Comp.FuseSound, ent);
        _vehicle.RefreshCanRun(ent.Owner);

        if (TryComp(ent, out FlammableComponent? flammable))
            _flammable.Ignite(ent, ent, flammable);
    }

    private void Explode(EntityUid uid, MotorbikeComponent motorbike, VehicleComponent vehicle)
    {
        if (vehicle.Operator is { } rider)
            _buckle.Unbuckle(rider, null);

        _explosion.QueueExplosion(uid,
            motorbike.ExplosionType,
            motorbike.ExplosionTotalIntensity,
            motorbike.ExplosionSlope,
            motorbike.ExplosionMaxTileIntensity);

        QueueDel(uid);
    }
}
