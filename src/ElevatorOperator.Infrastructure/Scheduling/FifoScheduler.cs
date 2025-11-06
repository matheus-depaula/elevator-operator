using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler : IElevatorScheduler
{
    public int? GetNextFloor(Queue<int> pendingRequests)
    {
        if (pendingRequests.Count == 0)
            return null;

        return pendingRequests.Dequeue();
    }
}
