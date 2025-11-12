using System.Diagnostics;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Logging;

public class Logger : ILogger
{
    private readonly TextWriter _synchronizedOutput = TextWriter.Synchronized(Console.Out);

    private static string Timestamp => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

    public void Info(string message)
    {
        Log("INFO", message, ConsoleColor.Blue);
    }

    public void Warn(string message)
    {
        Log("WARN", message, ConsoleColor.Yellow);
    }

    public void Error(string message, Exception? ex = null)
    {
        Log("ERROR", message, ConsoleColor.DarkRed, ex);
    }


    private void Log(string level, string message, ConsoleColor color, Exception? ex = null)
    {
        var declaringType = GetCallingClassName();

        lock (Console.Out)
        {
            Console.ForegroundColor = color;

            _synchronizedOutput.WriteLine($"[{Timestamp}] [{level}] [{declaringType}] {message}");

            if (ex != null)
            {
                _synchronizedOutput.WriteLine($"[{Timestamp}] [EXCEPTION] {ex.GetType().Name}: {ex.Message}");
                _synchronizedOutput.WriteLine(ex.StackTrace);
            }

            _synchronizedOutput.Flush();
            Console.ResetColor();
        }
    }

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
