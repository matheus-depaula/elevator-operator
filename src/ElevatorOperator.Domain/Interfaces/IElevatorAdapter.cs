namespace ElevatorOperator.Domain.Interfaces;

public interface IElevatorAdapter : IElevator
{
    void MoveToFloor(int floor);
}
