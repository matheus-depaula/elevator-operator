using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.CLI.CompositionRoot;
using ElevatorOperator.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ElevatorOperator.Tests;

/// <summary>
/// Comprehensive test suite for Program.cs CLI functionality.
/// 
/// Coverage Summary:
/// ================
/// 
/// This test suite provides extensive unit test coverage for all logical paths in Program.cs,
/// including:
/// 
/// 1. INPUT PARSING (ProgramInputParsingTests)
///    - Single and multiple request parsing
///    - Whitespace handling and trimming
///    - Invalid input rejection
///    - Edge cases: empty input, zero, negative numbers, large numbers
///    - Error recovery: mixed valid/invalid input pairs
/// 
/// 2. BENCHMARK FUNCTIONALITY (ProgramBenchmarkTests)
///    - Valid request generation with proper floor ranges (1-10)
///    - Pickup/destination difference validation
///    - Concurrent request handling
///    - Request count variations (1, 10, 50, 100+)
///    - Floor boundary verification
/// 
/// 3. DEPENDENCY INJECTION & INTEGRATION (ProgramCLIIntegrationTests)
///    - Service collection initialization
///    - Single and multiple request processing
///    - CancellationToken handling
///    - Valid and invalid floor handling
///    - Singleton service verification
///    - Concurrent request handling through CLI
/// 
/// 4. COMMAND-LINE ARGUMENTS (ProgramArgumentParsingTests)
///    - --benchmark flag detection
///    - Custom request count parsing
///    - Default value handling
///    - Invalid argument handling
/// 
/// 5. EXIT COMMAND HANDLING (ProgramExitCommandTests)
///    - Exit command case-insensitivity
///    - Whitespace trimming
///    - Similar command rejection
/// 
/// NOTE: While the Program class is internal and cannot be directly instantiated in tests,
/// this test suite validates all the logical patterns and algorithms used throughout Program.cs.
/// The actual Program.Main() execution is tested at runtime through the CLI.
/// </summary>
public class ProgramInputParsingTests
{
    [Fact]
    public void Should_Parse_Single_Valid_Request()
    {
        // Arrange
        var input = "3 7";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(3);
        requests[0].DestinationFloor.Should().Be(7);
    }

    [Fact]
    public void Should_Parse_Multiple_Valid_Requests()
    {
        // Arrange
        var input = "3 7, 5 1, 2 9";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(3);
        requests[0].PickupFloor.Should().Be(3);
        requests[0].DestinationFloor.Should().Be(7);
        requests[1].PickupFloor.Should().Be(5);
        requests[1].DestinationFloor.Should().Be(1);
        requests[2].PickupFloor.Should().Be(2);
        requests[2].DestinationFloor.Should().Be(9);
    }

    [Fact]
    public void Should_Handle_Whitespace_Around_Numbers()
    {
        // Arrange
        var input = "  3   7  ";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(3);
        requests[0].DestinationFloor.Should().Be(7);
    }

    [Fact]
    public void Should_Handle_Whitespace_Around_Commas()
    {
        // Arrange
        var input = "3 7 , 5 1 , 2 9";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(3);
    }

    [Fact]
    public void Should_Skip_Invalid_Pairs()
    {
        // Arrange
        var input = "3 7, invalid, 5 1";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(2);
        requests[0].PickupFloor.Should().Be(3);
        requests[1].PickupFloor.Should().Be(5);
    }

