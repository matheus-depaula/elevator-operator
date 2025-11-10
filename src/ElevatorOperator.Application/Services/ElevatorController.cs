using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;

namespace ElevatorOperator.Application.Services;

public class ElevatorController(IElevatorAdapter elevator, IScheduler scheduler, ILogger logger) : IElevatorController
{
    private readonly IElevatorAdapter Elevator = elevator;
    private readonly IScheduler _scheduler = scheduler;
    private readonly ILogger _logger = logger;
    private readonly Lock _syncLock = new();

    public void RequestElevator(int floor)
    {
        lock (_syncLock)
        {
            var request = new ElevatorRequest(floor);

            _scheduler.Enqueue(request);

            _logger.Info($"Received request for floor {floor}");
        }
    }

    public void ProcessRequests()
    {
        lock (_syncLock)
        {
            while (_scheduler.GetPendingCount() > 0)
            {
                var nextRequest = _scheduler.GetNext();
                if (nextRequest == null)
                    break;

                var targetFloor = nextRequest.Value.Floor;

                _logger.Info($"Processing request for floor {targetFloor}");

                Elevator.MoveToFloor(targetFloor);

                _logger.Info($"Arrived at floor {targetFloor}");
            }
        }
    }
}
