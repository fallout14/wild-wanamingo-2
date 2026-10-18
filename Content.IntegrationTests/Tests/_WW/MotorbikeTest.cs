#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._Goobstation.Vehicles;
using Content.Shared._Goobstation.Vehicles;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._WW;

/// <summary>
/// Misfits' motorbikes on Wizden's vehicles: one runs only with its key in and fuel in the tank, and catches fire (and
/// stops) once it's wrecked.
/// </summary>
[TestOf(typeof(MotorbikeSystem))]
public sealed class MotorbikeTest : GameTest
{
    private static readonly EntProtoId Bike = "MisfitsVehicleBikeGreen";
    private static readonly EntProtoId Key = "MisfitsVehicleKeyMotorbike";
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";

    [Test]
    public async Task KeyFuelAndBurning()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var map = await Pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var vehicles = entMan.System<VehicleSystem>();
            var bike = entMan.SpawnEntity(Bike, map.GridCoords);
            var mover = entMan.GetComponent<InputMoverComponent>(bike);
            vehicles.RefreshCanRun(bike);
            Assert.That(mover.CanMove, Is.False, "no key, no ride");

            var key = entMan.SpawnEntity(Key, map.GridCoords);
            Assert.That(entMan.System<ItemSlotsSystem>().TryInsert(bike, "key_slot", key, null));
            Assert.That(mover.CanMove, Is.False, "the tank starts empty");

            var solutions = entMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(bike, "motorbikeFuel", out var tank, out _));
            Assert.That(solutions.TryAddReagent(tank!.Value, "WeldingFuel", FixedPoint2.New(50)));
            vehicles.RefreshCanRun(bike);
            Assert.That(mover.CanMove, "key in and fuelled, it runs");

            var blunt = server.ProtoMan.Index(Blunt);
            entMan.System<DamageableSystem>().TryChangeDamage(bike, new DamageSpecifier(blunt, FixedPoint2.New(1000)), true);
            Assert.That(entMan.GetComponent<MotorbikeComponent>(bike).Burning, "a wrecked motorbike catches fire");
            Assert.That(mover.CanMove, Is.False, "and stops running");

            entMan.DeleteEntity(bike);
        });
    }
}
