# AI Agent Instructions for Elevator Operator

This document provides essential context for AI agents working in this codebase.

## Core Design Principles

1. **Thread Safety First**
   - Always use thread-safe collections (`ConcurrentQueue<T>`, etc.)
   - Protect shared state with appropriate locks
   - Design state transitions to be atomic

2. **Error Handling**
   - Use custom exceptions for domain-specific errors
   - Validate all inputs at system boundaries
   - Include proper context in error messages

## Project Architecture

This is a .NET 9-based elevator control system following clean architecture:

- **Domain Layer** (`src/ElevatorOperator.Domain/`):
  - Core business logic and entities
  - Key interface: `IElevator` defines elevator behavior contract
  - Uses enums (`ElevatorState`, `ElevatorDirection`) for state management
  - Contains domain exceptions and validation rules

- **Application Layer** (`src/ElevatorOperator.Application/`):
  - Contains elevator control logic
  - `IElevatorController` interface defines high-level control operations
  - Manages elevator state transitions and request processing

- **Infrastructure Layer** (`src/ElevatorOperator.Infrastructure/`):
  - Implements cross-cutting concerns without business logic
  - Provides logging implementation in `Logging/`
  - Contains FIFO elevator scheduling in `Scheduling/`
  - Use thread-safe collections and proper synchronization
  - Focus on technical implementations and performance monitoring

- **CLI Layer** (`src/ElevatorOperator.CLI/`):
  - Console interface for elevator system simulation
  - Entry point through `Program.cs`

## Key Development Workflows

### Building and Running

```bash
# Build the solution
dotnet build

# Run the CLI application
dotnet run --project src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj
```

### Testing

```bash
# Run tests
dotnet test
```

## Development Patterns

1. **State Management**
   - Elevator states are managed through the `ElevatorState` enum
   - State transitions should be explicit and handled in the controller
   - Implementation example:
   ```csharp
   public class Elevator : IElevator
   {
       private readonly object _stateLock = new();
       private ElevatorState _state;

       public ElevatorState State
       {
           get => _state;
           set
           {
               lock (_stateLock)
               {
                   if (!IsValidStateTransition(_state, value))
                       throw new InvalidStateTransitionException(_state, value);
                   _state = value;
               }
           }
       }
   }
   ```

2. **Request Processing**
   - Use Infrastructure layer's FIFO scheduler for request management
   - Log operations through Infrastructure logger
   - Request processing logic is coordinated between ElevatorController and Infrastructure
   - Implementation example:
   ```csharp
   public class ElevatorController : IElevatorController
   {
       private readonly IElevatorScheduler _scheduler;
       private readonly IElevatorLogger _logger;

       public ElevatorController(IElevatorScheduler scheduler, IElevatorLogger logger)
       {
           _scheduler = scheduler;
           _logger = logger;
       }

       public async Task RequestElevator(int floor, ElevatorDirection direction)
       {
           var request = new ElevatorRequest(floor, direction);
           await _scheduler.ScheduleRequest(request);
           _logger.LogRequest(floor, direction);
       }

       public async Task ProcessRequests(CancellationToken ct)
       {
           await _scheduler.ProcessScheduledRequests(ct);
       }
   }
   ```

3. **Interface-First Design**
   - All major components have interfaces defined first
   - Implementation classes should be in corresponding service folders
   - Example interface pattern:
   ```csharp
   public interface IElevator
   {
       int CurrentFloor { get; }
       ElevatorState State { get; }
       ConcurrentQueue<int> TargetFloors { get; }
       Task MoveToFloor(int floor, CancellationToken ct);
       ValueTask<bool> TryAddRequest(int floor);
   }
   ```

## Common Tasks

- Adding new elevator functionality:
  1. Define behavior in appropriate interface (`IElevator` or `IElevatorController`)
  2. Implement in corresponding service class
  3. Add unit tests in `ElevatorOperator.Tests` project

- Modifying state transitions:
  1. Update relevant enums in `Domain/Enums/` if needed
  2. Implement state change logic in `ElevatorController`
  3. Ensure state consistency in tests
