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

    /// <summary>Enqueues a passenger request for elevator service. Validates floors and signals the processing thread.</summary>
    /// <param name="pickup">The floor where the passenger is waiting.</param>
    /// <param name="destination">The floor where the passenger wants to go.</param>
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

    /// <summary>Processes all enqueued requests. Runs indefinitely until cancellation is requested. Only one instance can run at a time.</summary>
    /// <param name="ct">Cancellation token to stop processing.</param>
    /// <exception cref="InvalidOperationException">Thrown if ProcessRequests is already running.</exception>
    public void ProcessRequests(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _isProcessing, true, false))
            throw new InvalidOperationException("ProcessRequests is already running. Only one instance can process requests at a time.");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
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

                    HandleRequest(request, ct);
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
        finally
        {
            _isProcessing = false;
        }
    }


    /// <summary>Orchestrates handling of a single elevator request: moves to pickup, opens/closes doors, moves to destination, with door optimization if next pickup is at destination.</summary>
    /// <param name="request">The elevator request with pickup and destination floors.</param>
    /// <param name="ct">Cancellation token to stop operation.</param>
    private void HandleRequest(ElevatorRequest request, CancellationToken ct)
    {
        var nextRequest = _scheduler.PeekNext();

        // Pickup phase
        ExecuteWithRetry(() => MoveToFloor(request.PickupFloor, ct), "move to pickup", ct);
        SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.PickupFloor, ct);
        SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.PickupFloor, ct);

        // Destination phase
        ExecuteWithRetry(() => MoveToFloor(request.DestinationFloor, ct), "move to destination", ct);
        SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.DestinationFloor, ct);

        bool shouldKeepDoorsOpen =
            nextRequest != null &&
            nextRequest.PickupFloor == request.DestinationFloor;

        if (shouldKeepDoorsOpen)
        {
            _logger.Info($"Keeping doors open at floor {request.DestinationFloor} for next pickup.");
        }
        else
        {
            SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.DestinationFloor, ct);
        }
    }

    /// <summary>Moves elevator to specified floor with logging. Logs if already at floor.</summary>
    /// <param name="floor">The target floor to reach.</param>
    /// <param name="ct">Cancellation token to stop operation.</param>
    private void MoveToFloor(int floor, CancellationToken ct)
    {
        if (Elevator.CurrentFloor == floor)
        {
            _logger.Info($"Already at floor {floor}.");
            return;
        }

        _logger.Info($"Moving to floor {floor}.");

        Elevator.MoveToFloor(floor, ct);

        _logger.Info($"Reached floor {floor}.");
    }

    /// <summary>Safely executes door operations (open/close) with retry logic and state validation. Skips redundant operations and handles exceptions gracefully.</summary>
    /// <param name="doorAction">The door action to execute (e.g., OpenDoor, CloseDoor).</param>
    /// <param name="operation">The door operation type for logging.</param>
    /// <param name="floor">The floor where the operation occurs.</param>
    /// <param name="ct">Cancellation token to stop operation.</param>
    private void SafeDoorOperation(Action<CancellationToken> doorAction, DoorOperation operation, int floor, CancellationToken ct)
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
                doorAction(ct);
                _logger.Info($"{operation} doors at floor {floor}.");
            }, $"{operation} doors", ct);
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


    /// <summary>Executes an action with timeout and retry logic. On timeout, forces recovery to Idle state and retries up to MaxRetries times.</summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Description of the action for logging purposes.</param>
    /// <param name="ct">Cancellation token to stop operation.</param>
    private void ExecuteWithRetry(Action action, string context, CancellationToken ct)
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

                try
                {
                    RecoverFromTimeout();
                }
                catch (Exception recoveryEx)
                {
                    _logger.Error("Recovery from timeout failed.", recoveryEx);
                }

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

    /// <summary>
    /// Recovers elevator from timeout state by forcing it to a known safe state (Idle).
    /// This prevents stuck elevators in intermediate states like MovingUp/MovingDown/DoorOpen.
    /// </summary>
    private void RecoverFromTimeout()
    {
        _logger.Warn("Attempting to recover elevator from timeout state to Idle.");

        try
        {
            Elevator.ForceRecoveryToIdle();
            _logger.Info("Elevator forced to Idle state for recovery.");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to force elevator to Idle state during recovery.", ex);
            throw;
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
