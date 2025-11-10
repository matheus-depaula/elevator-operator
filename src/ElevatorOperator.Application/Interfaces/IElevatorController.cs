namespace ElevatorOperator.Application.Interfaces;


public interface IElevatorController
{
    void RequestElevator(int floor);
    void ProcessRequests();
}
