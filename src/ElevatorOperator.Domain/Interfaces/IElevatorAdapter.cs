namespace ElevatorOperator.Domain.Interfaces;

/// <summary>Elevator adapter with high-level convenience methods and validation.</summary>
public interface IElevatorAdapter : IElevator
{
    /// <summary>Moves elevator to target floor step-by-step, opens and closes doors. Validates floor range and handles state transitions with cancellation support.</summary>
    /// <param name="floor">Target floor (must be between MinFloor and MaxFloor).</param>
    /// <param name="ct">Cancellation token to stop operation.</param>
    /// <exception cref="InvalidFloorException">Thrown if floor is outside valid range.</exception>
    void MoveToFloor(int floor, CancellationToken ct);
}
