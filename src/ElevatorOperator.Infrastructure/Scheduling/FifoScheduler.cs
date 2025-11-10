using System.Collections.Concurrent;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler : IScheduler
{
    private readonly Lock _schedulerLock = new();
    private readonly ConcurrentQueue<ElevatorRequest> _requestQueue = new();

    public void Enqueue(ElevatorRequest request)
    {
        lock (_schedulerLock)
        {
            _requestQueue.Enqueue(request);
        }
    }

    public ElevatorRequest? GetNext()
    {
        lock (_schedulerLock)
        {
            if (_requestQueue.TryDequeue(out var next))
            {
                return next;
            }

            return null;
        }
    }

    public ElevatorRequest? PeekNext()
    {
        lock (_schedulerLock)
        {
            if (_requestQueue.TryPeek(out var next))
            {
                return next;
            }

            return null;
        }
    }

    public int GetPendingCount()
    {
        lock (_schedulerLock)
        {
            return _requestQueue.Count;
        }
    }

    public void Clear()
    {
        lock (_schedulerLock)
        {
            while (_requestQueue.TryDequeue(out _)) { }
        }
    }
}
