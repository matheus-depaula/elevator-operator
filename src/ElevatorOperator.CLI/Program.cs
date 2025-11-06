using ElevatorOperator.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.CLI.CompositionRoot;

internal partial class Program
{
    private static async Task Main(string[] args)
    {
        var provider = new ServiceCollection().AddElevatorOperator().BuildServiceProvider();
        var elevatorController = provider.GetRequiredService<IElevatorController>();

        Console.WriteLine("=== Elevator Control System CLI ===");

        while (true)
        {
            Console.WriteLine("Type the floor numbers to request the elevator (e.g. '3 5 2'). Type 'exit' to quit.");
            Console.Write("> ");
            var input = Console.ReadLine();

            if (input == null) continue;
            if (input.Trim().Equals("exit", StringComparison.CurrentCultureIgnoreCase)) break;

            var requestedFloors = input
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(floorStr => int.TryParse(floorStr, out var floor) ? (int?)floor : null)
                .ToList();

            if (requestedFloors.Any(floor => floor == null))
            {
                Console.WriteLine("Invalid input. Please enter valid floor numbers separated by spaces or type 'exit' to quit.");
                continue;
            }

            foreach (var requestedFloor in requestedFloors!)
            {
                var floor = requestedFloor!.Value;
                // Testing stuff
                Console.WriteLine($"[CLI] Requesting elevator to floor {floor}");
                await Task.Run(() => elevatorController.RequestElevator(floor));
                await Task.Run(() => elevatorController.ProcessRequests());
            }
        }
    }
}
