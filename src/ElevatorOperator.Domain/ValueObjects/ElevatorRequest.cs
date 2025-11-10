using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;

namespace ElevatorOperator.Domain.ValueObjects;

public class ElevatorRequest
{
    public int PickupFloor { get; }
    public int DestinationFloor { get; }
    public ElevatorDirection Direction { get; }

    public ElevatorRequest(int pickupFloor, int destinationFloor)
    {
        if (pickupFloor == destinationFloor)
            throw new InvalidPickupAndDestinationException(pickupFloor, destinationFloor);

        PickupFloor = pickupFloor;
        DestinationFloor = destinationFloor;
        Direction = destinationFloor > pickupFloor ? ElevatorDirection.Up : ElevatorDirection.Down;
    }
}
