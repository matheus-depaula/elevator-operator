namespace ElevatorOperator.Application.Interfaces;


public interface IElevatorController
{
    void ProcessRequests();
    void RequestElevator(int floor);
}
