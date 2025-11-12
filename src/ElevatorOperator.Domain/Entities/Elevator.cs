using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Interfaces;

namespace ElevatorOperator.Domain.Entities;

public class Elevator : IElevator
{
    public const int TravelDelayMs = 300;
    private const int DoorDelayMs = 500;

    private readonly object _syncLock = new();
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

    public void MoveUp(CancellationToken ct)
    {
        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();

            if (CurrentFloor >= MaxFloor)
                throw new InvalidOperationException($"Cannot move up from floor {CurrentFloor}");

            if (State != ElevatorState.Idle && State != ElevatorState.MovingUp)
                throw new InvalidStateTransitionException(State, ElevatorState.MovingUp);

            State = ElevatorState.MovingUp;
            _currentFloor++;
        }

        Task.Delay(TravelDelayMs, ct).Wait(ct);

        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();
            State = ElevatorState.Idle;
        }
    }

    public void MoveDown(CancellationToken ct)
    {
        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();

            if (CurrentFloor <= MinFloor)
                throw new InvalidOperationException($"Cannot move down from floor {CurrentFloor}");

            if (State != ElevatorState.Idle && State != ElevatorState.MovingDown)
                throw new InvalidStateTransitionException(State, ElevatorState.MovingDown);

            State = ElevatorState.MovingDown;
            _currentFloor--;
        }

        Task.Delay(TravelDelayMs, ct).Wait(ct);

        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();
            State = ElevatorState.Idle;
        }
    }

    public void OpenDoor(CancellationToken ct)
    {
        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();

            if (State != ElevatorState.Idle)
                throw new InvalidStateTransitionException(State, ElevatorState.DoorOpen);

            State = ElevatorState.DoorOpen;
        }

        Task.Delay(DoorDelayMs, ct).Wait(ct);

        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();
        }
    }

    public void CloseDoor(CancellationToken ct)
    {
        lock (_syncLock)
        {
            ct.ThrowIfCancellationRequested();

            if (State != ElevatorState.DoorOpen)
                throw new InvalidStateTransitionException(State, ElevatorState.Idle);

            State = ElevatorState.Idle;
        }

        Task.Delay(DoorDelayMs, ct).Wait(ct);
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
