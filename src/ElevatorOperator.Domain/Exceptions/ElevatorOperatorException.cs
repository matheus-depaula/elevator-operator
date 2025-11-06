using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Domain.Exceptions;

public class ElevatorOperatorException : Exception
{
    public ElevatorOperatorException(string message) : base(message) { }
    public ElevatorOperatorException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidFloorException(int floor) : ElevatorOperatorException($"Invalid floor requested: {floor}. Floor must be between 1 and 10.")
{
}

public class InvalidStateTransitionException(ElevatorState current, ElevatorState requested) : ElevatorOperatorException($"Invalid state transition from {current} to {requested}")
{
}

public class ElevatorTimeoutException(string operation) : ElevatorOperatorException($"Operation timed out: {operation}")
{
}
