using System.Diagnostics;
using ElevatorOperator.Application.Interfaces;

namespace ElevatorOperator.Infrastructure.Logging;

public class Logger : ILogger
{
    private readonly TextWriter _synchronizedOutput = TextWriter.Synchronized(Console.Out);

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
        var declaringType = GetCallingClassName();

        _synchronizedOutput.WriteLine($"[{Timestamp}] [{level}] [{declaringType}] {message}");

        if (ex != null)
        {
            _synchronizedOutput.WriteLine($"[{Timestamp}] [EXCEPTION] {ex.GetType().Name}: {ex.Message}");
            _synchronizedOutput.WriteLine(ex.StackTrace);
        }

        _synchronizedOutput.Flush();
        Console.ResetColor();
    }

    private string GetCallingClassName()
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
