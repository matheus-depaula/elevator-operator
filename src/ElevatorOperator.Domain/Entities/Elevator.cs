using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Interfaces;

namespace ElevatorOperator.Domain.Entities;


public class Elevator : IElevator
{
    const int MinFloor = 1;
    const int MaxFloor = 10;
    public int CurrentFloor { get; set; } = MinFloor;
    public ElevatorState State { get; set; } = ElevatorState.Idle;
    public List<int> TargetFloors { get; set; } = [];
    public void MoveUp()
    {
        if (this.State == ElevatorState.MovingUp && this.CurrentFloor < MaxFloor)
        {
            this.CurrentFloor++;
        }
    }
    public void MoveDown()
    {
        if (this.State == ElevatorState.MovingDown && this.CurrentFloor > MinFloor)
        {
            this.CurrentFloor--;
        }
    }
    public void OpenDoor()
    {
        if (this.State == ElevatorState.Idle)
        {
            this.State = ElevatorState.DoorOpen;
        }
    }
    public void CloseDoor()
    {
        if (this.State == ElevatorState.DoorOpen)
        {
            this.State = ElevatorState.Idle;
        }
    }
    public void AddRequest(int floor)
    {
        if (!this.TargetFloors.Contains(floor))
        {
            this.TargetFloors.Add(floor);
        }
    }
}
