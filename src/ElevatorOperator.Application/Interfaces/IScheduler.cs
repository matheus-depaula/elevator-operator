using ElevatorOperator.Domain.ValueObjects;

namespace ElevatorOperator.Application.Interfaces;

public interface IScheduler<T>
{
    void Enqueue(T request);
    T? GetNext();
    T? PeekNext();
    int GetPendingCount();
}
