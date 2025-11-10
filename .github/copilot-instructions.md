# AI Agent Instructions for Elevator Operator

This document provides essential context for AI agents working in this codebase.

## Architecture Overview

This is a .NET 9-based elevator control system following clean architecture with these layers:

- **Domain** (`src/ElevatorOperator.Domain/`): Core business logic, entities, enums, interfaces, exceptions
- **Application** (`src/ElevatorOperator.Application/`): High-level control logic and coordination
- **Infrastructure** (`src/ElevatorOperator.Infrastructure/`): Technical concerns—logging and FIFO scheduling
- **CLI** (`src/ElevatorOperator.CLI/`): Console entry point with dependency injection

## Core Design Principles

**Thread Safety**: The `Elevator` class uses `SemaphoreSlim` (not simple locks) for async-safe state management. Acquire locks with timeouts to prevent deadlocks. Use `ConcurrentQueue<T>` in `FifoScheduler`.

**Exceptions**: Custom exceptions inherit from `ElevatorOperatorException` and live in `Domain/Exceptions/`:

- `InvalidFloorException`: Floor outside 1-10 range
- `InvalidStateTransitionException`: Disallowed state change
- `ElevatorTimeoutException`: Lock acquisition timeout

**Floor Bounds**: Elevators operate floors 1-10. Validate at adapter layer (see below).

## Adapter Pattern (Critical)

The `ElevatorAdapter` in `src/ElevatorOperator.Domain/Adapters/` wraps `IElevator` to add validation and convenience methods without modifying the core `Elevator` class.

**Key rule**: When `ElevatorController.ProcessRequests()` needs to move elevators step-by-step toward targets, it must cast to `ElevatorAdapter` and call `MoveOneStepToward()`. See `ElevatorController.cs` line ~17 for the pattern.

**Adding adapter behavior**:

1. Implement `IElevator` interface
2. Delegate to `_inner` (composition pattern)
3. Add validation in override methods (e.g., `AddRequest` checks floor range)
4. Add helper methods (e.g., `MoveToTarget`, `MoveOneStepToward`)
5. Test with both happy path and boundary cases (floors 0, 11, etc.)

## State Management

States (`Idle`, `MovingUp`, `MovingDown`, `DoorOpen`) are managed via `ElevatorState` enum. The `Elevator.State` property uses `SetStateAsync()` to validate transitions with `IsValidStateTransition()` before changing state. Always check state validity before calling move operations.

## Request Processing Flow

1. CLI/caller invokes `RequestElevator(floor)` → adds floor to `Elevator.TargetFloors`
2. `ProcessRequests()` iterates target floors in FIFO order
3. For each target, uses `MoveOneStepToward()` until `CurrentFloor == target`
4. Logs via injected `ILogger`
5. Stops when `TargetFloors.Count == 0`

## Build, Run, Test

```bash
dotnet build
dotnet run --project src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj
dotnet test
```

## Testing Patterns

Tests use **xUnit**, **FluentAssertions**, and **Moq**. See `ElevatorAdapterTests.cs` and `ElevatorControllerTests.cs`:

- Adapters: test with real `Elevator` instances (integration approach)
- Controller: mock `IElevator`, `IElevatorScheduler`, `ILogger`
- Named `Should_*` for test method clarity
- Use `[Fact]` for unit tests, verify mocks with `Times.Once`, `Times.AtLeastOnce`

## Common Tasks

**Add elevator feature**:

1. Add method/property to `IElevator` interface
2. Implement in `Elevator` class with thread-safe locking via `SemaphoreSlim`
3. Wrap/enhance in `ElevatorAdapter` if validation/convenience needed
4. Call from `ElevatorController` or CLI
5. Add tests (real elevator for adapters, mocked for controller)

**Modify state transitions**:

1. Update `IsValidStateTransition()` logic in `Elevator`
2. Test invalid transitions throw `InvalidStateTransitionException`
3. Verify `ElevatorController` respects new rules
