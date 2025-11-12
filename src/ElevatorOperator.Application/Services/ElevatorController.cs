using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Application.Services;

public class ElevatorController(IElevatorAdapter elevator, IScheduler<ElevatorRequest> scheduler, ILogger logger) : IElevatorController
{
    private const int MaxRetries = 1;
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);
    private readonly IElevatorAdapter Elevator = elevator;
    private readonly IScheduler<ElevatorRequest> _scheduler = scheduler;
    private readonly ILogger _logger = logger;
    private readonly object _lock = new();

    public void RequestElevator(int pickup, int destination)
    {
        lock (_lock)
        {
            if (!IsValidRequest(pickup, destination))
            {
                _logger.Warn($"Invalid request ignored: pickup {pickup}, destination {destination}. Valid floors: {Elevator.MinFloor}-{Elevator.MaxFloor}.");
                return;
            }

            _scheduler.Enqueue(new ElevatorRequest(pickup, destination));
            _logger.Info($"Received request: pickup at floor {pickup}, destination {destination}.");

            Monitor.Pulse(_lock);
        }
    }

    public void ProcessRequests(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_scheduler.GetPendingCount() == 0)
                {
                    Monitor.Wait(_lock, TimeSpan.FromMilliseconds(500));
                    continue;
                }

                try
                {
                    var request = _scheduler.GetNext();
                    if (request == null)
                        break;

                    HandleRequest(request);
                    Console.WriteLine();
                }
                catch (ElevatorOperatorException ex)
                {
                    _logger.Warn($"Elevator domain issue: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while processing request.", ex);
                }
            }
        }
    }


    private void HandleRequest(ElevatorRequest request)
    {
        var nextRequest = _scheduler.PeekNext();

        // Pickup phase
        ExecuteWithRetry(() => MoveToFloor(request.PickupFloor), "move to pickup");
        if (Elevator.CurrentFloor != request.PickupFloor)
            SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.PickupFloor);
        if (Elevator.State == ElevatorState.DoorOpen)
            SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.PickupFloor);

        // Destination phase
        ExecuteWithRetry(() => MoveToFloor(request.DestinationFloor), "move to destination");
        SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.DestinationFloor);

        bool shouldKeepDoorsOpen =
            nextRequest != null &&
            nextRequest.PickupFloor == request.DestinationFloor;

        if (shouldKeepDoorsOpen)
        {
            _logger.Info($"Keeping doors open at floor {request.DestinationFloor} for next pickup.");
        }
        else
        {
            SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.DestinationFloor);
        }
    }

    private void MoveToFloor(int floor)
    {
        if (Elevator.CurrentFloor == floor)
        {
            _logger.Info($"Already at floor {floor}.");
            return;
        }

        _logger.Info($"Moving to floor {floor}.");

        Elevator.MoveToFloor(floor);

        _logger.Info($"Reached floor {floor}.");
    }

    private void SafeDoorOperation(Action doorAction, DoorOperation operation, int floor)
    {
        ExecuteWithRetry(() =>
        {
            doorAction();
            _logger.Info($"{operation} doors at floor {floor}.");
        }, $"{operation} doors");
    }

    private void ExecuteWithRetry(Action action, string context)
    {
        for (int attempt = 1; attempt <= MaxRetries + 1; attempt++)
        {
            try
            {
                ExecuteWithTimeout(action);
                return;
            }
            catch (TimeoutException)
            {
                _logger.Warn($"Timeout during {context} (attempt {attempt}).");

                if (attempt <= MaxRetries)
                {
                    _logger.Info($"Retrying {context}...");
                    continue;
                }

                _logger.Error($"Failed to complete {context} after {attempt} attempts.");
                break;
            }
            catch (InvalidStateTransitionException ex)
            {
                _logger.Warn($"Skipped invalid operation: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during {context}.", ex);
                break;
            }
        }
    }

    private void ExecuteWithTimeout(Action action)
    {
        if (!TryRunWithTimeout(action, OperationTimeout))
            throw new TimeoutException("Operation timed out.");
    }

    private bool TryRunWithTimeout(Action action, TimeSpan timeout)
    {
        try
        {
            var task = Task.Run(action);
            return task.Wait(timeout);
        }
        catch (Exception ex)
        {
            _logger.Error("Error during elevator operation.", ex);
            return false;
        }
    }

    private bool IsValidRequest(int pickup, int destination)
    {
        return pickup != destination &&
               pickup >= Elevator.MinFloor && pickup <= Elevator.MaxFloor &&
               destination >= Elevator.MinFloor && destination <= Elevator.MaxFloor;
    }
}
