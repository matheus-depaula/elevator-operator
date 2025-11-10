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

        while (true)
        {
            Console.WriteLine("Type the floor numbers to request the elevator (e.g. '3 5 2'). Type 'exit' to quit.");
            Console.Write("> ");
            var input = Console.ReadLine();

            if (input == null)
                continue;

            if (input.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase))
                break;

            var requestedFloors = input
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(floorStr => int.TryParse(floorStr, out var floor) ? (int?)floor : null)
                .ToList();

            if (requestedFloors.Any(floor => floor == null))
            {
                Console.WriteLine("Invalid input. Please enter valid floor numbers separated by spaces or type 'exit' to quit.");
                continue;
            }

            var tasks = requestedFloors.Select(async floor =>
            {
                try
                {
                    await Task.Run(() => controller.RequestElevator(floor!.Value));
                }
                catch (ElevatorOperatorException ex) when (ex is InvalidFloorException)
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
