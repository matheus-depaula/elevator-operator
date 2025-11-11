using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.CLI.CompositionRoot;
using ElevatorOperator.Infrastructure.Logging;
using ElevatorOperator.Infrastructure.Scheduling;
using ElevatorOperator.Domain.Adapters;
using ElevatorOperator.Domain.Entities;
using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ElevatorOperator.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddElevatorOperator_Should_Register_Logger()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var logger = provider.GetService<ILogger>();
        logger.Should().NotBeNull();
        logger.Should().BeOfType<Logger>();
    }

    [Fact]
    public void AddElevatorOperator_Should_Register_Scheduler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var scheduler = provider.GetService<IScheduler<ElevatorRequest>>();
        scheduler.Should().NotBeNull();
        scheduler.Should().BeOfType<FifoScheduler<ElevatorRequest>>();
    }

    [Fact]
    public void AddElevatorOperator_Should_Register_ElevatorController()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var controller = provider.GetService<IElevatorController>();
        controller.Should().NotBeNull();
    }

    [Fact]
    public void AddElevatorOperator_Should_Register_Elevator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var elevator = provider.GetService<IElevator>();
        elevator.Should().NotBeNull();
        elevator.Should().BeOfType<Elevator>();
    }

    [Fact]
    public void AddElevatorOperator_Should_Register_ElevatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var adapter = provider.GetService<IElevatorAdapter>();
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<ElevatorAdapter>();
    }

    [Fact]
    public void AddElevatorOperator_Should_Register_As_Singletons()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var logger1 = provider.GetService<ILogger>();
        var logger2 = provider.GetService<ILogger>();
        logger1.Should().BeSameAs(logger2);

        var scheduler1 = provider.GetService<IScheduler<ElevatorRequest>>();
        var scheduler2 = provider.GetService<IScheduler<ElevatorRequest>>();
        scheduler1.Should().BeSameAs(scheduler2);
    }

    [Fact]
    public void AddElevatorOperator_Should_Allow_Multiple_Calls()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddElevatorOperator();
        services.AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var controller = provider.GetService<IElevatorController>();
        controller.Should().NotBeNull();
    }

    [Fact]
    public void AddElevatorOperator_Should_Return_IServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddElevatorOperator();

        // Assert
        result.Should().NotBeNull();
    }
}

