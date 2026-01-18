# Verification Testing Implementation Plan

## Overview

This plan covers comprehensive test coverage for KnockOff's three-level verification system. The current test suite has significant gaps, particularly at the stub level where `Verify()` and `VerifyAll()` have zero test coverage despite being core API features.

---

## Verification Architecture Summary

KnockOff implements verification at three distinct levels, each building on the previous:

### Level 1: `Times.Verify(int actualCount)`

**Location:** `src/KnockOff/Times.cs:58-66`

The foundational verification primitive. A pure function that checks if an actual call count satisfies a constraint:

| TimesKind | Verification Logic |
|-----------|-------------------|
| `Exactly(n)` | `actualCount == n` |
| `Forever` | Always `true` |
| `AtLeast(n)` | `actualCount >= n` |
| `AtMost(n)` | `actualCount <= n` |
| `Never` | `actualCount == 0` |

**Current Coverage:** Partial - `AtLeast`, `AtMost`, and `Never` are tested. `Exactly` and `Forever` verification are NOT tested.

### Level 2: Method Interceptor `Verify()`

**Location:** Generated per interceptor (e.g., `SequenceTestKnockOff.AddInterceptor.Verify()`)

Iterates through all callbacks in the method's sequence and verifies each constraint:

```csharp
public bool Verify()
{
    foreach (var (_, times, tracking) in _sequence)
    {
        if (times.IsForever)
        {
            if (!tracking.WasCalled)
                return false;
        }
        else if (!times.Verify(tracking.CallCount))
            return false;
    }
    return true;
}
```

**Key Behaviors:**
- Returns `true` if no callbacks configured (empty sequence)
- For `Times.Forever`: verifies `WasCalled` is true (at least one call)
- For other Times: delegates to `Times.Verify(tracking.CallCount)`
- Returns `false` if ANY constraint fails

**Current Coverage:** Indirectly tested via `IMethodSequence.Verify()` (2 tests in `SequencingTests.cs`). Direct interceptor verification NOT tested.

### Level 3: Stub-Level `Verify()` and `VerifyAll()`

**Location:** Generated at stub level (e.g., `SequenceTestKnockOff.Verify()`)

Aggregates all method interceptor verifications:

```csharp
public bool Verify()
{
    var result = true;
    result &= Add.Verify();
    result &= DoWork.Verify();
    result &= GetMessage.Verify();
    return result;
}

public void VerifyAll()
{
    if (!Verify())
        throw new VerificationException("One or more method verifications failed.");
}
```

**Key Behaviors:**
- `Verify()` aggregates via `&=`, returning `false` if ANY method fails
- `VerifyAll()` throws `VerificationException` if `Verify()` returns `false`
- Only generated for stubs with method interceptors

**Current Coverage:** ZERO tests.

---

## Existing Test Assets

### Stubs to Reuse

