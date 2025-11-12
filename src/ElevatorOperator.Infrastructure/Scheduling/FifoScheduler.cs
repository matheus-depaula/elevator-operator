using System.Collections.Concurrent;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler<T> : IScheduler<T>
{
    private readonly ConcurrentQueue<T> _queue = new();

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
    }

    public T? GetNext()
    {
        return _queue.TryDequeue(out var next) ? next : default;
    }

    public T? PeekNext()
    {
        return _queue.TryPeek(out var next) ? next : default;
    }

    public int GetPendingCount()
    {
        return _queue.Count;
    }
}
