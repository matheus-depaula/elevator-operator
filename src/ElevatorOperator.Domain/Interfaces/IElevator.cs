using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Domain.Interfaces;

public interface IElevator
{
    /// <summary>Gets the minimum floor the elevator can access.</summary>
    int MinFloor { get; }

    /// <summary>Gets the maximum floor the elevator can access.</summary>
    int MaxFloor { get; }

    /// <summary>Gets the current floor where the elevator is located.</summary>
    int CurrentFloor { get; }

    /// <summary>Gets the current operational state of the elevator.</summary>
    ElevatorState State { get; }

    /// <summary>Gets a read-only list of requested target floors.</summary>
    IReadOnlyList<int> TargetFloors { get; }

    /// <summary>Moves elevator up one floor with cancellation support. Respects CancellationToken for clean shutdown.</summary>
    /// <param name="ct">Cancellation token to stop operation.</param>
    void MoveUp(CancellationToken ct);

    /// <summary>Moves elevator down one floor with cancellation support. Respects CancellationToken for clean shutdown.</summary>
    /// <param name="ct">Cancellation token to stop operation.</param>
    void MoveDown(CancellationToken ct);

    /// <summary>Opens elevator doors with cancellation support. Respects CancellationToken for clean shutdown.</summary>
    /// <param name="ct">Cancellation token to stop operation.</param>
    void OpenDoor(CancellationToken ct);

    /// <summary>Closes elevator doors with cancellation support. Respects CancellationToken for clean shutdown.</summary>
    /// <param name="ct">Cancellation token to stop operation.</param>
    void CloseDoor(CancellationToken ct);

    /// <summary>Adds a floor request to the elevator's target list if not already present.</summary>
    /// <param name="floor">Floor number to add (must be between MinFloor and MaxFloor).</param>
    /// <exception cref="InvalidFloorException">Thrown if floor is outside valid range.</exception>
    void AddRequest(int floor);

    /// <summary>Forces elevator to Idle state for timeout/error recovery. Bypasses validation to recover from stuck states.</summary>
    void ForceRecoveryToIdle();
}
