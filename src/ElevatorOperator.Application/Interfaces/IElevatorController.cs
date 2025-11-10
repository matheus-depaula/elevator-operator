using ElevatorOperator.Domain.Enums;

namespace ElevatorOperator.Application.Interfaces;


public interface IElevatorController
{
    void RequestElevator(int pickup, int destination);
    void ProcessRequests();
}
