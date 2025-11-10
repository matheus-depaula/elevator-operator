# Elevator Operator

A .NET-based elevator control system that efficiently manages elevator operations with thread safety and clean architecture principles.

## Overview

This project implements an elevator control system that:
- Manages single elevator operations
- Handles concurrent passenger requests
- Provides real-time monitoring and logging
- Uses FIFO scheduling for request processing

For detailed technical specifications and architecture details, see the [Requirements Documentation](docs/requirements.md).

Adapter pattern:
- The project uses adapters to extend domain implementations without changing their contracts. See `src/ElevatorOperator.Domain/Adapters/ElevatorAdapter.cs` for an example.
- Adapters add validation and convenience methods (e.g., move-to-target helpers). Keep adapters in the Domain layer and add unit tests when modifying them.

## Prerequisites

- .NET 9 SDK
- Visual Studio 2025+ or Visual Studio Code

## Quick Start

1. Clone the repository:
```bash
git clone https://github.com/yourusername/elevator-operator.git
cd elevator-operator
```

2. Build the solution:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run --project src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj
```

## Project Structure

```
src/
├── ElevatorOperator.Domain         # Core business logic
├── ElevatorOperator.Application    # Application services
├── ElevatorOperator.Infrastructure # Technical implementations
└── ElevatorOperator.CLI            # User interface
```

## Development

- Use Visual Studio 2025+ or VS Code for development
- Follow clean architecture principles
- Ensure thread safety in concurrent operations
- Add unit tests for new features

## Running Tests

```bash
dotnet test
```
