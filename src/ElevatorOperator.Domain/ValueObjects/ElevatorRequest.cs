using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;

namespace ElevatorOperator.Domain.ValueObjects;

public readonly record struct ElevatorRequest
{
    public int Floor { get; }
    public ElevatorDirection Direction { get; }
    public DateTime RequestTime { get; }

    public ElevatorRequest(int floor, ElevatorDirection direction)
    {
        if (floor < 1 || floor > 10)
            throw new InvalidFloorException(floor);

        Floor = floor;
        Direction = direction;
        RequestTime = DateTime.UtcNow;
    }
}
