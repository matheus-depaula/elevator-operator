using ElevatorOperator.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.CLI.CompositionRoot;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();

        var controller = services.GetRequiredService<IElevatorController>();
        var logger = services.GetRequiredService<ILogger>();

        if (args.Contains("--benchmark"))
        {
            int requestCount = 200;

            var countArg = args.SkipWhile(a => a != "--benchmark").Skip(1).FirstOrDefault();

            if (countArg != null && int.TryParse(countArg, out var parsed))
                requestCount = parsed;

            await RunBenchmark(controller, logger, requestCount);
            return;
        }

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

            logger.Info("Waiting for elevator to complete remaining requests...");
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

    private static async Task RunBenchmark(IElevatorController controller, ILogger logger, int requestCount = 200)
    {
        var rnd = new Random();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        logger.Info($"Starting benchmark with {requestCount} concurrent requests...");

        long memBefore = GC.GetTotalMemory(forceFullCollection: true);

        var tasks = Enumerable.Range(0, requestCount).Select(i => Task.Run(() =>
        {
            int pickup = rnd.Next(1, 10);
            int destination;
            do
            {
                destination = rnd.Next(1, 10);
            } while (destination == pickup);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            controller.RequestElevator(pickup, destination);
            sw.Stop();

            return sw.ElapsedMilliseconds;
        }));

        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        long memAfter = GC.GetTotalMemory(forceFullCollection: true);
        double avgLatency = results.Average();
        double maxLatency = results.Max();

        logger.Info($"Benchmark complete:");
        logger.Info($"  Total Requests: {requestCount}");
        logger.Info($"  Avg Assignment Time: {avgLatency:F2} ms");
        logger.Info($"  Max Assignment Time: {maxLatency:F2} ms");
        logger.Info($"  Total Duration: {stopwatch.ElapsedMilliseconds / 1000.0:F2} s");
        logger.Info($"  Memory Used: {(memAfter - memBefore) / 1024.0 / 1024.0:F2} MB");

        Console.WriteLine("\nPress ENTER to exit benchmark...");
        Console.ReadLine();
    }
}
