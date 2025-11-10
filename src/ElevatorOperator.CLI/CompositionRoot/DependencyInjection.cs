using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Application.Services;
using ElevatorOperator.Infrastructure.Logging;
using ElevatorOperator.Infrastructure.Scheduling;
using ElevatorOperator.Domain.Interfaces;
using ElevatorOperator.Domain.Entities;
using ElevatorOperator.Domain.Adapters;

namespace ElevatorOperator.CLI.CompositionRoot;

public static class DependencyInjection
{
    public static IServiceCollection AddElevatorOperator(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<ILogger, Logger>();
        services.AddSingleton<IScheduler, FifoScheduler>();

        // Application
        services.AddSingleton<IElevatorController, ElevatorController>();

        // Domain
        services.AddSingleton<IElevator, Elevator>();
        services.AddSingleton<IElevatorAdapter>(provider =>
        {
            var elevator = provider.GetRequiredService<IElevator>();
            return new ElevatorAdapter(elevator);
        });

        return services;
    }
}
