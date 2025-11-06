namespace ElevatorOperator.Application.Interfaces;

public interface IElevatorScheduler
{
    int? GetNextFloor(Queue<int> pendingRequests);
}
