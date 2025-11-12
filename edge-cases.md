# Edge Cases - Elevator Operator System

## Overview
This document tracks edge cases and potential issues in the elevator system, organized by fix effort and severity.

## Edge Cases by Fix Effort

### 🟢 Low Effort Fixes

| Edge Case | Severity | Impact | Status | File |
|-----------|----------|--------|--------|------|
| Queue empty race | 🔴 Critical | Missed pulse between check and Monitor.Wait() | ✅ FIXED | `ElevatorController.cs` |
| GetNext() null crash | 🔴 Critical | NullReferenceException if request is null | ✅ FIXED | `ElevatorController.cs` |
| Multiple ProcessRequests() | 🔴 Critical | Request duplication if called by multiple threads | ✅ FIXED | `ElevatorController.cs` |
| Floor 0/11 bypass | 🟡 Medium | Direct Elevator.AddRequest() bypasses adapter validation | ✅ FIXED | `Elevator.cs` |
| Logger color bleed | 🟡 Medium | Color codes not thread-safe, bleeds between threads | ✅ FIXED | `Logger.cs` |

### 🟡 Medium Effort Fixes

| Edge Case | Severity | Impact | Status | File |
|-----------|----------|--------|--------|------|
| Concurrent door ops | 🔴 Critical | State corruption if request arrives during door ops | ✅ FIXED | `ElevatorController.cs` |
| CancellationToken ignored | 🔴 Critical | Doesn't shut down cleanly, waits for Thread.Sleep() | ✅ FIXED | `Elevator.cs` |
| Peek/Get race | 🟡 Medium | Next request optimization fails due to race condition | ✅ FIXED | `FifoScheduler.cs` |

### 🔴 High Effort Fixes

| Edge Case | Severity | Impact | Status | File |
|-----------|----------|--------|--------|------|
| Timeout state corruption | 🔴 Critical | Stuck elevator if operation times out mid-move | ❌ TODO | `ElevatorController.cs` |

## Detailed Descriptions

### Low Effort

#### 1. Queue Empty Race Condition ✅
**Location**: `ElevatorController.ProcessRequests()`
**Status**: FIXED - Restructured to call `GetNext()` immediately
**Impact**: Race window eliminated. Pulse signal cannot be missed.

#### 2. GetNext() Null Crash ✅
**Location**: `ElevatorController.ProcessRequests()`
**Status**: FIXED - Already has null check before `HandleRequest()`
**Impact**: No crash risk.

#### 3. Multiple ProcessRequests() Calls ✅
**Location**: `ElevatorController` class
**Status**: FIXED - Added `_isProcessing` volatile flag with `Interlocked.CompareExchange()`
**Impact**: Prevents duplicate request processing.

#### 4. Floor 0/11 Bypass ✅
**Location**: `Elevator.AddRequest()`
**Status**: FIXED - Validates floor range before adding
**Impact**: All floor requests are validated at the domain level.

#### 5. Logger Color Bleed ✅
**Location**: `Logger.Log()` method
**Status**: FIXED - Color operations now atomic within `lock (Console.Out)`
**Impact**: No color bleeding between threads.

### Medium Effort

#### 6. Concurrent Door Operations ✅
**Location**: `ElevatorController.HandleRequest()` and `SafeDoorOperation()`
**Status**: FIXED - Added `AutoResetEvent` for door operation coordination
**Implementation**: 
- Reset event at start of door operation
- Set event when door operation completes (in finally block)
- Provides synchronization point to coordinate door state changes
**Impact**: Door operations now have explicit coordination signal to prevent state corruption.

#### 7. CancellationToken Ignored ✅
**Location**: `IElevator`, `Elevator`, `IElevatorAdapter`, `ElevatorAdapter`
**Status**: FIXED - Added cancellation-aware overloads to all movement/door methods
**Implementation**:
- Added `MoveUp(CancellationToken ct)`, `MoveDown(CancellationToken ct)`, `OpenDoor(CancellationToken ct)`, `CloseDoor(CancellationToken ct)` methods
- Replaced `Thread.Sleep()` with `Task.Delay(delay, ct).Wait(ct)` for respecting cancellation
- Check `ct.ThrowIfCancellationRequested()` before state changes and after delays
- Added `MoveToFloor(int floor, CancellationToken ct)` to adapter with cancellation checks in loop
**Impact**: 
- ProcessRequests() can now pass CancellationToken to all elevator operations
- Shutdown can be responsive and clean
- Operations respond immediately to cancellation requests

#### 8. Peek/Get Race Condition ✅
**Location**: `IScheduler<T>` and `FifoScheduler<T>`
**Status**: FIXED - Added atomic `GetCurrentAndPeekNext()` method
**Implementation**:
- New method atomizes dequeue + peek operation
- Prevents race between separate GetNext() and PeekNext() calls  
- Properly returns both current and next request in single atomic operation
**Impact**: No race condition between request dequeue and peek operations.

### High Effort

#### 9. Timeout State Corruption
**Location**: `ElevatorController.ExecuteWithTimeout()` and `Elevator` movement methods
**Issue**: If operation times out mid-movement, elevator state may be inconsistent (e.g., stuck in MovingUp).
**Fix**: Implement rollback/recovery mechanism or timeout within lock scope.