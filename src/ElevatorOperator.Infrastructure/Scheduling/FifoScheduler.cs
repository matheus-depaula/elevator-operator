using System.Collections.Concurrent;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler<T> : IScheduler<T>
{
    private readonly ConcurrentQueue<T> _queue = new();

    /// <summary>Adds an item to the end of the FIFO queue. Thread-safe via ConcurrentQueue.</summary>
    /// <param name="item">The item to add to the queue.</param>
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
    }

    /// <summary>Removes and returns the next item from the front of the queue, or default if queue is empty. Thread-safe.</summary>
    /// <returns>The next item, or default if queue is empty.</returns>
    public T? GetNext()
    {
        return _queue.TryDequeue(out var next) ? next : default;
    }

    /// <summary>Peeks at the next item without removing it from the queue. Thread-safe.</summary>
    /// <returns>The next item without dequeuing, or default if queue is empty.</returns>
    public T? PeekNext()
    {
        return _queue.TryPeek(out var next) ? next : default;
    }

    /// <summary>Gets the current number of items pending in the queue.</summary>
    /// <returns>The count of items in the queue.</returns>
    public int GetPendingCount()
    {
        return _queue.Count;
    }
}
