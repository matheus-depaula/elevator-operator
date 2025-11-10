using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Interfaces;

namespace ElevatorOperator.Domain.Entities;

public class Elevator : IElevator
{
    public const int TravelDelayMs = 300;
    private const int DoorDelayMs = 500;

    private readonly Lock _syncLock = new();
    private readonly List<int> _targetFloors = [];
    private ElevatorState _state = ElevatorState.Idle;
    private int _currentFloor;

    public virtual int MinFloor => 1;
    public virtual int MaxFloor => 10;

    public Elevator()
    {
        _currentFloor = MinFloor;
    }

    public int CurrentFloor
    {
        get { lock (_syncLock) return _currentFloor; }
        private set { lock (_syncLock) _currentFloor = value; }
    }

    public ElevatorState State
    {
        get { lock (_syncLock) return _state; }
        private set
        {
            lock (_syncLock)
            {
                if (!IsValidStateTransition(_state, value))
                    throw new InvalidStateTransitionException(_state, value);
                _state = value;
            }
        }
    }

    public IReadOnlyList<int> TargetFloors
    {
        get { lock (_syncLock) return [.. _targetFloors]; }
    }

    public void MoveUp()
    {
        lock (_syncLock)
        {
            if (CurrentFloor >= MaxFloor)
                throw new InvalidOperationException($"Cannot move up from floor {CurrentFloor}");

            if (State != ElevatorState.Idle && State != ElevatorState.MovingUp)
                throw new InvalidStateTransitionException(State, ElevatorState.MovingUp);

            State = ElevatorState.MovingUp;

            _currentFloor++;
        }

        Thread.Sleep(TravelDelayMs); // Simulate travel time (not under lock)

        lock (_syncLock)
        {
            State = ElevatorState.Idle;
        }
    }

    public void MoveDown()
    {
        lock (_syncLock)
        {
            if (CurrentFloor <= MinFloor)
                throw new InvalidOperationException($"Cannot move down from floor {CurrentFloor}");

            if (State != ElevatorState.Idle && State != ElevatorState.MovingDown)
                throw new InvalidStateTransitionException(State, ElevatorState.MovingDown);

            State = ElevatorState.MovingDown;

            _currentFloor--;
        }

        Thread.Sleep(TravelDelayMs);  // Simulate travel time (not under lock)

        lock (_syncLock)
        {
            State = ElevatorState.Idle;
        }
    }

    public void OpenDoor()
    {
        lock (_syncLock)
        {
            if (State != ElevatorState.Idle)
                throw new InvalidStateTransitionException(State, ElevatorState.DoorOpen);

            State = ElevatorState.DoorOpen;
        }

        Thread.Sleep(DoorDelayMs); // Simulate door opening time (not under lock)
    }

    public void CloseDoor()
    {
        lock (_syncLock)
        {
            if (State != ElevatorState.DoorOpen)
                throw new InvalidStateTransitionException(State, ElevatorState.Idle);

            State = ElevatorState.Idle;
        }

        Thread.Sleep(DoorDelayMs); // Simulate door closing time (not under lock)
    }

    public void AddRequest(int floor)
    {
        if (floor < MinFloor || floor > MaxFloor)
            throw new InvalidFloorException(floor);

        lock (_syncLock)
        {
            if (!_targetFloors.Contains(floor))
                _targetFloors.Add(floor);
        }
    }

    private static bool IsValidStateTransition(ElevatorState current, ElevatorState target)
    {
        return (current, target) switch
        {
            (ElevatorState.Idle, ElevatorState.MovingUp) => true,
            (ElevatorState.Idle, ElevatorState.MovingDown) => true,
            (ElevatorState.Idle, ElevatorState.DoorOpen) => true,
            (ElevatorState.MovingUp, ElevatorState.Idle) => true,
            (ElevatorState.MovingDown, ElevatorState.Idle) => true,
            (ElevatorState.DoorOpen, ElevatorState.Idle) => true,
            (var same, var other) when same == other => true,
            _ => false
        };
    }
}
