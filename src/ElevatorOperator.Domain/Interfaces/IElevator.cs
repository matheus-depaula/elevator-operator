using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Domain.Interfaces;

public interface IElevator
{
    int MinFloor { get; }
    int MaxFloor { get; }
    int CurrentFloor { get; }
    ElevatorState State { get; }
    IReadOnlyList<int> TargetFloors { get; }
    void MoveUp(CancellationToken ct);
    void MoveDown(CancellationToken ct);
    void OpenDoor(CancellationToken ct);
    void CloseDoor(CancellationToken ct);
    void AddRequest(int floor);
}
