using ElevatorOperator.Domain.Adapters;
using ElevatorOperator.Domain.Entities;
using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class ElevatorAdapterTests
{
    private readonly ElevatorAdapter _adapter;
    private readonly Elevator _innerElevator;
    private readonly CancellationToken _ct = CancellationToken.None;

    public ElevatorAdapterTests()
    {
        _innerElevator = new Elevator();
        _adapter = new ElevatorAdapter(_innerElevator);
    }

    [Fact]
    public void AddRequest_Should_Add_Floor_When_Valid()
    {
        _adapter.AddRequest(3);

        _innerElevator.TargetFloors.Should().Contain(3);
    }

    [Fact]
    public void AddRequest_Should_Throw_When_Floor_Invalid()
    {
        Action act = () => _adapter.AddRequest(0);

        act.Should().Throw<InvalidFloorException>();
        _innerElevator.TargetFloors.Should().BeEmpty();
    }

    [Fact]
    public void MoveUp_Should_Increment_CurrentFloor_When_Possible()
    {
        _adapter.AddRequest(3);
        _adapter.MoveUp(_ct);

        _innerElevator.CurrentFloor.Should().Be(2);
    }

    [Fact]
    public void MoveDown_Should_Decrement_CurrentFloor_When_Possible()
    {
        _adapter.AddRequest(1);
        _adapter.MoveUp(_ct);
        _adapter.MoveUp(_ct);
        _adapter.MoveUp(_ct);
        _adapter.MoveDown(_ct);

        _innerElevator.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void MoveToFloor_Should_Reach_Target_Floor()
    {
        _adapter.MoveToFloor(4, _ct);

        _innerElevator.CurrentFloor.Should().Be(4);
        _innerElevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void MoveToFloor_Should_Move_Down_When_Target_Is_Lower()
    {
        _adapter.MoveToFloor(6, _ct);
        _adapter.MoveToFloor(3, _ct);

        _innerElevator.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void AddRequest_Should_Throw_For_Floor_Above_Max()
    {
        Action act = () => _adapter.AddRequest(11);

        act.Should().Throw<InvalidFloorException>();
    }
}
