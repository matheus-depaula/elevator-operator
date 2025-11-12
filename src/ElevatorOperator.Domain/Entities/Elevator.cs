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

    /// <summary>Gets the current floor the elevator is on. Thread-safe.</summary>
    public int CurrentFloor
    {
        get { lock (_syncLock) return _currentFloor; }
        private set { lock (_syncLock) _currentFloor = value; }
    }

    /// <summary>Gets the current operational state. Thread-safe with validation on state transitions.</summary>
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

    /// <summary>Gets a snapshot of all requested target floors. Thread-safe.</summary>
    public IReadOnlyList<int> TargetFloors
    {
        get { lock (_syncLock) return [.. _targetFloors]; }
    }

    /// <summary>Moves elevator up one floor. Changes state to MovingUp, increments floor, simulates travel with cancellation support.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already at maximum floor.</exception>
    /// <exception cref="InvalidStateTransitionException">Thrown if state transition is invalid.</exception>
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

    /// <summary>Moves elevator down one floor. Changes state to MovingDown, decrements floor, simulates travel with cancellation support.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already at minimum floor.</exception>
    /// <exception cref="InvalidStateTransitionException">Thrown if state transition is invalid.</exception>
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

    /// <summary>Opens elevator doors. Changes state to DoorOpen, simulates door opening with cancellation support.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if elevator is not Idle.</exception>
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

    /// <summary>Closes elevator doors. Changes state from DoorOpen to Idle, simulates door closing with cancellation support.</summary>
    /// <exception cref="InvalidStateTransitionException">Thrown if doors are not open.</exception>
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

    /// <summary>Adds a floor request to the target floors list. No-op if floor already requested. Thread-safe.</summary>
    /// <param name="floor">The floor to add to requests.</param>
    /// <exception cref="InvalidFloorException">Thrown if floor is outside the valid range (1-10).</exception>
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

    /// <summary>
    /// Forces elevator to Idle state as a recovery mechanism for timeout/error states.
    /// Bypasses normal state validation to recover from stuck states.
    /// </summary>
    public void ForceRecoveryToIdle()
    {
        lock (_syncLock)
        {
            _state = ElevatorState.Idle;
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
