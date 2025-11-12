# AI Agent Instructions for Elevator Operator

This document provides essential context for AI agents working in this codebase.

## Architecture Overview

This is a .NET 9-based elevator control system following clean architecture with these layers:

- **Domain** (`src/ElevatorOperator.Domain/`): Core business logic, entities, enums, interfaces, exceptions
- **Application** (`src/ElevatorOperator.Application/`): High-level control logic and coordination
- **Infrastructure** (`src/ElevatorOperator.Infrastructure/`): Technical concerns—logging and FIFO scheduling
- **CLI** (`src/ElevatorOperator.CLI/`): Console entry point with dependency injection

## Core Design Principles

**Thread Safety**: The `Elevator` class uses the `Lock` struct (C# 13, monitor-based) for synchronous state management. All shared state access is protected via `lock(_syncLock)`. `FifoScheduler<T>` combines `Lock` with `ConcurrentQueue<T>` for thread-safe request queuing. Operations are **synchronous** with `Thread.Sleep()` for simulated travel/door delays (not under lock).

**Exceptions**: Custom exceptions inherit from `ElevatorOperatorException` and live in `Domain/Exceptions/`:

- `InvalidFloorException`: Floor outside 1-10 range (validated by adapter)
- `InvalidStateTransitionException`: Disallowed state change
- `InvalidPickupAndDestinationException`: Pickup equals destination
- `ElevatorTimeoutException`: Operation timeout via `TryRunWithTimeout()`

**Floor Bounds**: Elevators operate floors 1-10. Validation occurs at adapter layer and controller validation before enqueuing.

## Adapter Pattern (Critical)

The `ElevatorAdapter` in `src/ElevatorOperator.Domain/Adapters/` wraps `IElevator` to add validation and convenience methods without modifying the core `Elevator` class.

**Key rule**: `ElevatorAdapter.MoveToFloor(int floor)` moves the elevator step-by-step (alternating `MoveUp()` / `MoveDown()`) until reaching the target, then opens/closes doors. The adapter also validates floor ranges and handles door state.

**Adding adapter behavior**:

1. Implement `IElevatorAdapter` interface (which extends `IElevator`)
2. Delegate operations to `_inner` via composition
3. Add validation in override methods (e.g., `AddRequest` checks floor range, throws `InvalidFloorException` for floors 0 or 11)
4. Add helper methods with synchronous loop control (e.g., `MoveToFloor` is the primary convenience method)
5. Protect all operations with `lock (_adapterLock)`
6. Test with both happy path and boundary cases (floors 0, 11, etc.)

## State Management

States (`Idle`, `MovingUp`, `MovingDown`, `DoorOpen`) are managed via `ElevatorState` enum. The `Elevator.State` property validates transitions via `IsValidStateTransition()` before changing state. State transitions are protected by `lock(_syncLock)`, but `Thread.Sleep()` delays (simulating travel/door time) occur **outside** the lock to prevent blocking other operations.

## Request Processing Flow

1. CLI/caller invokes `RequestElevator(pickup, destination)` → creates `ElevatorRequest` value object (validates pickup ≠ destination)
2. Request enqueued to `IScheduler<ElevatorRequest>` (FIFO order)
3. `ProcessRequests()` iterates through scheduled requests:
   - Moves to pickup floor via `adapter.MoveToFloor(pickup)`
   - Opens/closes doors at pickup
   - Moves to destination floor via `adapter.MoveToFloor(destination)`
   - **Optimization**: Peeks next request—if its pickup == current destination, keeps doors open to optimize dwell time
   - Otherwise closes doors, returns to idle
4. Logs all operations via injected `ILogger`
5. Retry logic: 1 retry per failed operation (e.g., move, door) with 5-second timeout
6. Continues until `GetPendingCount() == 0`

## Build, Run, Test

```bash
dotnet build
dotnet run --project src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj
dotnet test
```

## Testing Patterns

Tests use **xUnit**, **FluentAssertions**, and **Moq**. See `ElevatorAdapterTests.cs` and `ElevatorControllerTests.cs`:

- Adapters: test with real `Elevator` instances (integration approach) to validate state transitions end-to-end
- Controller: mock `IElevator`, `IScheduler<ElevatorRequest>`, `ILogger` to isolate orchestration logic
- Named `Should_*` for test method clarity
- Use `[Fact]` for unit tests, verify mocks with `Times.Once`, `Times.AtLeastOnce`
- Key test scenarios: floor boundaries (0, 11), invalid state transitions, timeout handling, retry logic

## Common Tasks

**Add elevator feature**:

1. Add method/property to `IElevator` interface
2. Implement in `Elevator` class with thread-safe locking via `Lock`
3. Wrap/enhance in `ElevatorAdapter` if validation/convenience needed
4. Call from `ElevatorController` or CLI
5. Add tests (real elevator for adapters, mocked for controller)

**Modify state transitions**:

1. Update `IsValidStateTransition()` logic in `Elevator`
2. Test invalid transitions throw `InvalidStateTransitionException`
3. Verify `ElevatorController` respects new rules

**Implement door optimization**:

In `ElevatorController.HandleRequest()`, `PeekNext()` on scheduler to check if next pickup matches current destination. If yes, keep doors open (`CloseDoor()` is skipped). This reduces dwell time at frequently-used intermediate floors.
