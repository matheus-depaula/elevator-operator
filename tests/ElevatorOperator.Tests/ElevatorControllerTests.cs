using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Application.Services;
using ElevatorOperator.Domain.Adapters;
using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.Exceptions;
using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace ElevatorOperator.Tests;

public class ElevatorControllerTests
{
    private readonly Mock<IElevator> _mockElevator;
    private readonly Mock<IScheduler<ElevatorRequest>> _mockScheduler;
    private readonly Mock<ILogger> _mockLogger;
    private readonly IElevatorController _controller;

    public ElevatorControllerTests()
    {
        _mockElevator = new Mock<IElevator>();
        _mockScheduler = new Mock<IScheduler<ElevatorRequest>>();
        _mockLogger = new Mock<ILogger>();

        _mockElevator.SetupGet(e => e.TargetFloors).Returns([]);
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(1);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);
        _mockElevator.SetupGet(e => e.MinFloor).Returns(1);
        _mockElevator.SetupGet(e => e.MaxFloor).Returns(10);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>())).Verifiable();
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>())).Verifiable();
        _mockElevator.Setup(e => e.OpenDoor(It.IsAny<CancellationToken>())).Verifiable();
        _mockElevator.Setup(e => e.CloseDoor(It.IsAny<CancellationToken>())).Verifiable();

        var elevatorAdapter = new ElevatorAdapter(_mockElevator.Object);

        _controller = new ElevatorController(
            elevatorAdapter,
            _mockScheduler.Object,
            _mockLogger.Object
        );
    }

    #region RequestElevator Tests

    [Fact]
    public void RequestElevator_Should_Enqueue_Valid_Request()
    {
        // Act
        _controller.RequestElevator(4, 7);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Once);
        _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void RequestElevator_Should_Ignore_Invalid_Request_Pickup_Below_Min()
    {
        // Act
        _controller.RequestElevator(0, 5);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Never);
        _mockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RequestElevator_Should_Ignore_Invalid_Request_Pickup_Above_Max()
    {
        // Act
        _controller.RequestElevator(11, 5);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Never);
        _mockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RequestElevator_Should_Ignore_Invalid_Request_Destination_Below_Min()
    {
        // Act
        _controller.RequestElevator(5, 0);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Never);
    }

    [Fact]
    public void RequestElevator_Should_Ignore_Invalid_Request_Destination_Above_Max()
    {
        // Act
        _controller.RequestElevator(5, 11);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Never);
    }

    [Fact]
    public void RequestElevator_Should_Ignore_Invalid_Request_Pickup_Equals_Destination()
    {
        // Act
        _controller.RequestElevator(5, 5);

        // Assert
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.Never);
    }

    [Fact]
    public async Task RequestElevator_Should_Not_Throw_On_Concurrent_Requests()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int pickup = (i % 9) + 1;
            int destination = ((i + 1) % 10) + 1;
            if (pickup == destination) destination = (destination % 10) + 1;

            tasks.Add(Task.Run(() => _controller.RequestElevator(pickup, destination)));
        }

        // Act & Assert - should not throw
        await Task.WhenAll(tasks);
        _mockScheduler.Verify(s => s.Enqueue(It.IsAny<ElevatorRequest>()), Times.AtLeast(5));
    }

    #endregion

    #region ProcessRequests Tests

    [Fact]
    public void ProcessRequests_Should_Move_Up_When_Target_Is_Higher()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        // Cancel immediately after a short delay to let the test run
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockElevator.Verify(e => e.MoveUp(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Move_Down_When_Target_Is_Lower()
    {
        // Arrange
        var request = new ElevatorRequest(5, 1);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 5;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockElevator.Verify(e => e.MoveDown(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Do_Nothing_When_No_Requests()
    {
        // Arrange
        _mockScheduler.Setup(s => s.GetPendingCount()).Returns(0);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockElevator.Verify(e => e.MoveUp(It.IsAny<CancellationToken>()), Times.Never);
        _mockElevator.Verify(e => e.MoveDown(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ProcessRequests_Should_Stop_When_Cancelled()
    {
        // Arrange
        _mockScheduler.Setup(s => s.GetPendingCount()).Returns(0);
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockScheduler.Verify(s => s.GetPendingCount(), Times.Never);
    }

    [Fact]
    public void ProcessRequests_Should_Handle_ElevatorOperatorException()
    {
        // Arrange
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);

        // Setup elevator to throw invalid state transition
        _mockElevator.Setup(e => e.OpenDoor(It.IsAny<CancellationToken>())).Throws(() =>
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp));

        var cts = new CancellationTokenSource();
        Task.Delay(10).ContinueWith(_ => cts.Cancel());

        // Act & Assert - should not throw
        _controller.Invoking(c => c.ProcessRequests(cts.Token)).Should().NotThrow();
    }

    [Fact]
    public void ProcessRequests_Should_Handle_Generic_Exception()
    {
        // Arrange
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Throws<ArgumentException>()
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act & Assert - should not throw
        _controller.Invoking(c => c.ProcessRequests(cts.Token)).Should().NotThrow();
        _mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Open_Doors_At_Pickup()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockElevator.Verify(e => e.OpenDoor(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Close_Doors_At_Destination_When_No_Next_Request()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockElevator.Verify(e => e.CloseDoor(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Keep_Doors_Open_When_Next_Pickup_Matches_Destination()
    {
        // Arrange
        var request1 = new ElevatorRequest(2, 5);
        var request2 = new ElevatorRequest(5, 8); // Next pickup is at same floor as request1 destination

        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request1)
            .Returns(request2)
            .Returns((ElevatorRequest?)null);

        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(2)  // First call during first iteration
            .Returns(1)  // After first request processed
            .Returns(1)  // Second iteration
            .Returns(0); // After second request processed

        _mockScheduler.SetupSequence(s => s.PeekNext())
            .Returns(request2) // During first request processing
            .Returns((ElevatorRequest?)null); // During second request processing

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(100).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockLogger.Verify(
            l => l.Info(It.Is<string>(s => s.Contains("Keeping doors open"))),
            Times.AtLeastOnce);
    }

    #endregion

    #region Retry and Timeout Tests

    [Fact]
    public void ProcessRequests_Should_Retry_Failed_Operations()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        // Simulate timeout on first attempt, then succeed
        int callCount = 0;
        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new TimeoutException();
                currentFloor++;
            });
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(100).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert
        _mockLogger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("Timeout"))), Times.AtLeastOnce);
        _mockLogger.Verify(l => l.Info(It.Is<string>(s => s.Contains("Retrying"))), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessRequests_Should_Handle_Invalid_State_Transitions()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        _mockElevator.Setup(e => e.OpenDoor(It.IsAny<CancellationToken>())).Throws(() =>
            new InvalidStateTransitionException(ElevatorState.Idle, ElevatorState.MovingUp));

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act & Assert
        _controller.Invoking(c => c.ProcessRequests(cts.Token)).Should().NotThrow();
    }

    [Fact]
    public void ProcessRequests_Should_Log_Operation_Errors()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Throws<InvalidOperationException>();
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act & Assert
        _controller.Invoking(c => c.ProcessRequests(cts.Token)).Should().NotThrow();
        _mockLogger.Verify(l => l.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Should_Handle_Complete_Request_Lifecycle()
    {
        // Arrange
        var request = new ElevatorRequest(2, 5);
        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request)
            .Returns((ElevatorRequest?)null);
        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(1)
            .Returns(0);
        _mockScheduler.Setup(s => s.PeekNext()).Returns((ElevatorRequest?)null);

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        // Act
        _controller.ProcessRequests(cts.Token);

        // Assert - verify key operations were called
        _mockElevator.Verify(e => e.OpenDoor(It.IsAny<CancellationToken>()), Times.AtLeastOnce, "Should open doors at pickup");
        _mockElevator.Verify(e => e.CloseDoor(It.IsAny<CancellationToken>()), Times.AtLeastOnce, "Should close doors");
        _mockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce, "Should log operations");
    }

    [Fact]
    public void Should_Process_Multiple_Sequential_Requests()
    {
        // Arrange
        var request1 = new ElevatorRequest(2, 5);
        var request2 = new ElevatorRequest(6, 8);

        _mockScheduler.SetupSequence(s => s.GetNext())
            .Returns(request1)
            .Returns(request2)
            .Returns((ElevatorRequest?)null);

        _mockScheduler.SetupSequence(s => s.GetPendingCount())
            .Returns(2)  // First call during first iteration
            .Returns(1)  // After first request processed
            .Returns(1)  // Second iteration
            .Returns(0); // After second request processed

        _mockScheduler.SetupSequence(s => s.PeekNext())
            .Returns(request2) // During first request processing
            .Returns((ElevatorRequest?)null); // During second request processing

        int currentFloor = 2;
        _mockElevator.SetupGet(e => e.CurrentFloor).Returns(() => currentFloor);
        _mockElevator.SetupGet(e => e.State).Returns(ElevatorState.Idle);

        _mockElevator.Setup(e => e.MoveUp(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor++);
        _mockElevator.Setup(e => e.MoveDown(It.IsAny<CancellationToken>()))
            .Callback(() => currentFloor--);

        var cts = new CancellationTokenSource();
        Task.Delay(100).ContinueWith(_ => cts.Cancel());

        // Act & Assert
        _controller.Invoking(c => c.ProcessRequests(cts.Token)).Should().NotThrow();
    }

    #endregion
}
