namespace ElevatorOperator.Application.Interfaces;

public interface ILogger
{
    void Info(string message, string caller = "");
    void Warn(string message, string caller = "");
    void Error(string message, Exception? ex = null, string caller = "");
}
