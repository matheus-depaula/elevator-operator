using Microsoft.Extensions.DependencyInjection;
using ElevatorOperator.Application.Interfaces;
using ElevatorOperator.Application.Services;
using ElevatorOperator.Infrastructure.Logging;
using ElevatorOperator.Infrastructure.Scheduling;

namespace ElevatorOperator.CLI.CompositionRoot;

public static class DependencyInjection
{
    public static IServiceCollection AddElevatorOperator(this IServiceCollection services)
    {
        services.AddSingleton<ILogger, Logger>();
        services.AddSingleton<IElevatorScheduler, FifoScheduler>();
        services.AddSingleton<IElevatorController, ElevatorController>();

        return services;
    }
}
