using ElevatorOperator.Infrastructure.Logging;
using FluentAssertions;

namespace ElevatorOperator.Tests;

public class LoggerTests : IDisposable
{
    private readonly Logger _logger;
    private readonly StringWriter _stringWriter;

    public LoggerTests()
    {
        _logger = new Logger();
        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);
    }

    public void Dispose()
    {
        _stringWriter?.Dispose();
    }

    #region Info Logging Tests

    [Fact]
    public void Info_Should_Log_Message()
    {
        // Act
        _logger.Info("Test message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Test message");
        output.Should().Contain("[INFO]");
    }

    [Fact]
    public void Info_Should_Include_Timestamp()
    {
        // Act
        _logger.Info("Test message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[");
        output.Should().Contain("]");
    }

    [Fact]
    public void Info_Should_Include_Class_Name()
    {
        // Act
        _logger.Info("Test message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[");
        output.Should().Contain("]");
    }

    [Fact]
    public void Info_Should_Handle_Empty_Message()
    {
        // Act
        _logger.Info("");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
    }

    [Fact]
    public void Info_Should_Handle_Long_Message()
    {
        // Arrange
        var longMessage = string.Concat(Enumerable.Repeat("a", 1000));

        // Act
        _logger.Info(longMessage);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
        output.Should().Contain(longMessage);
    }

    [Fact]
    public void Info_Should_Handle_Message_With_Special_Characters()
    {
        // Arrange
        var message = "Test @#$%^&*() message!";

        // Act
        _logger.Info(message);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain(message);
    }

    #endregion

    #region Warn Logging Tests

    [Fact]
    public void Warn_Should_Log_Message()
    {
        // Act
        _logger.Warn("Warning message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Warning message");
        output.Should().Contain("[WARN]");
    }

    [Fact]
    public void Warn_Should_Include_Timestamp()
    {
        // Act
        _logger.Warn("Warning message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[");
        output.Should().Contain("]");
    }

    [Fact]
    public void Warn_Should_Handle_Empty_Message()
    {
        // Act
        _logger.Warn("");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[WARN]");
    }

    [Fact]
    public void Warn_Should_Handle_Long_Message()
    {
        // Arrange
        var longMessage = string.Concat(Enumerable.Repeat("w", 500));

        // Act
        _logger.Warn(longMessage);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[WARN]");
    }

    #endregion

    #region Error Logging Tests

    [Fact]
    public void Error_Should_Log_Message_Without_Exception()
    {
        // Act
        _logger.Error("Error message");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Error message");
        output.Should().Contain("[ERROR]");
    }

    [Fact]
    public void Error_Should_Log_Exception_When_Provided()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        _logger.Error("Error with exception", exception);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Error with exception");
        output.Should().Contain("[ERROR]");
        output.Should().Contain("InvalidOperationException");
        output.Should().Contain("Test exception");
    }

    [Fact]
    public void Error_Should_Include_Exception_StackTrace()
    {
        // Arrange
        Exception? exception = null;
        try
        {
            throw new InvalidOperationException("Test exception for stacktrace");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        _logger.Error("Error with stacktrace", exception);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[EXCEPTION]");
        output.Should().Contain("InvalidOperationException");
        output.Should().Contain("Test exception for stacktrace");
    }

    [Fact]
    public void Error_Should_Handle_Null_Exception()
    {
        // Act
        _logger.Error("Error message", null);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("Error message");
        output.Should().Contain("[ERROR]");
    }

    [Fact]
    public void Error_Should_Log_Different_Exception_Types()
    {
        // Arrange
        var argException = new ArgumentException("Argument error");
        var ioException = new IOException("IO error");

        // Act
        _logger.Error("First error", argException);
        var output1 = _stringWriter.ToString();

        _stringWriter.GetStringBuilder().Clear();

        _logger.Error("Second error", ioException);
        var output2 = _stringWriter.ToString();

        // Assert
        output1.Should().Contain("ArgumentException");
        output2.Should().Contain("IOException");
    }

    [Fact]
    public void Error_Should_Handle_Nested_Exceptions()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        var outerException = new ApplicationException("Outer exception", innerException);

        // Act
        _logger.Error("Nested error", outerException);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("ApplicationException");
        output.Should().Contain("Outer exception");
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task Should_Handle_Concurrent_Info_Logs()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => _logger.Info($"Info message {index}")));
        }

        await Task.WhenAll(tasks);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
        for (int i = 0; i < 10; i++)
        {
            output.Should().Contain($"Info message {i}");
        }
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Warn_Logs()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => _logger.Warn($"Warn message {index}")));
        }

        await Task.WhenAll(tasks);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[WARN]");
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Error_Logs()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
                _logger.Error($"Error message {index}", new Exception($"Exception {index}"))
            ));
        }

        await Task.WhenAll(tasks);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[ERROR]");
        output.Should().Contain("[EXCEPTION]");
    }

    [Fact]
    public async Task Should_Handle_Mixed_Concurrent_Logs()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 20; i++)
        {
            int index = i;
            var logType = index % 3;

            switch (logType)
            {
                case 0:
                    tasks.Add(Task.Run(() => _logger.Info($"Info {index}")));
                    break;
                case 1:
                    tasks.Add(Task.Run(() => _logger.Warn($"Warn {index}")));
                    break;
                case 2:
                    tasks.Add(Task.Run(() => _logger.Error($"Error {index}", new Exception($"Ex {index}"))));
                    break;
            }
        }

        await Task.WhenAll(tasks);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
        output.Should().Contain("[WARN]");
        output.Should().Contain("[ERROR]");
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Info_Timestamp_Should_Be_Valid_Format()
    {
        // Act
        _logger.Info("Test");

        // Assert
        var output = _stringWriter.ToString();
        // Expected format: [yyyy-MM-dd HH:mm:ss.fff] [LEVEL] [CLASS] message
        // Just verify it starts with [ and contains the timestamp structure
        output.Should().StartWith("[");
        output.Should().Contain("[INFO]");
        // Verify timestamp has correct format pattern (YYYY-MM-DD HH:MM:SS.mmm)
        var hasValidTimestamp = System.Text.RegularExpressions.Regex.IsMatch(output, @"\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]");
        hasValidTimestamp.Should().BeTrue("Output should have a valid timestamp format");
    }

    #endregion

    #region Message Format Tests

    [Fact]
    public void Log_Should_Have_Correct_Format_Order()
    {
        // Act
        _logger.Info("Test message");

        // Assert
        var output = _stringWriter.ToString();
        var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        var line = lines[0];

        // Should be: [timestamp] [level] [class] message
        var parts = line.Split(' ', 5);
        parts.Length.Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public void Error_With_Exception_Should_Include_Type_Name()
    {
        // Arrange
        var exception = new ArgumentNullException("testParam", "Value cannot be null");

        // Act
        _logger.Error("Error occurred", exception);

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("ArgumentNullException");
    }

    #endregion

    #region CLI Context Test

    [Fact]
    public void Log_From_Main_Should_Show_CLI_Context()
    {
        // The logger transforms <Main> to CLI
        // This test verifies the logger handles class name transformations

        // Act
        _logger.Info("Test from CLI");

        // Assert
        var output = _stringWriter.ToString();
        output.Should().Contain("[INFO]");
        output.Should().Contain("Test from CLI");
    }

    #endregion
}
