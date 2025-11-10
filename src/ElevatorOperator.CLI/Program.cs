using ElevatorOperator.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.CLI.CompositionRoot;
using ElevatorOperator.Domain.Exceptions;

internal partial class Program
{
    private static async Task Main()
    {
        var services = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();

        var controller = services.GetRequiredService<IElevatorController>();
        var logger = services.GetRequiredService<ILogger>();

        Console.WriteLine("=== Elevator Control System CLI ===");
        Console.WriteLine("Usage: type a pair of floors 'pickup destination' (e.g. '3 7').");
        Console.WriteLine("You can enter multiple pairs separated by commas (e.g. '1 5, 4 2').");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                break;

            var requests = input
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair =>
                {
                    var numbers = pair
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x, out var floor) ? (int?)floor : null)
                        .ToList();

                    return numbers.Count == 2 && numbers.All(f => f.HasValue)
                        ? (pickup: numbers[0]!.Value, destination: numbers[1]!.Value)
                        : ((int pickup, int destination)?)null;
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (requests.Count == 0)
            {
                logger.Warn("Invalid input. Please use pairs of numbers like '2 7, 5 9'.");
                continue;
            }

            var tasks = requests.Select(async req =>
            {
                try
                {
                    await Task.Run(() => controller.RequestElevator(req.pickup, req.destination));
                }
                catch (ElevatorOperatorException ex) when (ex is InvalidFloorException || ex is InvalidPickupAndDestinationException)
                {
                    logger.Warn(ex.Message);
                }
                catch (ElevatorOperatorException ex)
                {
                    logger.Error("Elevator operation error.", ex);
                }
                catch (Exception ex)
                {
                    logger.Error("Unexpected error occurred.", ex);
                }
            });

            await Task.WhenAll(tasks);

            await Task.Run(controller.ProcessRequests);
        }
    }
}
