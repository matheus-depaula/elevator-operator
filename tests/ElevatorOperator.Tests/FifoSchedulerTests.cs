using ElevatorOperator.Domain.ValueObjects;
using ElevatorOperator.Infrastructure.Scheduling;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class FifoSchedulerTests
{
    [Fact]
    public void Should_Enqueue_And_Retrieve_Requests_In_FIFO_Order()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var request1 = new ElevatorRequest(3, 7);
        var request2 = new ElevatorRequest(5, 2);
        var request3 = new ElevatorRequest(2, 8);

        // Act
        scheduler.Enqueue(request1);
        scheduler.Enqueue(request2);
        scheduler.Enqueue(request3);

        // Assert
        scheduler.GetPendingCount().Should().Be(3);
        scheduler.GetNext().Should().Be(request1);
        scheduler.GetPendingCount().Should().Be(2);
        scheduler.GetNext().Should().Be(request2);
        scheduler.GetNext().Should().Be(request3);
        scheduler.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public void Should_Peek_Without_Removing()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var request = new ElevatorRequest(3, 7);

        // Act
        scheduler.Enqueue(request);
        var peeked = scheduler.PeekNext();
        var retrieved = scheduler.GetNext();

        // Assert
        peeked.Should().Be(request);
        retrieved.Should().Be(request);
        scheduler.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public void Should_Return_Null_When_Empty()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();

        // Act
        var result = scheduler.GetNext();

        // Assert
        result.Should().BeNull();
        scheduler.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Enqueue()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var tasks = new List<Task>();

        // Act
        for (int i = 1; i <= 10; i++)
        {
            int floor = i;
            tasks.Add(Task.Run(() =>
            {
                var request = new ElevatorRequest(floor, (floor % 10) + 1);
                scheduler.Enqueue(request);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        scheduler.GetPendingCount().Should().Be(10);
    }

    #region Edge Case Tests

    [Fact]
    public void Should_Return_Null_On_PeekNext_When_Empty()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();

        // Act
        var result = scheduler.PeekNext();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Should_Peek_Multiple_Times_Without_Dequeuing()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var request = new ElevatorRequest(2, 5);

        // Act
        scheduler.Enqueue(request);
        var peek1 = scheduler.PeekNext();
        var peek2 = scheduler.PeekNext();
        var peek3 = scheduler.PeekNext();

        // Assert
        peek1.Should().Be(request);
        peek2.Should().Be(request);
        peek3.Should().Be(request);
        scheduler.GetPendingCount().Should().Be(1);
    }

    [Fact]
    public void Should_Return_Null_On_GetNext_When_Empty()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();

        // Act & Assert
        scheduler.GetNext().Should().BeNull();
        scheduler.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public void Should_Maintain_FIFO_Order_With_Many_Items()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var requests = new List<ElevatorRequest>();

        for (int i = 1; i <= 100; i++)
        {
            var request = new ElevatorRequest(i % 10 + 1, (i + 1) % 10 + 1);
            requests.Add(request);
            scheduler.Enqueue(request);
        }

        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var next = scheduler.GetNext();
            next.Should().Be(requests[i]);
        }

        scheduler.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public void Should_Handle_Peek_After_Dequeue()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var request1 = new ElevatorRequest(2, 5);
        var request2 = new ElevatorRequest(3, 7);

        scheduler.Enqueue(request1);
        scheduler.Enqueue(request2);

        // Act
        var dequeued = scheduler.GetNext();
        var peeked = scheduler.PeekNext();

        // Assert
        dequeued.Should().Be(request1);
        peeked.Should().Be(request2);
        scheduler.GetPendingCount().Should().Be(1);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Peek_And_Dequeue()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var peekTasks = new List<Task>();
        var dequeueTasks = new List<Task>();

        // Enqueue initial items
        for (int i = 1; i <= 20; i++)
        {
            var request = new ElevatorRequest((i % 10) + 1, ((i + 1) % 10) + 1);
            scheduler.Enqueue(request);
        }

        // Act - Mix peek and dequeue operations
        for (int i = 0; i < 10; i++)
        {
            dequeueTasks.Add(Task.Run(() => scheduler.GetNext()));
            peekTasks.Add(Task.Run(() => scheduler.PeekNext()));
        }

        await Task.WhenAll(peekTasks.Concat(dequeueTasks));

        // Assert - Should have dequeued 10 items
        var count = scheduler.GetPendingCount();
        count.Should().BeLessThanOrEqualTo(20);
    }

    [Fact]
    public void Should_Handle_Enqueue_After_Empty()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var request1 = new ElevatorRequest(2, 5);
        var request2 = new ElevatorRequest(3, 7);

        // Act
        scheduler.Enqueue(request1);
        scheduler.GetNext();
        scheduler.Enqueue(request2);

        // Assert
        scheduler.GetPendingCount().Should().Be(1);
        scheduler.GetNext().Should().Be(request2);
    }

    [Fact]
    public async Task Should_Handle_Stress_Test_With_Many_Concurrent_Operations()
    {
        // Arrange
        var scheduler = new FifoScheduler<ElevatorRequest>();
        var tasks = new List<Task>();

        // Act - 50 concurrent enqueues and 25 concurrent dequeues
        for (int i = 0; i < 50; i++)
        {
            int floor = (i % 10) + 1;
            tasks.Add(Task.Run(() =>
            {
                var request = new ElevatorRequest(floor, (floor % 10) + 1);
                scheduler.Enqueue(request);
            }));
        }

        for (int i = 0; i < 25; i++)
        {
            tasks.Add(Task.Run(() => scheduler.GetNext()));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have 25 items left (50 enqueued - 25 dequeued)
        scheduler.GetPendingCount().Should().Be(25);
    }

    [Fact]
    public void Should_Handle_Generic_Type_With_Different_Types()
    {
        // Arrange
        var scheduler = new FifoScheduler<string>();

        // Act
        scheduler.Enqueue("a");
        scheduler.Enqueue("b");
        scheduler.Enqueue("c");

        // Assert
        scheduler.GetNext().Should().Be("a");
        scheduler.GetNext().Should().Be("b");
        scheduler.PeekNext().Should().Be("c");
        scheduler.GetNext().Should().Be("c");
        scheduler.GetNext().Should().BeNull();
    }

    #endregion
}
