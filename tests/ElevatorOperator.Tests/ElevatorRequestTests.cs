using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.ValueObjects;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class ElevatorRequestTests
{
    #region Constructor and Validation

    [Fact]
    public void Constructor_Should_Initialize_Valid_Request()
    {
        var request = new ElevatorRequest(3, 7);

        request.PickupFloor.Should().Be(3);
        request.DestinationFloor.Should().Be(7);
    }

    [Fact]
    public void Constructor_With_Equal_Floors_Should_Throw()
    {
        Action act = () => new ElevatorRequest(5, 5);

        act.Should().Throw<InvalidPickupAndDestinationException>();
    }

    [Fact]
    public void Constructor_With_Equal_Floors_All_Combinations()
    {
        for (int floor = 1; floor <= 10; floor++)
        {
            Action act = () => new ElevatorRequest(floor, floor);
            act.Should().Throw<InvalidPickupAndDestinationException>();
        }
    }

    [Fact]
    public void Constructor_Exception_Message_Should_Contain_Floors()
    {
        Action act = () => new ElevatorRequest(5, 5);

        act.Should().Throw<InvalidPickupAndDestinationException>()
            .WithMessage("*5*5*");
    }

    #endregion

    #region Direction Inference

    [Fact]
    public void Direction_Should_Be_Up_When_Destination_Is_Higher()
    {
        var request = new ElevatorRequest(3, 7);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Direction_Should_Be_Down_When_Destination_Is_Lower()
    {
        var request = new ElevatorRequest(7, 3);
        request.Direction.Should().Be(ElevatorDirection.Down);
    }

    [Fact]
    public void Direction_Up_For_Adjacent_Floors_Ascending()
    {
        var request = new ElevatorRequest(5, 6);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Direction_Down_For_Adjacent_Floors_Descending()
    {
        var request = new ElevatorRequest(6, 5);
        request.Direction.Should().Be(ElevatorDirection.Down);
    }

    [Fact]
    public void Direction_Up_For_Extreme_Range()
    {
        var request = new ElevatorRequest(1, 10);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Direction_Down_For_Extreme_Range()
    {
        var request = new ElevatorRequest(10, 1);
        request.Direction.Should().Be(ElevatorDirection.Down);
    }

    [Fact]
    public void Direction_Inference_All_Combinations()
    {
        for (int pickup = 1; pickup <= 10; pickup++)
        {
            for (int destination = 1; destination <= 10; destination++)
            {
                if (pickup == destination)
                    continue; // Skip equal floors

                var request = new ElevatorRequest(pickup, destination);

                if (destination > pickup)
                {
                    request.Direction.Should().Be(ElevatorDirection.Up,
                        $"Direction should be Up for pickup={pickup}, destination={destination}");
                }
                else
                {
                    request.Direction.Should().Be(ElevatorDirection.Down,
                        $"Direction should be Down for pickup={pickup}, destination={destination}");
                }
            }
        }
    }

    #endregion

    #region Properties

    [Fact]
    public void PickupFloor_Is_Readable()
    {
        var request = new ElevatorRequest(3, 7);
        request.PickupFloor.Should().Be(3);
    }

    [Fact]
    public void DestinationFloor_Is_Readable()
    {
        var request = new ElevatorRequest(3, 7);
        request.DestinationFloor.Should().Be(7);
    }

    [Fact]
    public void Direction_Is_Readable()
    {
        var request = new ElevatorRequest(3, 7);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Properties_Are_ReadOnly()
    {
        var request = new ElevatorRequest(3, 7);

        // Properties should not have setters
        var pickupProperty = typeof(ElevatorRequest).GetProperty(nameof(ElevatorRequest.PickupFloor));
        pickupProperty.Should().NotBeNull();
        pickupProperty!.CanWrite.Should().BeFalse();

        var destinationProperty = typeof(ElevatorRequest).GetProperty(nameof(ElevatorRequest.DestinationFloor));
        destinationProperty.Should().NotBeNull();
        destinationProperty!.CanWrite.Should().BeFalse();

        var directionProperty = typeof(ElevatorRequest).GetProperty(nameof(ElevatorRequest.Direction));
        directionProperty.Should().NotBeNull();
        directionProperty!.CanWrite.Should().BeFalse();
    }

    #endregion

    #region Value Object Semantics

    [Fact]
    public void Two_Requests_With_Same_Values_Should_Have_Same_ToString()
    {
        var request1 = new ElevatorRequest(3, 7);
        var request2 = new ElevatorRequest(3, 7);

        // Value objects should represent the same data
        request1.PickupFloor.Should().Be(request2.PickupFloor);
        request1.DestinationFloor.Should().Be(request2.DestinationFloor);
        request1.Direction.Should().Be(request2.Direction);
    }

    [Fact]
    public void Two_Requests_With_Different_Values_Should_Differ()
    {
        var request1 = new ElevatorRequest(3, 7);
        var request2 = new ElevatorRequest(4, 7);

        request1.PickupFloor.Should().NotBe(request2.PickupFloor);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void Request_With_Floor_1_As_Pickup()
    {
        var request = new ElevatorRequest(1, 5);
        request.PickupFloor.Should().Be(1);
    }

    [Fact]
    public void Request_With_Floor_10_As_Destination()
    {
        var request = new ElevatorRequest(1, 10);
        request.DestinationFloor.Should().Be(10);
    }

    [Fact]
    public void Request_With_Floor_10_As_Pickup()
    {
        var request = new ElevatorRequest(10, 1);
        request.PickupFloor.Should().Be(10);
    }

    [Fact]
    public void Request_With_Floor_1_As_Destination()
    {
        var request = new ElevatorRequest(10, 1);
        request.DestinationFloor.Should().Be(1);
    }

    [Fact]
    public void All_Valid_Floor_Combinations()
    {
        int validCount = 0;

        for (int pickup = 1; pickup <= 10; pickup++)
        {
            for (int destination = 1; destination <= 10; destination++)
            {
                if (pickup != destination)
                {
                    var request = new ElevatorRequest(pickup, destination);
                    request.Should().NotBeNull();
                    validCount++;
                }
            }
        }

        // Should be (10*10 - 10) = 90 valid combinations
        validCount.Should().Be(90);
    }

    #endregion

    #region Exception Behavior

    [Fact]
    public void Invalid_Request_Exception_Should_Be_ElevatorOperatorException()
    {
        Action act = () => new ElevatorRequest(5, 5);

        act.Should().Throw<ElevatorOperatorException>();
    }

    [Fact]
    public void Invalid_Request_Exception_Should_Be_InvalidPickupAndDestinationException()
    {
        Action act = () => new ElevatorRequest(5, 5);

        act.Should().Throw<InvalidPickupAndDestinationException>();
    }

    [Fact]
    public void Exception_Message_Should_Be_Informative()
    {
        Action act = () => new ElevatorRequest(7, 7);

        var exception = act.Should().Throw<InvalidPickupAndDestinationException>().Which;
        exception.Message.Should().Contain("Pair cannot be the same");
    }

    #endregion

    #region Typical Usage Scenarios

    [Theory]
    [InlineData(1, 10)]
    [InlineData(5, 1)]
    [InlineData(3, 7)]
    [InlineData(10, 5)]
    public void Various_Valid_Requests(int pickup, int destination)
    {
        var request = new ElevatorRequest(pickup, destination);

        request.PickupFloor.Should().Be(pickup);
        request.DestinationFloor.Should().Be(destination);
        request.Direction.Should().BeOneOf(ElevatorDirection.Up, ElevatorDirection.Down);
    }

    [Fact]
    public void Request_For_Going_Up_One_Floor()
    {
        var request = new ElevatorRequest(5, 6);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Request_For_Going_Down_One_Floor()
    {
        var request = new ElevatorRequest(6, 5);
        request.Direction.Should().Be(ElevatorDirection.Down);
    }

    [Fact]
    public void Request_For_Large_Jump_Up()
    {
        var request = new ElevatorRequest(2, 9);
        request.Direction.Should().Be(ElevatorDirection.Up);
    }

    [Fact]
    public void Request_For_Large_Jump_Down()
    {
        var request = new ElevatorRequest(9, 2);
        request.Direction.Should().Be(ElevatorDirection.Down);
    }

    #endregion
}
