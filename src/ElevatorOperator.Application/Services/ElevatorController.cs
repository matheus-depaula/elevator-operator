using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Domain.Entities;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Application.Services;

public class ElevatorController : IElevatorController
{
    public IElevator Elevator = new Elevator();
    public void ProcessRequests()
    {
        while (this.Elevator.TargetFloors.Count > 0)
        {
            var targetFloor = this.Elevator.TargetFloors[0];
            ElevatorDirection direction = targetFloor > this.Elevator.CurrentFloor ? ElevatorDirection.Up : ElevatorDirection.Down;

            Console.WriteLine($"[ElevatorController] Moving to floor {targetFloor}");

            while (this.Elevator.CurrentFloor != targetFloor)
            {
                if (direction == ElevatorDirection.Up)
                {
                    this.Elevator.State = ElevatorState.MovingUp;
                    this.Elevator.MoveUp();
                }
                else
                {
                    this.Elevator.State = ElevatorState.MovingDown;
                    this.Elevator.MoveDown();
                }
            }

            Console.WriteLine($"[ElevatorController] Arrived at floor {targetFloor}");
            this.Elevator.State = ElevatorState.Idle;
            Console.WriteLine($"[ElevatorController] Opening the door");
            this.Elevator.OpenDoor();
            Console.WriteLine($"[ElevatorController] Closing the door");
            this.Elevator.CloseDoor();

            this.Elevator.TargetFloors.RemoveAt(0);
        }
    }
    public void RequestElevator(int floor)
    {
        this.Elevator.AddRequest(floor);
    }
}
