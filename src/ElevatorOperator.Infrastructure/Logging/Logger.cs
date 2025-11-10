using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Logging;

public class Logger : ILogger
{
    private readonly Lock _lock = new();

    private static string Timestamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    public void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Log("INFO", message);
    }

    public void Warn(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Log("WARN", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Log("ERROR", message, ex);
    }


    private void Log(string level, string message, Exception? ex = null)
    {
        lock (_lock)
        {
            var frame = new StackTrace().GetFrame(2);
            var declaringType = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

            if (declaringType.Contains("<Main>"))
                declaringType = "CLI";

            Console.WriteLine($"[{Timestamp}] [{level}] [{declaringType}] {message}");

            if (ex != null)
            {
                Console.WriteLine($"[{Timestamp}] [EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.ResetColor();
        }
    }
}
