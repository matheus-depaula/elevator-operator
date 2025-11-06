using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Domain.Interfaces;

public interface IElevator
{
    int CurrentFloor { get; set; }
    ElevatorState State { get; set; }
    List<int> TargetFloors { get; set; }
    void MoveUp();
    void MoveDown();
    void OpenDoor();
    void CloseDoor();
    void AddRequest(int floor);
}
