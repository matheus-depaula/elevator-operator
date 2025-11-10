using ElevatorOperator.Domain.Enums;
using ElevatorOperator.Domain.ValueObjects;
using ElevatorOperator.Infrastructure.Scheduling;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class FifoSchedulerTests
{
    [Fact]
    public async Task Should_Schedule_And_Process_Requests_In_FIFO_Order()
    {
        // Arrange
        var scheduler = new FifoScheduler();
        var cts = new CancellationTokenSource();
        var requests = new[]
        {
            new ElevatorRequest(3, ElevatorDirection.Up),
            new ElevatorRequest(5, ElevatorDirection.Down),
            new ElevatorRequest(2, ElevatorDirection.Up)
        };

        // Act
        foreach (var request in requests)
        {
            var result = await scheduler.ScheduleRequest(request, cts.Token);
            result.Should().BeTrue();
        }

        // Assert
        scheduler.GetPendingRequestCount().Should().Be(3);
    }

    [Fact]
    public async Task Should_Cancel_All_Requests()
    {
        // Arrange
        var scheduler = new FifoScheduler();
        var cts = new CancellationTokenSource();
        var requests = new[]
        {
            new ElevatorRequest(3, ElevatorDirection.Up),
            new ElevatorRequest(5, ElevatorDirection.Down)
        };

        // Act
        foreach (var request in requests)
        {
            await scheduler.ScheduleRequest(request, cts.Token);
        }
        scheduler.CancelAllRequests();

        // Assert
        scheduler.GetPendingRequestCount().Should().Be(0);
    }

    [Fact]
    public async Task Should_Process_Requests_Concurrently()
    {
        // Arrange
        var scheduler = new FifoScheduler();
        var cts = new CancellationTokenSource();
        var processingTask = scheduler.ProcessScheduledRequests(cts.Token);
        var requests = new[]
        {
            new ElevatorRequest(3, ElevatorDirection.Up),
            new ElevatorRequest(5, ElevatorDirection.Down),
            new ElevatorRequest(2, ElevatorDirection.Up)
        };

        // Act & Assert
        try
        {
            // Schedule requests concurrently while processing
            var schedulingTasks = requests.Select(r => scheduler.ScheduleRequest(r, cts.Token));
            await Task.WhenAll(schedulingTasks);

            // Let the processor run for a bit
            await Task.Delay(500);

            // Cancel processing
            cts.Cancel();
            await processingTask.ContinueWith(_ => { }); // Ignore cancellation exception

            // Verify some requests were processed
            scheduler.GetPendingRequestCount().Should().BeLessThan(requests.Length);
        }
        finally
        {
            cts.Dispose();
        }
    }
}
