using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class ElevatorOperatorExceptionTests
{
    #region Base Exception Tests

    [Fact]
    public void ElevatorOperatorException_Should_Inherit_From_Exception()
    {
        var ex = new ElevatorOperatorException("Test message");
        ex.Should().BeOfType<ElevatorOperatorException>();
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ElevatorOperatorException_Constructor_With_Message()
    {
        var message = "Test error message";
        var ex = new ElevatorOperatorException(message);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void ElevatorOperatorException_Constructor_With_Message_And_Inner()
    {
        var innerException = new InvalidOperationException("Inner error");
        var message = "Outer error";

        var ex = new ElevatorOperatorException(message, innerException);

        ex.Message.Should().Be(message);
        ex.InnerException.Should().Be(innerException);
    }

    #endregion

    #region InvalidFloorException Tests

    [Fact]
    public void InvalidFloorException_Should_Inherit_From_ElevatorOperatorException()
    {
        var ex = new InvalidFloorException(0);
        ex.Should().BeOfType<InvalidFloorException>();
        ex.Should().BeAssignableTo<ElevatorOperatorException>();
    }

    [Fact]
    public void InvalidFloorException_Should_Contain_Floor_In_Message()
    {
        var ex = new InvalidFloorException(0);
        ex.Message.Should().Contain("0");
    }

    [Fact]
    public void InvalidFloorException_Message_Should_Mention_Valid_Range()
    {
        var ex = new InvalidFloorException(11);
        ex.Message.Should().Contain("1").And.Contain("10");
    }

    [Fact]
    public void InvalidFloorException_For_Floor_Below_Min()
    {
        var ex = new InvalidFloorException(0);
        ex.Message.Should().Contain("Invalid floor");
    }

    [Fact]
    public void InvalidFloorException_For_Floor_Above_Max()
    {
        var ex = new InvalidFloorException(11);
        ex.Message.Should().Contain("Invalid floor");
    }

    [Fact]
    public void InvalidFloorException_For_Negative_Floor()
    {
        var ex = new InvalidFloorException(-5);
        ex.Message.Should().Contain("-5");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(11)]
    [InlineData(100)]
    public void InvalidFloorException_Various_Invalid_Floors(int floor)
    {
        var ex = new InvalidFloorException(floor);
        ex.Message.Should().Contain(floor.ToString());
        ex.Should().BeOfType<InvalidFloorException>();
    }

    #endregion

    #region InvalidPickupAndDestinationException Tests

    [Fact]
    public void InvalidPickupAndDestinationException_Should_Inherit_From_ElevatorOperatorException()
    {
        var ex = new InvalidPickupAndDestinationException(5, 5);
        ex.Should().BeOfType<InvalidPickupAndDestinationException>();
        ex.Should().BeAssignableTo<ElevatorOperatorException>();
    }

    [Fact]
    public void InvalidPickupAndDestinationException_Should_Contain_Pickup_Floor()
    {
        var ex = new InvalidPickupAndDestinationException(5, 5);
        ex.Message.Should().Contain("5");
    }

    [Fact]
    public void InvalidPickupAndDestinationException_Should_Contain_Destination_Floor()
    {
        var ex = new InvalidPickupAndDestinationException(3, 3);
        ex.Message.Should().Contain("3");
    }

    [Fact]
    public void InvalidPickupAndDestinationException_Message_Format()
    {
        var ex = new InvalidPickupAndDestinationException(4, 4);
        ex.Message.Should().Contain("Pickup").And.Contain("Destination");
    }

    [Fact]
    public void InvalidPickupAndDestinationException_Should_Mention_Cannot_Be_Same()
    {
        var ex = new InvalidPickupAndDestinationException(5, 5);
        ex.Message.Should().Contain("cannot be the same");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public void InvalidPickupAndDestinationException_Various_Floors(int pickup, int destination)
    {
        var ex = new InvalidPickupAndDestinationException(pickup, destination);
        ex.Message.Should().Contain(pickup.ToString()).And.Contain(destination.ToString());
    }

    #endregion

    #region InvalidStateTransitionException Tests

    [Fact]
    public void InvalidStateTransitionException_Should_Inherit_From_ElevatorOperatorException()
    {
        var ex = new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp);
        ex.Should().BeOfType<InvalidStateTransitionException>();
        ex.Should().BeAssignableTo<ElevatorOperatorException>();
    }

    [Fact]
    public void InvalidStateTransitionException_Should_Contain_Current_State()
    {
        var ex = new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp);
        ex.Message.Should().Contain("Idle");
    }

    [Fact]
    public void InvalidStateTransitionException_Should_Contain_Target_State()
    {
        var ex = new InvalidStateTransitionException(ElevatorState.DoorOpen, ElevatorState.MovingUp);
        ex.Message.Should().Contain("MovingUp");
    }

    [Fact]
    public void InvalidStateTransitionException_Message_Format()
    {
        var ex = new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.DoorOpen);
        ex.Message.Should().Contain("Invalid state transition");
    }

    [Theory]
    [InlineData(ElevatorState.Idle, ElevatorState.MovingUp)]
    [InlineData(ElevatorState.Idle, ElevatorState.MovingDown)]
    [InlineData(ElevatorState.Idle, ElevatorState.DoorOpen)]
    [InlineData(ElevatorState.MovingUp, ElevatorState.DoorOpen)]
    [InlineData(ElevatorState.MovingDown, ElevatorState.MovingUp)]
    [InlineData(ElevatorState.DoorOpen, ElevatorState.MovingUp)]
    public void InvalidStateTransitionException_Various_Transitions(
        ElevatorState current, ElevatorState target)
    {
        var ex = new InvalidStateTransitionException(current, target);
        ex.Message.Should().Contain(current.ToString()).And.Contain(target.ToString());
    }

    #endregion

    #region ElevatorTimeoutException Tests

    [Fact]
    public void ElevatorTimeoutException_Should_Inherit_From_ElevatorOperatorException()
    {
        var ex = new ElevatorTimeoutException("move");
        ex.Should().BeOfType<ElevatorTimeoutException>();
        ex.Should().BeAssignableTo<ElevatorOperatorException>();
    }

    [Fact]
    public void ElevatorTimeoutException_Should_Contain_Operation_Name()
    {
        var ex = new ElevatorTimeoutException("move to floor");
        ex.Message.Should().Contain("move to floor");
    }

    [Fact]
    public void ElevatorTimeoutException_Message_Should_Mention_Timeout()
    {
        var ex = new ElevatorTimeoutException("door opening");
        ex.Message.Should().Contain("timed out");
    }

    [Theory]
    [InlineData("move")]
    [InlineData("door opening")]
    [InlineData("door closing")]
    [InlineData("add request")]
    public void ElevatorTimeoutException_Various_Operations(string operation)
    {
        var ex = new ElevatorTimeoutException(operation);
        ex.Message.Should().Contain(operation);
    }

    #endregion

    #region Exception Hierarchy

    [Fact]
    public void All_Custom_Exceptions_Inherit_From_Base()
    {
        var exceptions = new Exception[]
        {
            new InvalidFloorException(0),
            new InvalidPickupAndDestinationException(5, 5),
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp),
            new ElevatorTimeoutException("test")
        };

        foreach (var ex in exceptions)
        {
            ex.Should().BeAssignableTo<ElevatorOperatorException>();
        }
    }

    [Fact]
    public void All_Custom_Exceptions_Inherit_From_Exception()
    {
        var exceptions = new Exception[]
        {
            new InvalidFloorException(0),
            new InvalidPickupAndDestinationException(5, 5),
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp),
            new ElevatorTimeoutException("test")
        };

        foreach (var ex in exceptions)
        {
            ex.Should().BeAssignableTo<Exception>();
        }
    }

    #endregion

    #region Exception Throwing Scenarios

    [Fact]
    public void Exception_Can_Be_Caught_As_ElevatorOperatorException()
    {
        Action act = () => throw new InvalidFloorException(0);

        act.Should().Throw<ElevatorOperatorException>();
    }

    [Fact]
    public void Exception_Can_Be_Caught_As_Specific_Type()
    {
        Action act = () => throw new InvalidFloorException(0);

        act.Should().Throw<InvalidFloorException>();
    }

    [Fact]
    public void Specific_Exception_Cannot_Catch_Other_Type()
    {
        Action act = () => throw new InvalidPickupAndDestinationException(5, 5);

        // Should not be caught as InvalidFloorException
        act.Should().NotThrow<InvalidFloorException>();
        // But should be caught as base type
        act.Should().Throw<ElevatorOperatorException>();
    }

    #endregion

    #region Message Content Tests

    [Fact]
    public void All_Exceptions_Should_Have_Non_Empty_Messages()
    {
        var exceptions = new Exception[]
        {
            new InvalidFloorException(0),
            new InvalidPickupAndDestinationException(5, 5),
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp),
            new ElevatorTimeoutException("operation")
        };

        foreach (var ex in exceptions)
        {
            ex.Message.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Exception_Messages_Should_Be_Descriptive()
    {
        var ex1 = new InvalidFloorException(15);
        ex1.Message.Length.Should().BeGreaterThan(10);

        var ex2 = new InvalidPickupAndDestinationException(5, 5);
        ex2.Message.Length.Should().BeGreaterThan(10);

        var ex3 = new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp);
        ex3.Message.Length.Should().BeGreaterThan(10);

        var ex4 = new ElevatorTimeoutException("operation");
        ex4.Message.Length.Should().BeGreaterThan(5);
    }

    #endregion

    #region Serialization Support

    [Fact]
    public void Exception_Can_Be_Converted_To_String()
    {
        var ex = new InvalidFloorException(0);
        var str = ex.ToString();
        str.Should().Contain("InvalidFloorException");
    }

    [Fact]
    public void All_Exceptions_Can_Be_Converted_To_String()
    {
        var exceptions = new Exception[]
        {
            new InvalidFloorException(0),
            new InvalidPickupAndDestinationException(5, 5),
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp),
            new ElevatorTimeoutException("test")
        };

        foreach (var ex in exceptions)
        {
            var str = ex.ToString();
            str.Should().NotBeNullOrEmpty();
        }
    }

    #endregion
}
