using System.Runtime.CompilerServices;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Logging;

public class Logger : ILogger
{
    public void Info(string message, [CallerMemberName] string caller = "")
    {
        Console.WriteLine($"[INFO] [{caller}] {message}");
    }

    public void Warn(string message, [CallerMemberName] string caller = "")
    {
        Console.WriteLine($"[WARN] [{caller}] {message}");
    }

    public void Error(string message, Exception? ex = null, [CallerMemberName] string caller = "")
    {
        Console.WriteLine($"[ERROR] [{caller}] {message}");
        if (ex != null)
        {
            Console.WriteLine($"[EXCEPTION] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
