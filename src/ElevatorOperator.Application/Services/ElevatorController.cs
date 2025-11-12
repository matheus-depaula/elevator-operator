using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Application.Services;

public class ElevatorController(IElevatorAdapter elevator, IScheduler<ElevatorRequest> scheduler, ILogger logger) : IElevatorController, IDisposable
{
    private const int MaxRetries = 1;
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(5);
    private readonly IElevatorAdapter Elevator = elevator;
    private readonly IScheduler<ElevatorRequest> _scheduler = scheduler;
    private readonly ILogger _logger = logger;
    private readonly object _lock = new();
    private volatile bool _isProcessing = false;
    private readonly AutoResetEvent _doorOperationComplete = new(true);

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
        if (Interlocked.CompareExchange(ref _isProcessing, true, false))
            throw new InvalidOperationException("ProcessRequests is already running. Only one instance can process requests at a time.");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ElevatorRequest? request = null;

                lock (_lock)
                {
                    request = _scheduler.GetNext();

                    if (request == null)
                    {
                        Monitor.Wait(_lock, TimeSpan.FromMilliseconds(500));
                        continue;
                    }
                }

                try
                {
                    HandleRequest(request);
                }
                catch (ElevatorOperatorException ex)
                {
                    _logger.Warn($"Elevator domain issue: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Unexpected error while processing request.", ex);
                }

                Console.WriteLine();

            }
        }
        finally
        {
            _isProcessing = false;
        }
    }


    private void HandleRequest(ElevatorRequest request)
    {
        var nextRequest = _scheduler.PeekNext();

        // Pickup phase
        ExecuteWithRetry(() => MoveToFloor(request.PickupFloor), "move to pickup");
        SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.PickupFloor);
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
        _doorOperationComplete.Reset();

        try
        {
            if ((operation == DoorOperation.Opening && Elevator.State == ElevatorState.DoorOpen) ||
                (operation == DoorOperation.Closing && Elevator.State == ElevatorState.Idle))
            {
                _logger.Info($"Skipped redundant {operation.ToString().ToLower()} doors at floor {floor} (already in correct state).");
                return;
            }

            ExecuteWithRetry(() =>
            {
                doorAction();
                _logger.Info($"{operation} doors at floor {floor}.");
            }, $"{operation} doors");
        }
        catch (InvalidStateTransitionException)
        {
            _logger.Warn($"Skipped invalid {operation.ToString().ToLower()} doors at floor {floor} (state conflict).");
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while {operation.ToString().ToLower()} doors at floor {floor}.", ex);
        }
        finally
        {
            _doorOperationComplete.Set();
        }
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

    public void Dispose()
    {
        _doorOperationComplete?.Dispose();
        GC.SuppressFinalize(this);
    }
}
