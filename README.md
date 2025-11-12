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

## Building for Production

### Single Platform Build

Build a self-contained executable for your platform:

**macOS (Apple Silicon):**
```bash
dotnet publish src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj -c Release -r osx-arm64 \
  --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o dist/osx-arm64
```

**Windows (x64):**
```bash
dotnet publish src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o dist/win-x64
```

**Linux (x64):**
```bash
dotnet publish src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj -c Release -r linux-x64 \
  --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o dist/linux-x64
```

### Build All Platforms at Once

**Option 1: Using the build script**

Use the provided build script to generate executables for all platforms:
```bash
./scripts/build.sh
```

**Option 2: Using VS Code Tasks**

VS Code tasks are configured in `.vscode/tasks.json`. Build using:
- Press `Cmd+Shift+B` (macOS) or `Ctrl+Shift+B` (Windows/Linux)
- Select one of the available tasks:
  - "Build for macOS (Apple Silicon)" - Creates macOS executable
  - "Build for Windows" - Creates Windows executable
  - "Build for Linux" - Creates Linux executable
  - "Build All Platforms" - Creates executables for all platforms

Both methods create self-contained, single-file executables in the `dist/` directory:
- `dist/osx-arm64/` - macOS executable
- `dist/win-x64/` - Windows executable
- `dist/linux-x64/` - Linux executable

## Running the Application

### Development Mode
```bash
dotnet run --project src/ElevatorOperator.CLI/ElevatorOperator.CLI.csproj
```

### Production (Pre-built Executable)
After building, run the executable from the `dist/` directory:

**macOS:**
```bash
./dist/osx-arm64/ElevatorOperator.CLI
```

**Windows:**
```bash
./dist/win-x64/ElevatorOperator.CLI.exe
```

**Linux:**
```bash
./dist/linux-x64/ElevatorOperator.CLI
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