| Stub | Interface | Methods | Use For |
|------|-----------|---------|---------|
| `SequenceTestKnockOff` | `ISequenceTestService` | `Add(int, int)`, `DoWork()`, `GetMessage(string)` | Multi-method verification, void method verification |
| `OverloadTestKnockOff` | `IOverloadTestService` | `Format(string)`, `Format(string, bool)`, `Format(string, int)` | Overload verification scenarios |
| `IndexerTestKnockOff` | `IIndexerTestService` | String indexer, Int32 indexer | NOT USED (indexers don't have Verify) |

### Existing Test Files

| File | Current Coverage |
|------|-----------------|
| `TimesTests.cs` | `Once`, `Twice`, `Exactly`, `Forever` properties; `AtLeast`, `AtMost`, `Never` verification |
| `SequencingTests.cs` | `IMethodSequence.Verify()` via sequence (2 tests) |

---

## File Organization

### Level 1 Tests

**File:** `src/Tests/KnockOffTests/TimesTests.cs` (existing)

Add tests to existing file to maintain cohesion of `Times` struct tests.

### Level 2 and Level 3 Tests

**File:** `src/Tests/KnockOffTests/VerificationTests.cs` (new)

New dedicated file for verification behavior tests. Separates verification testing from sequencing behavior tested in `SequencingTests.cs`.

---

## Implementation Phases

### Phase 1: Level 1 - Times.Verify() Gap Coverage

**File:** `TimesTests.cs`

**Duration:** ~15 minutes

Fill gaps in `Times.Verify()` test coverage:

| Test Name | Description | Expected Result |
|-----------|-------------|-----------------|
| `Exactly_Verify_ReturnsTrue_WhenCountMatches` | `Times.Exactly(3).Verify(3)` | `true` |
| `Exactly_Verify_ReturnsFalse_WhenCountTooLow` | `Times.Exactly(3).Verify(2)` | `false` |
| `Exactly_Verify_ReturnsFalse_WhenCountTooHigh` | `Times.Exactly(3).Verify(4)` | `false` |
| `Once_Verify_ReturnsTrue_WhenCalledOnce` | `Times.Once.Verify(1)` | `true` |
| `Once_Verify_ReturnsFalse_WhenNotCalled` | `Times.Once.Verify(0)` | `false` |
| `Once_Verify_ReturnsFalse_WhenCalledTwice` | `Times.Once.Verify(2)` | `false` |
| `Twice_Verify_ReturnsTrue_WhenCalledTwice` | `Times.Twice.Verify(2)` | `true` |
| `Forever_Verify_AlwaysReturnsTrue` | `Times.Forever.Verify(0)`, `.Verify(100)` | Both `true` |

**Verification:** All existing tests pass + new tests pass.

---

### Phase 2: Level 2 - Method Interceptor Verify()

**File:** `VerificationTests.cs` (new)

**Duration:** ~30 minutes

**Dependencies:** Uses `SequenceTestKnockOff` from `SequencingTests.cs`

#### 2A: Basic Interceptor Verification (No Callbacks)

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_ReturnsTrue_WhenNoCallbacksConfigured` | Fresh stub, no `OnCall()` | `true` |

#### 2B: Forever Constraint Verification

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_ReturnsTrue_WhenForeverConstraintCalled` | `OnCall(..., Times.Forever)`, method called | `true` |
| `MethodInterceptor_Verify_ReturnsFalse_WhenForeverConstraintNotCalled` | `OnCall(..., Times.Forever)`, method NOT called | `false` |

**Note:** `OnCall(callback)` without explicit Times uses `Times.Forever` internally, so this tests the implicit Forever case.

#### 2C: Exact Count Verification

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_ReturnsTrue_WhenOnceConstraintSatisfied` | `OnCall(..., Times.Once)`, called once | `true` |
| `MethodInterceptor_Verify_ReturnsFalse_WhenOnceConstraintNotCalled` | `OnCall(..., Times.Once)`, not called | `false` |
| `MethodInterceptor_Verify_ReturnsTrue_WhenExactlyNConstraintSatisfied` | `OnCall(..., Times.Exactly(3))`, called 3 times | `true` |

#### 2D: Sequence Verification (Multiple Callbacks)

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_ReturnsTrue_WhenAllSequenceConstraintsSatisfied` | `OnCall(..., Once).ThenCall(..., Once)`, both called | `true` |
| `MethodInterceptor_Verify_ReturnsFalse_WhenFirstConstraintNotSatisfied` | `OnCall(..., Twice).ThenCall(..., Once)`, called once | `false` |
| `MethodInterceptor_Verify_ReturnsFalse_WhenLastConstraintNotSatisfied` | `OnCall(..., Once).ThenCall(..., Once)`, called once total | `false` |

#### 2E: Void Method Verification

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_WorksWithVoidMethods` | `stub.DoWork.OnCall(..., Times.Once)`, called | `true` |

#### 2F: Reset Interaction

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `MethodInterceptor_Verify_ReturnsFalse_AfterReset_WhenConstraintNoLongerSatisfied` | Configure Once, call, verify (true), reset, verify again | `false` |
| `MethodInterceptor_Verify_ReturnsTrue_AfterReset_WhenRecalled` | Configure Once, call, reset, call again, verify | `true` |

**Verification:** New file compiles and all tests pass.

---

### Phase 3: Level 3 - Stub-Level Verify() and VerifyAll()

**File:** `VerificationTests.cs`

**Duration:** ~45 minutes

**Dependencies:** Uses `SequenceTestKnockOff` and `OverloadTestKnockOff`

#### 3A: Stub Verify() - Returns Bool

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `StubVerify_ReturnsTrue_WhenNoMethodsConfigured` | Fresh stub, no callbacks on any method | `true` |
| `StubVerify_ReturnsTrue_WhenAllMethodsSatisfied` | Configure and satisfy all methods | `true` |
| `StubVerify_ReturnsFalse_WhenOneMethodFails` | Configure 3 methods, satisfy 2 | `false` |
| `StubVerify_ReturnsFalse_WhenAllMethodsFail` | Configure 3 methods, satisfy none | `false` |
| `StubVerify_ReturnsTrue_WhenMixedConfiguredAndUnconfigured` | Configure 1 method (satisfied), leave 2 unconfigured | `true` |

#### 3B: Stub VerifyAll() - Throws Exception

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `StubVerifyAll_DoesNotThrow_WhenAllSatisfied` | All methods satisfied | No exception |
| `StubVerifyAll_ThrowsVerificationException_WhenFails` | One method unsatisfied | `VerificationException` |
| `StubVerifyAll_ExceptionMessage_ContainsExpectedText` | Verify exception message content | Message contains "verification" |

#### 3C: Overloaded Methods

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `StubVerify_ReturnsTrue_WithOverloadedMethods_AllOverloadsSatisfied` | Configure all 3 Format overloads, satisfy all | `true` |
| `StubVerify_ReturnsFalse_WithOverloadedMethods_OneOverloadNotSatisfied` | Configure 3 overloads, satisfy 2 | `false` |
| `StubVerify_ReturnsTrue_WithOverloadedMethods_SomeUnconfigured` | Configure 1 overload (satisfied), leave 2 unconfigured | `true` |

**Verification:** All tests pass; no regression in existing tests.

---

### Phase 4: Edge Cases and Boundary Conditions

**File:** `VerificationTests.cs`

**Duration:** ~20 minutes

| Test Name | Scenario | Expected |
|-----------|----------|----------|
| `Verify_BeforeAnyCalls_WithForeverConstraint_ReturnsFalse` | Configure Forever, verify before calling | `false` |
| `Verify_BeforeAnyCalls_WithNoConstraints_ReturnsTrue` | Fresh stub, verify immediately | `true` |
| `Verify_WithExactlyZero_BehavesLikeNever` | `Times.Exactly(0).Verify(0)` vs `Times.Never.Verify(0)` | Both `true` |
| `Verify_AfterMultipleResets_StillWorks` | Call, reset, call, reset, verify | Consistent behavior |
| `StubVerify_WithStrictMode_StillVerifiesCorrectly` | `Strict = true`, verify unconfigured methods | `true` (Strict doesn't affect Verify) |

**Verification:** All tests pass; comprehensive coverage confirmed.

---

## Test Code Patterns

### Pattern: Testing Method Interceptor Verify()

```csharp
[Fact]
public void MethodInterceptor_Verify_Scenario()
{
    // Arrange
    var stub = new SequenceTestKnockOff();
    stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

    // Act - optionally call the method
    ISequenceTestService svc = stub;
    svc.Add(1, 2);

    // Assert - verify directly on interceptor
    Assert.True(stub.Add.Verify());
}
```

### Pattern: Testing Stub-Level Verify()

```csharp
[Fact]
public void StubVerify_Scenario()
{
    // Arrange
    var stub = new SequenceTestKnockOff();
    stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
    stub.DoWork.OnCall(ko => { }, Times.Once);

    // Act
    ISequenceTestService svc = stub;
    svc.Add(1, 2);
    svc.DoWork();

    // Assert - verify at stub level
    Assert.True(stub.Verify());
}
```

### Pattern: Testing VerifyAll() Exception

```csharp
[Fact]
public void StubVerifyAll_ThrowsWhenFails()
{
    // Arrange
    var stub = new SequenceTestKnockOff();
    stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
    // Don't call Add - constraint unsatisfied

    // Act & Assert
    var ex = Assert.Throws<VerificationException>(() => stub.VerifyAll());
    Assert.Contains("verification", ex.Message, StringComparison.OrdinalIgnoreCase);
}
```

---

## Summary Checklist

### Phase 1: TimesTests.cs
- [ ] `Exactly_Verify_ReturnsTrue_WhenCountMatches`
- [ ] `Exactly_Verify_ReturnsFalse_WhenCountTooLow`
- [ ] `Exactly_Verify_ReturnsFalse_WhenCountTooHigh`
- [ ] `Once_Verify_ReturnsTrue_WhenCalledOnce`
- [ ] `Once_Verify_ReturnsFalse_WhenNotCalled`
- [ ] `Once_Verify_ReturnsFalse_WhenCalledTwice`
- [ ] `Twice_Verify_ReturnsTrue_WhenCalledTwice`
- [ ] `Forever_Verify_AlwaysReturnsTrue`

### Phase 2: VerificationTests.cs - Method Interceptor
- [ ] `MethodInterceptor_Verify_ReturnsTrue_WhenNoCallbacksConfigured`
- [ ] `MethodInterceptor_Verify_ReturnsTrue_WhenForeverConstraintCalled`
- [ ] `MethodInterceptor_Verify_ReturnsFalse_WhenForeverConstraintNotCalled`
- [ ] `MethodInterceptor_Verify_ReturnsTrue_WhenOnceConstraintSatisfied`
- [ ] `MethodInterceptor_Verify_ReturnsFalse_WhenOnceConstraintNotCalled`
- [ ] `MethodInterceptor_Verify_ReturnsTrue_WhenExactlyNConstraintSatisfied`
- [ ] `MethodInterceptor_Verify_ReturnsTrue_WhenAllSequenceConstraintsSatisfied`
- [ ] `MethodInterceptor_Verify_ReturnsFalse_WhenFirstConstraintNotSatisfied`
- [ ] `MethodInterceptor_Verify_ReturnsFalse_WhenLastConstraintNotSatisfied`
- [ ] `MethodInterceptor_Verify_WorksWithVoidMethods`
- [ ] `MethodInterceptor_Verify_ReturnsFalse_AfterReset_WhenConstraintNoLongerSatisfied`
- [ ] `MethodInterceptor_Verify_ReturnsTrue_AfterReset_WhenRecalled`

### Phase 3: VerificationTests.cs - Stub Level
- [ ] `StubVerify_ReturnsTrue_WhenNoMethodsConfigured`
- [ ] `StubVerify_ReturnsTrue_WhenAllMethodsSatisfied`
- [ ] `StubVerify_ReturnsFalse_WhenOneMethodFails`
- [ ] `StubVerify_ReturnsFalse_WhenAllMethodsFail`
- [ ] `StubVerify_ReturnsTrue_WhenMixedConfiguredAndUnconfigured`
- [ ] `StubVerifyAll_DoesNotThrow_WhenAllSatisfied`
- [ ] `StubVerifyAll_ThrowsVerificationException_WhenFails`
- [ ] `StubVerifyAll_ExceptionMessage_ContainsExpectedText`
- [ ] `StubVerify_ReturnsTrue_WithOverloadedMethods_AllOverloadsSatisfied`
- [ ] `StubVerify_ReturnsFalse_WithOverloadedMethods_OneOverloadNotSatisfied`
- [ ] `StubVerify_ReturnsTrue_WithOverloadedMethods_SomeUnconfigured`

### Phase 4: VerificationTests.cs - Edge Cases
- [ ] `Verify_BeforeAnyCalls_WithForeverConstraint_ReturnsFalse`
- [ ] `Verify_BeforeAnyCalls_WithNoConstraints_ReturnsTrue`
- [ ] `Verify_WithExactlyZero_BehavesLikeNever`
- [ ] `Verify_AfterMultipleResets_StillWorks`
- [ ] `StubVerify_WithStrictMode_StillVerifiesCorrectly`

---

## Notes

### Reusing Existing Stubs

The plan intentionally reuses `SequenceTestKnockOff` and `OverloadTestKnockOff` from `SequencingTests.cs`. These stubs are defined in that file's `#region Test Interface and Stub` section. The new `VerificationTests.cs` file will reference these same types since they're in the `KnockOff.Tests` namespace.

### No New Interfaces Required

All test scenarios can be covered with existing stubs:
- Multi-method verification: `SequenceTestKnockOff` (3 methods)
- Void method verification: `SequenceTestKnockOff.DoWork()`
- Overload verification: `OverloadTestKnockOff` (3 overloads of `Format`)

### Sequence vs Interceptor Verify()

There are two `Verify()` methods to test:
1. `IMethodSequence<T>.Verify()` - Already tested in `SequencingTests.cs`
2. `{Method}Interceptor.Verify()` - Direct interceptor verification (this plan)

Both delegate to the same internal logic, but testing the interceptor directly ensures the public API works as documented.
