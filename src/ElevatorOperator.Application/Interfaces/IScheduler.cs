using ElevatorOperator.Domain.ValueObjects;

namespace ElevatorOperator.Application.Interfaces;

public interface IScheduler
{
    void Enqueue(ElevatorRequest request);
    ElevatorRequest? GetNext();
    ElevatorRequest? PeekNext();
    int GetPendingCount();
    void Clear();
}
