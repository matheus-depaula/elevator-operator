using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Domain.ValueObjects;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Application.Services;

public class ElevatorController(IElevatorAdapter elevator, IScheduler scheduler, ILogger logger)
    : IElevatorController
{
    private readonly IElevatorAdapter Elevator = elevator;
    private readonly IScheduler _scheduler = scheduler;
    private readonly ILogger _logger = logger;
    private readonly Lock _syncLock = new();

    public void RequestElevator(int pickup, int destination)
    {
        lock (_syncLock)
        {
            var request = new ElevatorRequest(pickup, destination);
            _scheduler.Enqueue(request);

            _logger.Info($"Received request: pickup at floor {pickup}, destination {destination}.");
        }
    }

    public void ProcessRequests()
    {
        lock (_syncLock)
        {
            Console.WriteLine();
            while (_scheduler.GetPendingCount() > 0)
            {
                var request = _scheduler.GetNext();
                if (request == null)
                    break;

                var nextRequest = _scheduler.PeekNext();

                // Pickup phase
                MoveToFloor(request.PickupFloor);

                if (Elevator.CurrentFloor != request.PickupFloor)
                    SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.PickupFloor);
                if (Elevator.State == ElevatorState.DoorOpen)
                    SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.PickupFloor);

                // Destination phase
                MoveToFloor(request.DestinationFloor);
                SafeDoorOperation(Elevator.OpenDoor, DoorOperation.Opening, request.DestinationFloor);

                bool shouldKeepDoorsOpen =
                    nextRequest != null &&
                    nextRequest.PickupFloor == request.DestinationFloor;

                if (shouldKeepDoorsOpen)
                    _logger.Info($"Keeping doors open at floor {request.DestinationFloor} for next pickup.");
                else
                    SafeDoorOperation(Elevator.CloseDoor, DoorOperation.Closing, request.DestinationFloor);

                Console.WriteLine();
            }
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
        try
        {
            doorAction();
            _logger.Info($"{operation} doors at floor {floor}.");
        }
        catch (InvalidStateTransitionException)
        {
            _logger.Warn($"Skipped {operation.ToString().ToLower()} doors at floor {floor} (already in correct state).");
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while {operation.ToString().ToLower()} doors at floor {floor}.", ex);
        }
    }
}
