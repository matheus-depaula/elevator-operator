using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Interfaces;

namespace ElevatorOperator.Domain.Adapters;

/// <summary>
/// Adapter that wraps the provided Elevator class and extends it
/// with improved domain behavior and validation logic.
/// </summary>
public class ElevatorAdapter(IElevator elevator) : IElevatorAdapter
{
    private readonly IElevator _inner = elevator ?? throw new ArgumentNullException(nameof(elevator));
    private readonly object _adapterLock = new();

    public int MinFloor
    {
        get { lock (_adapterLock) return _inner.MinFloor; }
    }

    public int MaxFloor
    {
        get { lock (_adapterLock) return _inner.MaxFloor; }
    }


    public int CurrentFloor
    {
        get { lock (_adapterLock) return _inner.CurrentFloor; }
    }

    public ElevatorState State
    {
        get { lock (_adapterLock) return _inner.State; }
    }

    public IReadOnlyList<int> TargetFloors
    {
        get { lock (_adapterLock) return _inner.TargetFloors; }
    }

    public void MoveUp(CancellationToken ct)
    {
        lock (_adapterLock)
        {
            _inner.MoveUp(ct);
        }
    }

    public void MoveDown(CancellationToken ct)
    {
        lock (_adapterLock)
        {
            _inner.MoveDown(ct);
        }
    }

    public void OpenDoor(CancellationToken ct)
    {
        lock (_adapterLock)
        {
            _inner.OpenDoor(ct);
        }
    }

    public void CloseDoor(CancellationToken ct)
    {
        lock (_adapterLock)
        {
            _inner.CloseDoor(ct);
        }
    }

    public void AddRequest(int floor)
    {
        ValidateFloor(floor);

        lock (_adapterLock)
        {
            _inner.AddRequest(floor);
        }
    }

    public void MoveToFloor(int floor, CancellationToken ct)
    {
        ValidateFloor(floor);

        lock (_adapterLock)
        {
            ct.ThrowIfCancellationRequested();

            if (floor == _inner.CurrentFloor)
                return;

            if (_inner.State == ElevatorState.DoorOpen)
                _inner.CloseDoor(ct);

            while (_inner.CurrentFloor != floor)
            {
                ct.ThrowIfCancellationRequested();

                if (floor > _inner.CurrentFloor)
                    _inner.MoveUp(ct);
                else
                    _inner.MoveDown(ct);
            }

            _inner.OpenDoor(ct);
            _inner.CloseDoor(ct);
        }
    }

    public void ForceRecoveryToIdle()
    {
        lock (_adapterLock)
        {
            _inner.ForceRecoveryToIdle();
        }
    }

    private void ValidateFloor(int floor)
    {
        if (floor < _inner.MinFloor || floor > _inner.MaxFloor)
            throw new InvalidFloorException(floor);
    }
}
