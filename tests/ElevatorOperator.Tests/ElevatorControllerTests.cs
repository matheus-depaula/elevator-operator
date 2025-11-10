using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Application.Services;
using ElevatorOperator.Domain.Adapters;
using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Interfaces;
using Moq;

namespace ElevatorOperator.Tests;

public class ElevatorControllerTests
{
    private readonly Mock<IElevator> _mockElevator;
    private readonly Mock<IScheduler> _mockScheduler;
    private readonly Mock<ILogger> _mockLogger;
    private readonly IElevatorController _controller;

    public ElevatorControllerTests()
    {
        _mockElevator = new Mock<IElevator>();
        _mockScheduler = new Mock<IScheduler>();
        _mockLogger = new Mock<ILogger>();

        _mockElevator.SetupGet(e => e.TargetFloors).Returns([]);
        _mockElevator.SetupProperty(e => e.CurrentFloor, 1);
        _mockElevator.SetupProperty(e => e.State, ElevatorState.Idle);

        var elevatorAdapter = new ElevatorAdapter(_mockElevator.Object);

        _controller = new ElevatorController(
            elevatorAdapter,
            _mockScheduler.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void RequestElevator_Should_Forward_To_Elevator()
    {
        _controller.RequestElevator(4);

        _mockElevator.Verify(e => e.AddRequest(4), Times.Once);
    }

    [Fact]
    public void ProcessRequests_Should_Move_Up_When_Target_Is_Higher()
    {
        var pendingFloors = new List<int> { 5 };
        _mockElevator.SetupGet(e => e.TargetFloors).Returns(pendingFloors);
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(2);

        _controller.ProcessRequests();

        _mockElevator.Verify(e => e.MoveUp(), Times.AtLeastOnce);
        _mockElevator.Verify(e => e.OpenDoor(), Times.Once);
        _mockElevator.Verify(e => e.CloseDoor(), Times.Once);
    }

    [Fact]
    public void ProcessRequests_Should_Move_Down_When_Target_Is_Lower()
    {
        var pendingFloors = new List<int> { 1 };
        _mockElevator.SetupGet(e => e.TargetFloors).Returns(pendingFloors);
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(5);

        _controller.ProcessRequests();

        _mockElevator.Verify(e => e.MoveDown(), Times.AtLeastOnce);
        _mockElevator.Verify(e => e.OpenDoor(), Times.Once);
        _mockElevator.Verify(e => e.CloseDoor(), Times.Once);
    }

    [Fact]
    public void ProcessRequests_Should_Do_Nothing_When_No_Requests()
    {
        _mockElevator.SetupGet(e => e.TargetFloors).Returns(new List<int>());

        _controller.ProcessRequests();

        _mockElevator.Verify(e => e.MoveUp(), Times.Never);
        _mockElevator.Verify(e => e.MoveDown(), Times.Never);
        _mockElevator.Verify(e => e.OpenDoor(), Times.Never);
    }
}