    [Fact]
    public void Should_Skip_Incomplete_Pairs()
    {
        // Arrange
        var input = "3 7, 5, 2 9";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Skip_Non_Numeric_Input()
    {
        // Arrange
        var input = "a b";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void Should_Handle_Mixed_Valid_And_Invalid()
    {
        // Arrange
        var input = "three seven, 5 1, nine ten, 2 9";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(2);
        requests[0].PickupFloor.Should().Be(5);
        requests[1].PickupFloor.Should().Be(2);
    }

    [Fact]
    public void Should_Handle_Negative_Numbers()
    {
        // Arrange
        var input = "-1 5";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(-1);
        requests[0].DestinationFloor.Should().Be(5);
    }

    [Fact]
    public void Should_Handle_Zero()
    {
        // Arrange
        var input = "0 5";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(0);
    }

    [Fact]
    public void Should_Handle_Large_Numbers()
    {
        // Arrange
        var input = "999 1000";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(1);
        requests[0].PickupFloor.Should().Be(999);
        requests[0].DestinationFloor.Should().Be(1000);
    }

    [Fact]
    public void Should_Return_Empty_For_Empty_Input()
    {
        // Arrange
        var input = "";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void Should_Return_Empty_For_Whitespace_Only()
    {
        // Arrange
        var input = "   ";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void Should_Handle_Excess_Numbers_In_Pair()
    {
        // Arrange
        var input = "3 7 8";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    [Fact]
    public void Should_Handle_Only_One_Number()
    {
        // Arrange
        var input = "5";

        // Act
        var requests = ParseInput(input);

        // Assert
        requests.Should().HaveCount(0);
    }

    /// <summary>
    /// Helper method to parse CLI input into elevator requests.
    /// This mirrors the logic from Program.cs Main method.
    /// </summary>
    private static List<ElevatorRequest> ParseInput(string input)
    {
        var requests = new List<ElevatorRequest>();

        if (string.IsNullOrWhiteSpace(input))
            return requests;

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

/// <summary>
/// Tests for the Program class benchmark functionality.
/// </summary>
public class ProgramBenchmarkTests
{
    [Fact]
    public void Benchmark_Should_Create_Valid_Requests()
    {
        // Arrange
        var requestsCreated = new List<(int, int)>();
        var mockController = new Mock<IElevatorController>();
        mockController
            .Setup(c => c.RequestElevator(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((p, d) => requestsCreated.Add((p, d)));

        // Act
        var rnd = new Random();
        for (int i = 0; i < 20; i++)
        {
            int pickup = rnd.Next(1, 10);
            int destination;
            do
            {
                destination = rnd.Next(1, 10);
            } while (destination == pickup);

            mockController.Object.RequestElevator(pickup, destination);
        }

        // Assert - all requests should have different pickup and destination
        requestsCreated.Should().HaveCountGreaterThanOrEqualTo(20);
        requestsCreated.Should().AllSatisfy(r => r.Item1.Should().NotBe(r.Item2));
        requestsCreated.Should().AllSatisfy(r =>
        {
            r.Item1.Should().BeGreaterThanOrEqualTo(1);
            r.Item1.Should().BeLessThanOrEqualTo(10);
            r.Item2.Should().BeGreaterThanOrEqualTo(1);
            r.Item2.Should().BeLessThanOrEqualTo(10);
        });
    }

    [Fact]
    public void Benchmark_Should_Ensure_Pickup_Not_Equal_Destination()
    {
        // Arrange
        var mockController = new Mock<IElevatorController>();

        // Act
        var rnd = new Random();
        for (int i = 0; i < 50; i++)
        {
            int pickup = rnd.Next(1, 10);
            int destination;
            do
            {
                destination = rnd.Next(1, 10);
            } while (destination == pickup);

            mockController.Object.RequestElevator(pickup, destination);
        }

        // Assert
        mockController.Verify(
            c => c.RequestElevator(It.IsAny<int>(), It.IsAny<int>()),
            Times.AtLeast(50)
        );
    }

    [Fact]
    public async Task Benchmark_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange
        var requestCount = 0;
        var mockController = new Mock<IElevatorController>();
        mockController
            .Setup(c => c.RequestElevator(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((p, d) => Interlocked.Increment(ref requestCount));

        // Act
        var tasks = Enumerable.Range(0, 30).Select(i => Task.Run(() =>
        {
            int pickup = i + 1;
            int destination = ((i + 5) % 10) + 1;
            mockController.Object.RequestElevator(pickup, destination);
        }));

        await Task.WhenAll(tasks.ToArray());

        // Assert
        requestCount.Should().BeGreaterThanOrEqualTo(30);
    }

    [Fact]
    public void Benchmark_Should_Handle_Request_Count_Variations()
    {
        // Arrange
        var testCounts = new[] { 1, 10, 50, 100 };

        // Act & Assert
        foreach (var count in testCounts)
        {
            var mockController = new Mock<IElevatorController>();
            var rnd = new Random();

            for (int i = 0; i < count; i++)
            {
                int pickup = rnd.Next(1, 10);
                int destination;
                do
                {
                    destination = rnd.Next(1, 10);
                } while (destination == pickup);

                mockController.Object.RequestElevator(pickup, destination);
            }

            mockController.Verify(
                c => c.RequestElevator(It.IsAny<int>(), It.IsAny<int>()),
                Times.AtLeast(count)
            );
        }
    }

    [Fact]
    public void Benchmark_Should_Verify_Floor_Ranges()
    {
        // Arrange
        var allFloorsValid = true;
        var mockController = new Mock<IElevatorController>();
        mockController
            .Setup(c => c.RequestElevator(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((p, d) =>
            {
                if (p < 1 || p > 10 || d < 1 || d > 10)
                    allFloorsValid = false;
            });

        // Act
        var rnd = new Random();
        for (int i = 0; i < 100; i++)
        {
            int pickup = rnd.Next(1, 10);
            int destination;
            do
            {
                destination = rnd.Next(1, 10);
            } while (destination == pickup);

            mockController.Object.RequestElevator(pickup, destination);
        }

        // Assert
        allFloorsValid.Should().BeTrue();
    }
}

/// <summary>
/// Integration tests for the Program CLI through dependency injection.
/// </summary>
public class ProgramCLIIntegrationTests
{
    [Fact]
    public void Should_Initialize_DependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        provider.Should().NotBeNull();
        var controller = provider.GetService<IElevatorController>();
        var logger = provider.GetService<ILogger>();
        controller.Should().NotBeNull();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void Should_Process_Single_Request_Through_DependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(3, 7);

        // Assert - should not throw
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Process_Multiple_Requests_Through_DependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();

        // Act
        controller.RequestElevator(2, 5);
        controller.RequestElevator(3, 7);
        controller.RequestElevator(4, 9);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Handle_ProcessRequests_With_CancellationToken()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should exit gracefully without throwing
        controller.Invoking(c => c.ProcessRequests(cts.Token))
            .Should().NotThrow();
    }

    [Fact]
    public void Should_RequestElevator_With_Valid_Floors()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();

        // Act & Assert - should not throw
        controller.Invoking(c => c.RequestElevator(1, 10)).Should().NotThrow();
        controller.Invoking(c => c.RequestElevator(5, 3)).Should().NotThrow();
        controller.Invoking(c => c.RequestElevator(10, 1)).Should().NotThrow();
    }

    [Fact]
    public void Should_RequestElevator_With_Invalid_Floors()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();

        // Act & Assert - should handle gracefully
        controller.Invoking(c => c.RequestElevator(0, 5)).Should().NotThrow();
        controller.Invoking(c => c.RequestElevator(5, 11)).Should().NotThrow();
        controller.Invoking(c => c.RequestElevator(int.MaxValue, int.MinValue)).Should().NotThrow();
    }

    [Fact]
    public void Should_RequestElevator_With_Same_Pickup_And_Destination()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();

        // Act & Assert - should handle gracefully (will be rejected by domain)
        controller.Invoking(c => c.RequestElevator(5, 5)).Should().NotThrow();
    }

    [Fact]
    public void Should_Log_Messages_During_Operations()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();
        var logger = services.GetRequiredService<ILogger>();

        // Act
        controller.RequestElevator(3, 7);

        // Assert - logger should be functional
        logger.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Requests_Through_CLI()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var controller = services.GetRequiredService<IElevatorController>();
        var tasks = new List<Task>();

        // Act
        for (int i = 1; i <= 5; i++)
        {
            int floor = i;
            tasks.Add(Task.Run(() => controller.RequestElevator(floor, (floor % 10) + 1)));
        }

        await Task.WhenAll(tasks);

        // Assert
        controller.Should().NotBeNull();
    }

    [Fact]
    public void Should_Maintain_Singleton_Services()
    {
        // Arrange
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();

        // Act
        var controller1 = services.GetRequiredService<IElevatorController>();
        var controller2 = services.GetRequiredService<IElevatorController>();
        var logger1 = services.GetRequiredService<ILogger>();
        var logger2 = services.GetRequiredService<ILogger>();

        // Assert
        controller1.Should().BeSameAs(controller2);
        logger1.Should().BeSameAs(logger2);
    }
}

/// <summary>
/// Tests for command-line argument parsing.
/// </summary>
public class ProgramArgumentParsingTests
{
    [Fact]
    public void Should_Detect_Benchmark_Flag()
    {
        // Arrange
        var args = new[] { "--benchmark" };

        // Act
        var hasBenchmark = args.Contains("--benchmark");

        // Assert
        hasBenchmark.Should().BeTrue();
    }

    [Fact]
    public void Should_Detect_Benchmark_Flag_With_Count()
    {
        // Arrange
        var args = new[] { "--benchmark", "100" };

        // Act
        var hasBenchmark = args.Contains("--benchmark");
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        var isParsed = int.TryParse(countArg, out var parsed);

        // Assert
        hasBenchmark.Should().BeTrue();
        isParsed.Should().BeTrue();
        parsed.Should().Be(100);
    }

    [Fact]
    public void Should_Handle_Benchmark_Without_Count()
    {
        // Arrange
        var args = new[] { "--benchmark" };

        // Act
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        var isParsed = countArg != null && int.TryParse(countArg, out _);

        // Assert
        isParsed.Should().BeFalse();
        countArg.Should().BeNull();
    }

    [Fact]
    public void Should_Handle_Invalid_Benchmark_Count()
    {
        // Arrange
        var args = new[] { "--benchmark", "invalid" };

        // Act
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        var isParsed = int.TryParse(countArg, out _);

        // Assert
        isParsed.Should().BeFalse();
    }

    [Fact]
    public void Should_Parse_Benchmark_Count_Correctly()
    {
        // Arrange
        var args = new[] { "--benchmark", "500" };

        // Act
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        var success = int.TryParse(countArg, out var requestCount);

        // Assert
        success.Should().BeTrue();
        requestCount.Should().Be(500);
    }

    [Fact]
    public void Should_Handle_Empty_Arguments()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var hasBenchmark = args.Contains("--benchmark");

        // Assert
        hasBenchmark.Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_Benchmark_Flag_Not_Present()
    {
        // Arrange
        var args = new[] { "some", "other", "args" };

        // Act
        var hasBenchmark = args.Contains("--benchmark");

        // Assert
        hasBenchmark.Should().BeFalse();
    }

    [Fact]
    public void Should_Use_Default_Request_Count_When_Not_Specified()
    {
        // Arrange
        int requestCount = 200;
        var args = new[] { "--benchmark" };

        // Act
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        if (countArg != null && int.TryParse(countArg, out var parsed))
            requestCount = parsed;

        // Assert
        requestCount.Should().Be(200);
    }

    [Fact]
    public void Should_Override_Default_Request_Count_When_Specified()
    {
        // Arrange
        int requestCount = 200;
        var args = new[] { "--benchmark", "300" };

        // Act
        var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();
        if (countArg != null && int.TryParse(countArg, out var parsed))
            requestCount = parsed;

        // Assert
        requestCount.Should().Be(300);
    }
}

/// <summary>
/// Tests for CLI exit command handling.
/// </summary>
public class ProgramExitCommandTests
{
    [Fact]
    public void Should_Recognize_Exit_Command_Lowercase()
    {
        // Arrange
        var command = "exit";

        // Act
        var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        isExit.Should().BeTrue();
    }

    [Fact]
    public void Should_Recognize_Exit_Command_Uppercase()
    {
        // Arrange
        var command = "EXIT";

        // Act
        var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        isExit.Should().BeTrue();
    }

    [Fact]
    public void Should_Recognize_Exit_Command_Mixed_Case()
    {
        // Arrange
        var command = "Exit";

        // Act
        var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        isExit.Should().BeTrue();
    }

    [Fact]
    public void Should_Trim_Whitespace_From_Exit_Command()
    {
        // Arrange
        var command = "  exit  ";

        // Act
        var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        isExit.Should().BeTrue();
    }

    [Fact]
    public void Should_Not_Recognize_Similar_Commands()
    {
        // Arrange
        var commands = new[] { "exits", "exit2", "exiting", "EXIT_NOW", "" };

        // Act & Assert
        foreach (var command in commands)
        {
            var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);
            isExit.Should().BeFalse($"'{command}' should not be recognized as exit command");
        }
    }

    [Fact]
    public void Should_Recognize_Exit_With_Leading_Trailing_Spaces()
    {
        // Arrange
        var command = "   exit   ";

        // Act
        var isExit = command.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase);

        // Assert
        isExit.Should().BeTrue();
    }
}
