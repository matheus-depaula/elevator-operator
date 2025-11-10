using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Domain.Interfaces;

public interface IElevator
{
    int MinFloor { get; }
    int MaxFloor { get; }
    int CurrentFloor { get; }
    ElevatorState State { get; }
    IReadOnlyList<int> TargetFloors { get; }
    void MoveUp();
    void MoveDown();
    void OpenDoor();
    void CloseDoor();
    void AddRequest(int floor);
}
