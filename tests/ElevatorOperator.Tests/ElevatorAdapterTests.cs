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
        _innerElevator.CurrentFloor = 2;
        _innerElevator.State = ElevatorState.MovingUp;

        _adapter.MoveUp();

        _innerElevator.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void MoveDown_Should_Decrement_CurrentFloor_When_Possible()
    {
        _innerElevator.CurrentFloor = 5;
        _innerElevator.State = ElevatorState.MovingDown;

        _adapter.MoveDown();

        _innerElevator.CurrentFloor.Should().Be(4);
    }

    [Fact]
    public void MoveToTarget_Should_Reach_Target_Floor()
    {
        _innerElevator.CurrentFloor = 1;

        _adapter.MoveToTarget(4);

        _innerElevator.CurrentFloor.Should().Be(4);
        _innerElevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void MoveToTarget_Should_Move_Down_When_Target_Is_Lower()
    {
        _innerElevator.CurrentFloor = 6;

        _adapter.MoveToTarget(3);

        _innerElevator.CurrentFloor.Should().Be(3);
    }

    [Fact]
    public void OpenAndCloseDoor_Should_Update_State_Correctly()
    {
        _innerElevator.State = ElevatorState.Idle;

        _adapter.OpenDoor();
        _adapter.CloseDoor();

        _innerElevator.State.Should().Be(ElevatorState.Idle);
    }
}