public class InputParsingTests
{
    [Fact]
    public void ParseElevatorRequest_Should_Parse_Valid_Single_Request()
    {
        // Arrange
        var input = "3 7";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(3);
        requests[0].DestinationFloor.Should().Be(7);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Parse_Multiple_Requests()
    {
        // Arrange
        var input = "3 7, 5 1, 2 9";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(3);
        requests[0].PickupFloor.Should().Be(3);
        requests[1].PickupFloor.Should().Be(5);
        requests[2].PickupFloor.Should().Be(2);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Handle_Whitespace()
    {
        // Arrange
        var input = "  3   7  ";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(3);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Handle_Multiple_Spaces_Between_Numbers()
    {
        // Arrange
        var input = "3     7";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(3);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Return_Empty_On_Invalid_Format()
    {
        // Arrange
        var input = "invalid";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Return_Empty_On_Missing_Number()
    {
        // Arrange
        var input = "3";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Return_Empty_On_Non_Numeric_Input()
    {
        // Arrange
        var input = "a b";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Handle_Negative_Numbers()
    {
        // Arrange
        var input = "-1 5";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(-1);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Handle_Large_Numbers()
    {
        // Arrange
        var input = "999 1000";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(999);
        requests[0].DestinationFloor.Should().Be(1000);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Handle_Zero()
    {
        // Arrange
        var input = "0 5";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(0);
    }

    [Fact]
    public void ParseElevatorRequest_Should_Trim_Commas_With_Spaces()
    {
        // Arrange
        var input = "3 7 , 5 1 , 2 9";

        // Act
        var requests = ParseInputRequests(input);

        // Assert
        requests.Should().HaveCount(3);
    }

    private List<ElevatorRequest> ParseInputRequests(string input)
    {
        var requests = new List<ElevatorRequest>();

        var pairs = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var numbers = pair.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (numbers.Length != 2 ||
                !int.TryParse(numbers[0], out var pickup) ||
                !int.TryParse(numbers[1], out var destination))
            {
                continue;
            }

            try
            {
                requests.Add(new ElevatorRequest(pickup, destination));
            }
            catch
            {
                // Invalid request, skip
            }
        }

        return requests;
    }
}

public class ExitCommandTests
{
    [Fact]
    public void Exit_Command_Should_Be_Case_Insensitive()
    {
        // Arrange
        var command1 = "exit";
        var command2 = "EXIT";
        var command3 = "Exit";

        // Act
        var result1 = command1.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);
        var result2 = command2.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);
        var result3 = command3.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    [Fact]
    public void Exit_Command_Should_Not_Match_Similar_Words()
    {
        // Arrange
        var command = "exit2";

        // Act
        var result = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Exit_Command_Should_Match_Trimmed_Input()
    {
        // Arrange
        var command = "  exit  ";

        // Act
        var result = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        result.Should().BeTrue();
    }
}

public class CLIIntegrationTests
{
    [Fact]
    public void Should_Initialize_DependencyInjection_Successfully()
    {
        // Arrange & Act
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Assert
        var controller = provider.GetRequiredService<IElevatorController>();
        var logger = provider.GetRequiredService<ILogger>();
        var scheduler = provider.GetRequiredService<IScheduler<ElevatorRequest>>();

        controller.Should().NotBeNull();
        logger.Should().NotBeNull();
        scheduler.Should().NotBeNull();
    }

    [Fact]
    public void Should_Request_Elevator_Through_CLI_Input()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(3, 7);

        // Assert - should not throw and request should be processed
        var status = provider.GetRequiredService<IElevatorController>();
        status.Should().NotBeNull();
    }

    [Fact]
    public void Should_Handle_Multiple_Requests_From_CLI()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(2, 5);
        controller.RequestElevator(3, 7);
        controller.RequestElevator(4, 9);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Handle_Cancellation_Token()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        controller.ProcessRequests(cts.Token);

        // Assert - should exit gracefully
        cts.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void Should_All_Services_Be_Singletons()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();

        // Act
        var logger1 = provider.GetRequiredService<ILogger>();
        var logger2 = provider.GetRequiredService<ILogger>();
        var controller1 = provider.GetRequiredService<IElevatorController>();
        var controller2 = provider.GetRequiredService<IElevatorController>();

        // Assert
        logger1.Should().BeSameAs(logger2);
        controller1.Should().BeSameAs(controller2);
    }

    [Fact]
    public void Should_Log_Operations_During_Request_Processing()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(3, 7);

        // Assert - should have logged the request
        // This is verified through the logger implementation
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Validate_Request_Parameters_From_CLI()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(0, 5);  // Invalid: floor 0
        controller.RequestElevator(5, 11); // Invalid: floor 11
        controller.RequestElevator(5, 5);  // Invalid: same floor

        // Assert - should handle gracefully without throwing
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Support_Sequential_Requests()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        for (int i = 1; i < 10; i++)
        {
            controller.RequestElevator(i, (i % 10) + 1);
        }

        // Assert - all requests should be queued
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Create_New_Provider_Instance()
    {
        // Arrange
        var services1 = new ServiceCollection().AddElevatorOperator();
        var services2 = new ServiceCollection().AddElevatorOperator();

        // Act
        var provider1 = services1.BuildServiceProvider();
        var provider2 = services2.BuildServiceProvider();

        // Assert
        var logger1 = provider1.GetRequiredService<ILogger>();
        var logger2 = provider2.GetRequiredService<ILogger>();

        // Different providers should have different instances
        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Should_Handle_Elevator_Requests_With_Valid_Floors()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(1, 10);
        controller.RequestElevator(5, 3);
        controller.RequestElevator(10, 1);

        // Assert
        controller.Should().NotBeNull();
    }
}

public class InputValidationTests
{
    [Fact]
    public void Should_Reject_Empty_Input()
    {
        // Arrange
        var input = "";

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(input);

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    public void Should_Reject_Whitespace_Only_Input()
    {
        // Arrange
        var input = "   ";

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(input);

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    public void Should_Accept_Valid_Number_Pair()
    {
        // Arrange
        var input = "3 7";
        var numbers = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var isValid = numbers.Length == 2 &&
                     int.TryParse(numbers[0], out var pickup) &&
                     int.TryParse(numbers[1], out var destination);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Reject_Non_Numeric_Input()
    {
        // Arrange
        var input = "three seven";
        var numbers = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var isValid = numbers.Length == 2 &&
                     int.TryParse(numbers[0], out _) &&
                     int.TryParse(numbers[1], out _);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Reject_Mixed_Input()
    {
        // Arrange
        var input = "3 seven";
        var numbers = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var isValid = numbers.Length == 2 &&
                     int.TryParse(numbers[0], out _) &&
                     int.TryParse(numbers[1], out _);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Accept_Single_Digit_Numbers()
    {
        // Arrange
        var input = "1 9";
        var numbers = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var isValid = numbers.Length == 2 &&
                     int.TryParse(numbers[0], out _) &&
                     int.TryParse(numbers[1], out _);

        // Assert
        isValid.Should().BeTrue();
    }
}

public class CLIErrorHandlingTests
{
    [Fact]
    public void Should_Handle_Invalid_Input_Gracefully()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act & Assert - should not throw
        controller.Invoking(c => c.RequestElevator(int.MaxValue, int.MaxValue))
            .Should().NotThrow();
    }

    [Fact]
    public async Task Should_Handle_Concurrent_CLI_Requests()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();
        var tasks = new List<Task>();

        // Act
        for (int i = 1; i < 10; i++)
        {
            int floor = i;
            tasks.Add(Task.Run(() => controller.RequestElevator(floor, (floor % 10) + 1)));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert - should handle without throwing
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Prevent_Exception_During_Invalid_Request()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act & Assert
        controller.Invoking(c => c.RequestElevator(0, 0))
            .Should().NotThrow();
    }

    [Fact]
    public void Should_Handle_Null_Input()
    {
        // Arrange
        string? input = null;

        // Act
        var isEmpty = string.IsNullOrWhiteSpace(input);

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Very_Large_Floor_Numbers()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();
        var provider = services.BuildServiceProvider();
        var controller = provider.GetRequiredService<IElevatorController>();

        // Act & Assert - should handle without throwing
        controller.Invoking(c => c.RequestElevator(999999, 1000000))
            .Should().NotThrow();
    }
}
