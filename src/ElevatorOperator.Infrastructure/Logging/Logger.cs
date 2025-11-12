using System.Diagnostics;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Logging;

public class Logger : ILogger
{
    private static string Timestamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    /// <summary>Logs an informational message in blue color with timestamp and calling class name.</summary>
    /// <param name="message">The message to log.</param>
    public void Info(string message)
    {
        Log("INFO", message, ConsoleColor.Blue);
    }

    /// <summary>Logs a warning message in yellow color with timestamp and calling class name.</summary>
    /// <param name="message">The message to log.</param>
    public void Warn(string message)
    {
        Log("WARN", message, ConsoleColor.Yellow);
    }

    /// <summary>Logs an error message in dark red color with optional exception details including stack trace.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">Optional exception to log with its type, message, and stack trace.</param>
    public void Error(string message, Exception? ex = null)
    {
        Log("ERROR", message, ConsoleColor.DarkRed, ex);
    }


    /// <summary>Core logging implementation with thread-safe atomic color operations. Writes to synchronized output with timestamp, level, class name, and optional exception info.</summary>
    /// <param name="level">The log level (INFO, WARN, ERROR, EXCEPTION).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="color">The console color for the output.</param>
    /// <param name="ex">Optional exception with details to include.</param>
    private void Log(string level, string message, ConsoleColor color, Exception? ex = null)
    {
        var declaringType = GetCallingClassName();

        lock (Console.Out)
        {
            Console.ForegroundColor = color;

            Console.Out.WriteLine($"[{Timestamp}] [{level}] [{declaringType}] {message}");

            if (ex != null)
            {
                Console.Out.WriteLine($"[{Timestamp}] [EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                Console.Out.WriteLine(ex.StackTrace);
            }

            Console.Out.Flush();
            Console.ResetColor();
        }
    }

    /// <summary>Extracts the calling class name from the stack trace, skipping framework types and internal compiler-generated classes. Returns "CLI" for Main entry point.</summary>
    /// <returns>The name of the first non-framework class in the call stack, or "Unknown" if none found.</returns>
    private static string GetCallingClassName()
    {
        var stackTrace = new StackTrace();

        for (int i = 3; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame?.GetMethod();
            var declaringType = method?.DeclaringType;

            if (declaringType == null)
                continue;

            var typeName = declaringType.Name;

            if (typeName.Contains("<") || typeName.Contains(">") || typeName.StartsWith("<>"))
                continue;

            if (typeName.Contains("<Main>"))
                return "CLI";

            return typeName;
        }

        return "Unknown";
    }
}
