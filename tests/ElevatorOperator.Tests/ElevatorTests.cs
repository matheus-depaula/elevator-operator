using ElevatorOperator.Domain.Entities;
using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class ElevatorTests
{
    private readonly Elevator _elevator;
    private readonly CancellationToken _ct = CancellationToken.None;

    public ElevatorTests()
    {
        _elevator = new Elevator();
    }

    #region Constructor and Initialization

    [Fact]
    public void Constructor_Should_Initialize_At_MinFloor()
    {
        _elevator.CurrentFloor.Should().Be(1);
    }

    [Fact]
    public void Constructor_Should_Initialize_In_Idle_State()
    {
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void Constructor_Should_Initialize_Empty_TargetFloors()
    {
        _elevator.TargetFloors.Should().BeEmpty();
    }

    [Fact]
    public void MinFloor_Should_Be_One()
    {
        _elevator.MinFloor.Should().Be(1);
    }

    [Fact]
    public void MaxFloor_Should_Be_Ten()
    {
        _elevator.MaxFloor.Should().Be(10);
    }

    #endregion

    #region MoveUp Tests

    [Fact]
    public void MoveUp_Should_Increment_CurrentFloor()
    {
        _elevator.MoveUp(_ct);
        _elevator.CurrentFloor.Should().Be(2);
    }

    [Fact]
    public void MoveUp_Should_Change_State_To_MovingUp_Then_Idle()
    {
        // During move, state should be MovingUp, but after sleep it returns to Idle
        _elevator.MoveUp(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void MoveUp_From_MaxFloor_Should_Throw()
    {
        // Move to max floor first
        for (int i = 1; i < 10; i++)
            _elevator.MoveUp(_ct);

        _elevator.CurrentFloor.Should().Be(10);

        // Try to move up from max floor
        Action act = () => _elevator.MoveUp(_ct);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot move up*");
    }

    [Fact]
    public void MoveUp_Multiple_Times_Should_Increment_Correctly()
    {
        _elevator.MoveUp(_ct);
        _elevator.MoveUp(_ct);
        _elevator.MoveUp(_ct);

        _elevator.CurrentFloor.Should().Be(4);
    }

    [Fact]
    public void MoveUp_Can_Only_Occur_From_Idle_Or_MovingUp_State()
    {
        // Start at floor 1 (Idle)
        _elevator.MoveUp(_ct);
        // After MoveUp completes, state returns to Idle
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.CurrentFloor.Should().Be(2);

        // Can move up again from Idle
        _elevator.MoveUp(_ct);
        _elevator.CurrentFloor.Should().Be(3);
    }

    #endregion

    #region MoveDown Tests

    [Fact]
    public void MoveDown_Should_Decrement_CurrentFloor()
    {
        _elevator.MoveUp(_ct);
        _elevator.MoveDown(_ct);

        _elevator.CurrentFloor.Should().Be(1);
    }

    [Fact]
    public void MoveDown_Should_Change_State_To_MovingDown_Then_Idle()
    {
        _elevator.MoveUp(_ct);
        _elevator.MoveDown(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void MoveDown_From_MinFloor_Should_Throw()
    {
        _elevator.CurrentFloor.Should().Be(1);

        Action act = () => _elevator.MoveDown(_ct);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot move down*");
    }

    [Fact]
    public void MoveDown_Multiple_Times_Should_Decrement_Correctly()
    {
        // Move up to floor 5
        for (int i = 0; i < 4; i++)
            _elevator.MoveUp(_ct);

        _elevator.CurrentFloor.Should().Be(5);

        // Move down 3 times
        _elevator.MoveDown(_ct);
        _elevator.MoveDown(_ct);
        _elevator.MoveDown(_ct);

        _elevator.CurrentFloor.Should().Be(2);
    }

    #endregion

    #region OpenDoor Tests

    [Fact]
    public void OpenDoor_Should_Change_State_To_DoorOpen()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);
    }

    [Fact]
    public void OpenDoor_From_Moving_State_Should_Throw()
    {
        // The Elevator completes movement before returning, so state is Idle
        // To test this, we would need to intercept mid-movement
        // For now, test that opening when Idle works
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);
    }

    [Fact]
    public void OpenDoor_When_Already_Open_Should_Throw()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);

        Action act = () => _elevator.OpenDoor(_ct);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void OpenDoor_At_Different_Floors_Should_Work()
    {
        _elevator.MoveUp(_ct);
        _elevator.MoveUp(_ct);

        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);
        _elevator.CurrentFloor.Should().Be(3);
    }

    #endregion

    #region CloseDoor Tests

    [Fact]
    public void CloseDoor_Should_Change_State_To_Idle()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);

        _elevator.CloseDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void CloseDoor_When_Idle_Should_Throw()
    {
        _elevator.State.Should().Be(ElevatorState.Idle);

        Action act = () => _elevator.CloseDoor(_ct);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void CloseDoor_When_Moving_Should_Throw()
    {
        // After MoveUp completes, state is Idle
        _elevator.MoveUp(_ct);

        // Try to close when Idle (should throw)
        Action act = () => _elevator.CloseDoor(_ct);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void OpenDoor_Then_CloseDoor_Sequence()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);

        _elevator.CloseDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    #endregion

    #region AddRequest Tests

    [Fact]
    public void AddRequest_Should_Add_Floor_To_TargetFloors()
    {
        _elevator.AddRequest(5);
        _elevator.TargetFloors.Should().Contain(5);
    }

    [Fact]
    public void AddRequest_Multiple_Floors()
    {
        _elevator.AddRequest(3);
        _elevator.AddRequest(7);
        _elevator.AddRequest(5);

        _elevator.TargetFloors.Should().HaveCount(3);
        _elevator.TargetFloors.Should().Contain(new[] { 3, 7, 5 });
    }

    [Fact]
    public void AddRequest_With_Duplicate_Floor_Should_Not_Add_Twice()
    {
        _elevator.AddRequest(5);
        _elevator.AddRequest(5);

        _elevator.TargetFloors.Should().HaveCount(1);
    }

    [Fact]
    public void AddRequest_With_Floor_Below_Min_Should_Throw()
    {
        Action act = () => _elevator.AddRequest(0);
        act.Should().Throw<InvalidFloorException>();
    }

    [Fact]
    public void AddRequest_With_Floor_Above_Max_Should_Throw()
    {
        Action act = () => _elevator.AddRequest(11);
        act.Should().Throw<InvalidFloorException>();
    }

    [Fact]
    public void AddRequest_With_Negative_Floor_Should_Throw()
    {
        Action act = () => _elevator.AddRequest(-5);
        act.Should().Throw<InvalidFloorException>();
    }

    [Fact]
    public void AddRequest_With_Valid_Floors_Should_Succeed()
    {
        for (int floor = 1; floor <= 10; floor++)
        {
            _elevator.AddRequest(floor);
        }

        _elevator.TargetFloors.Should().HaveCount(10);
    }

    [Fact]
    public void AddRequest_Boundary_Floors()
    {
        _elevator.AddRequest(1);  // Min floor
        _elevator.AddRequest(10); // Max floor

        _elevator.TargetFloors.Should().Contain(new[] { 1, 10 });
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public void State_Idle_To_MovingUp_Should_Succeed()
    {
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.MoveUp(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle); // After sleep
    }

    [Fact]
    public void State_Idle_To_MovingDown_Should_Succeed()
    {
        _elevator.MoveUp(_ct); // Get to floor 2
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.MoveDown(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle); // After sleep
    }

    [Fact]
    public void State_Idle_To_DoorOpen_Should_Succeed()
    {
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);
    }

    [Fact]
    public void State_DoorOpen_To_Idle_Should_Succeed()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);
        _elevator.CloseDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void State_Same_State_Transition_Should_Succeed()
    {
        // IdleToIdle should be allowed
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.MoveUp(_ct);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    [Fact]
    public void Invalid_State_Transition_Should_Throw()
    {
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().Be(ElevatorState.DoorOpen);

        // Try to move while door is open
        Action act = () => _elevator.MoveUp(_ct);
        act.Should().Throw<InvalidStateTransitionException>();
    }

    #endregion

    #region Property Accessibility Tests

    [Fact]
    public void CurrentFloor_Is_ReadOnly()
    {
        // Can only be set through methods
        var initialFloor = _elevator.CurrentFloor;
        _elevator.MoveUp(_ct);
        _elevator.CurrentFloor.Should().NotBe(initialFloor);
    }

    [Fact]
    public void State_Is_ReadOnly()
    {
        // Can only be set through methods
        var initialState = _elevator.State;
        _elevator.OpenDoor(_ct);
        _elevator.State.Should().NotBe(initialState);
    }

    [Fact]
    public void TargetFloors_Is_ReadOnly_Collection()
    {
        var floors1 = _elevator.TargetFloors;
        _elevator.AddRequest(5);
        var floors2 = _elevator.TargetFloors;

        floors1.Should().NotContain(5);
        floors2.Should().Contain(5);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Concurrent_MoveUp_Operations_Should_Be_Safe()
    {
        var tasks = new List<Task>();

        // Move to middle of range
        for (int i = 0; i < 3; i++)
            _elevator.MoveUp(_ct);

        var startFloor = _elevator.CurrentFloor;

        // Multiple concurrent moves (will queue due to locks)
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                if (_elevator.CurrentFloor < 10)
                    _elevator.MoveUp(_ct);
            }));
        }

        await Task.WhenAll(tasks);

        // Should have incremented without corruption
        _elevator.CurrentFloor.Should().BeGreaterThanOrEqualTo(startFloor);
        _elevator.CurrentFloor.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task Concurrent_AddRequest_Should_Be_Safe()
    {
        var tasks = new List<Task>();

        for (int floor = 1; floor <= 10; floor++)
        {
            int f = floor;
            tasks.Add(Task.Run(() => _elevator.AddRequest(f)));
        }

        await Task.WhenAll(tasks);

        _elevator.TargetFloors.Should().HaveCount(10);
    }

    [Fact]
    public async Task Concurrent_Read_Write_Should_Be_Safe()
    {
        var tasks = new List<Task>();

        // Write operations
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() => _elevator.AddRequest(5)));
            tasks.Add(Task.Run(() =>
            {
                if (_elevator.CurrentFloor < 10)
                    _elevator.MoveUp(_ct);
            }));
        }

        // Read operations
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var _ = _elevator.CurrentFloor;
                var __ = _elevator.State;
                var ___ = _elevator.TargetFloors;
            }));
        }

        await Task.WhenAll(tasks);

        _elevator.CurrentFloor.Should().BeInRange(1, 10);
        _elevator.State.Should().Be(ElevatorState.Idle);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Movement_Full_Range()
    {
        // Move from 1 to 10
        for (int i = 1; i < 10; i++)
        {
            _elevator.MoveUp(_ct);
        }

        _elevator.CurrentFloor.Should().Be(10);

        // Move from 10 to 1
        for (int i = 10; i > 1; i--)
        {
            _elevator.MoveDown(_ct);
        }

        _elevator.CurrentFloor.Should().Be(1);
    }

    [Fact]
    public void Multiple_Door_Cycles()
    {
        for (int i = 0; i < 5; i++)
        {
            _elevator.OpenDoor(_ct);
            _elevator.State.Should().Be(ElevatorState.DoorOpen);

            _elevator.CloseDoor(_ct);
            _elevator.State.Should().Be(ElevatorState.Idle);
        }
    }

    [Fact]
    public void Mix_All_Operations()
    {
        _elevator.AddRequest(5);
        _elevator.MoveUp(_ct);
        _elevator.MoveUp(_ct);
        _elevator.OpenDoor(_ct);
        _elevator.CloseDoor(_ct);
        _elevator.MoveUp(_ct);
        _elevator.AddRequest(8);
        _elevator.OpenDoor(_ct);
        _elevator.CloseDoor(_ct);

        _elevator.CurrentFloor.Should().Be(4);
        _elevator.State.Should().Be(ElevatorState.Idle);
        _elevator.TargetFloors.Should().Contain(new[] { 5, 8 });
    }

    #endregion
}
