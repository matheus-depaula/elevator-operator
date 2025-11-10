using System.Collections.Concurrent;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;

namespace ElevatorOperator.Infrastructure.Scheduling;

public class FifoScheduler : IScheduler
{
    private readonly ConcurrentQueue<ElevatorRequest> _requestQueue = new();

    public void Enqueue(ElevatorRequest request)
    {
        _requestQueue.Enqueue(request);
    }

    public ElevatorRequest? GetNext()
    {
        if (_requestQueue.TryDequeue(out var next))
        {
            return next;
        }

        return null;
    }

    public int GetPendingCount()
    {
        return _requestQueue.Count;
    }

    public void Clear()
    {
        while (_requestQueue.TryDequeue(out _)) { }
    }
}
