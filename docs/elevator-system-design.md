# Elevator System Design

## Problem Overview

Design and implement an elevator system that can handle passenger requests efficiently.
You'll need to create classes that manage elevator operations, handle multiple requests,
and ensure thread safety for concurrent operations.

## Core Requirements

- Implement an Elevator class that represents a single elevator
- Implement an ElevatorController class that manages elevator operations
- Handle passenger requests (pickup and destination floors)
- Ensure thread-safe operations for concurrent requests
- Implement a basic scheduling algorithm

## Difficulty Levels

### Easy Level: Single Elevator System

**Requirements:**

- Design a system with one elevator serving floors 1- 10
- The elevator should handle requests to go up or down
- Implement basic states: Idle, MovingUp, MovingDown, DoorOpen
- Handle pickup requests (floor + direction) and destination requests
- Implement a simple FIFO (First In, First Out) scheduling algorithm

**Key Classes to Implement:**

class Elevator:
```
- CurrentFloor: int
- State: ElevatorState
- TargetFloors: List<int>
- methods: MoveUp(), MoveDown(), OpenDoor(), CloseDoor(), AddRequest()
```

class ElevatorController:
```
- Elevator: Elevator
- methods: RequestElevator(floor, direction), ProcessRequests()
```

**Expected Features:**

- Basic elevator movement simulation
- Queue management for floor requests
- Simple logging of elevator actions

## Implementation Guidelines

### Thread Safety Considerations

- Use appropriate locks/mutexes for shared resources
- Ensure atomic operations for elevator state changes
- Handle race conditions in request assignment
- Consider using thread-safe collections

### Performance Requirements

- System should handle 100+ concurrent requests efficiently
- Response time for elevator assignment should be < 100ms
- Memory usage should remain reasonable under load

### Error Handling

- Handle invalid floor requests gracefully
- Implement timeouts for stuck elevators
- Add proper exception handling for concurrent operations
