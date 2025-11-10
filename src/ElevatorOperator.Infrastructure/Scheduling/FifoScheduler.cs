using System.Collections.Concurrent;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler<T> : IScheduler<T>
{
    private readonly Lock _lock = new(); // Lock should be object, not Lock struct
    private readonly ConcurrentQueue<T> _queue = new();

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            _queue.Enqueue(item);
        }
    }

    public T? GetNext()
    {
        lock (_lock)
        {
            return _queue.TryDequeue(out var next) ? next : default;
        }
    }

    public T? PeekNext()
    {
        lock (_lock)
        {
            return _queue.TryPeek(out var next) ? next : default;
        }
    }

    public int GetPendingCount()
    {
        lock (_lock)
        {
            return _queue.Count;
        }
    }
}
