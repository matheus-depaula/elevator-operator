using ElevatorOperator.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.CLI.CompositionRoot;

internal partial class Program
{
    private static async Task Main()
    {
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();

        var controller = services.GetRequiredService<IElevatorController>();
        var logger = services.GetRequiredService<ILogger>();

        var cts = new CancellationTokenSource();

        Console.WriteLine("=== Elevator Control System CLI ===");
        Console.WriteLine("Enter pickup and destination (e.g. '3 7, 5 1') or 'exit': ");

        var processingTask = Task.Run(() =>
        {
            try
            {
                controller.ProcessRequests(cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.Info("Processing stopped.");
            }
            catch (Exception ex)
            {
                logger.Error("Fatal error in elevator processing loop.", ex);
            }
        }, cts.Token);

        while (!cts.Token.IsCancellationRequested)
        {
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
            {
                cts.Cancel();
                break;
            }

            try
            {
                // Support multiple pairs separated by commas
                var pairs = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var numbers = pair
                        .Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (numbers.Length != 2 ||
                        !int.TryParse(numbers[0], out var pickup) ||
                        !int.TryParse(numbers[1], out var destination))
                    {
                        logger.Warn($"Invalid input '{pair}'. Use format: pickup destination (e.g. '3 7').");
                        continue;
                    }

                    controller.RequestElevator(pickup, destination);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error handling user input.", ex);
            }

        }

        try
        {
            await processingTask;
        }
        catch (TaskCanceledException)
        {
            logger.Info("Shutdown complete.");
        }

        logger.Info("Elevator system stopped safely.");
    }
}
