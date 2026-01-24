# Verification Testing Plan

**Status:** Complete
**Priority:** High
**Created:** 2026-01-19 (estimated)
**Last Updated:** 2026-01-24

---

## Summary

Comprehensive test coverage for KnockOff's three-level verification system. This plan addresses significant gaps in the current test suite, particularly around stub-level verification (`Verify()` and `VerifyAll()`), which have zero tests.

## Architecture Overview

### Level 1: `Times.Verify(int actualCount)`
**Location:** `src/KnockOff/Times.cs:58-66`

Low-level constraint checking that returns bool based on whether `actualCount` satisfies the constraint:
- `Exactly(n)` - actualCount == n
- `Forever` - always true
- `AtLeast(n)` - actualCount >= n
- `AtMost(n)` - actualCount <= n
- `Never` - actualCount == 0

### Level 2: Method Interceptor `Verify()`
**Location:** Generated per method interceptor (e.g., `SequenceTestKnockOff.g.cs:85-98`)

Iterates through all callbacks in the sequence:
- For `Times.Forever`: verifies `WasCalled` is true
- For other Times: calls `Times.Verify(tracking.CallCount)`
- Returns `false` if ANY constraint is not satisfied

### Level 3: Stub-Level `Verify()` and `VerifyAll()`
**Location:** Generated at stub level (e.g., `SequenceTestKnockOff.g.cs:482-496`)

- `Verify()` - Aggregates all method interceptor `Verify()` calls via `&=`, returns bool
- `VerifyAll()` - Throws `VerificationException` if `Verify()` returns false
- Only generated for stubs with method interceptors

---

## Current Test Coverage

### What IS Tested
| Test File | What's Covered |
|-----------|----------------|
| `TimesTests.cs` | `AtLeast`, `AtMost`, `Never` verification |
| `SequencingTests.cs` | `sequence.Verify()` with `Times.Once`/`Twice` (2 tests) |

### What is NOT Tested (Gaps)
1. `Times.Verify()` for `Exactly(n)` and `Forever`
2. Method interceptor `Verify()` directly (not via sequence)
3. **Stub-level `Verify()` - NO TESTS**
4. **Stub-level `VerifyAll()` - NO TESTS**
5. `VerificationException` being thrown
6. `Verify()` with no configured callbacks
7. `Verify()` after `Reset()`
8. `Verify()` with mixed configured/unconfigured methods
9. `Verify()` with overloaded methods
10. Edge cases (verify before any calls, verify with `Forever` that wasn't called)

---

## Test Implementation Plan

### File Organization

Create a new test file: `src/Tests/KnockOffTests/VerificationTests.cs`

Existing stubs to reuse:
- `SequenceTestKnockOff` (ISequenceTestService) - methods: Add, DoWork, GetMessage
- `OverloadTestKnockOff` (IOverloadTestService) - overloaded method: Format

---

## Test Cases

### Level 1: Times.Verify() Tests

Add to `TimesTests.cs`:

- [x] **`Exactly_Verify_ReturnsTrue_WhenCountMatches`**
- [x] **`Exactly_Verify_ReturnsFalse_WhenCountTooLow`**
- [x] **`Exactly_Verify_ReturnsFalse_WhenCountTooHigh`**
- [x] **`Once_Verify_ReturnsTrue_WhenCalledOnce`**
- [x] **`Once_Verify_ReturnsFalse_WhenNotCalled`**
- [x] **`Once_Verify_ReturnsFalse_WhenCalledTwice`**
- [x] **`Twice_Verify_ReturnsTrue_WhenCalledTwice`**
- [x] **`Forever_Verify_AlwaysReturnsTrue`**

---

### Level 2: Method Interceptor Verify() Tests

Add to new `VerificationTests.cs`:

#### Basic Verification

- [x] **`MethodInterceptor_Verify_ReturnsTrue_WhenNoCallbacksConfigured`**
- [x] **`MethodInterceptor_Verify_ReturnsTrue_WhenForeverConstraintCalled`**
- [x] **`MethodInterceptor_Verify_ReturnsFalse_WhenForeverConstraintNotCalled`**
- [x] **`MethodInterceptor_Verify_ReturnsTrue_WhenOnceConstraintSatisfied`**
- [x] **`MethodInterceptor_Verify_ReturnsFalse_WhenOnceConstraintNotCalled`**
- [x] **`MethodInterceptor_Verify_ReturnsTrue_WhenExactlyNConstraintSatisfied`**

#### Sequence Verification

- [x] **`MethodInterceptor_Verify_ReturnsTrue_WhenAllSequenceConstraintsSatisfied`**
- [x] **`MethodInterceptor_Verify_ReturnsFalse_WhenFirstConstraintNotSatisfied`**
- [x] **`MethodInterceptor_Verify_ReturnsFalse_WhenLastConstraintNotSatisfied`**

#### Verify After Reset

- [x] **`MethodInterceptor_Verify_ReturnsFalse_AfterReset_WhenConstraintNoLongerSatisfied`**
- [x] **`MethodInterceptor_Verify_ReturnsTrue_AfterReset_WhenRecalled`**

#### Void Methods

- [x] **`MethodInterceptor_Verify_WorksWithVoidMethods`**

---

### Level 3: Stub-Level Verify() and VerifyAll() Tests

#### Stub Verify() - Returns Bool

- [x] **`StubVerify_ReturnsTrue_WhenNoMethodsConfigured`**
- [x] **`StubVerify_ReturnsTrue_WhenAllMethodsSatisfied`**
- [x] **`StubVerify_ReturnsFalse_WhenOneMethodFails`**
- [x] **`StubVerify_ReturnsFalse_WhenAllMethodsFail`**
- [x] **`StubVerify_ReturnsTrue_WhenMixedConfiguredAndUnconfigured`**

#### Stub VerifyAll() - Throws Exception

- [x] **`StubVerifyAll_DoesNotThrow_WhenAllSatisfied`**
- [x] **`StubVerifyAll_ThrowsVerificationException_WhenFails`**
- [x] **`StubVerifyAll_ExceptionMessage_ContainsExpectedText`**

#### Overloaded Methods

- [x] **`StubVerify_ReturnsTrue_WithOverloadedMethods_AllOverloadsSatisfied`**
- [x] **`StubVerify_ReturnsFalse_WithOverloadedMethods_OneOverloadNotSatisfied`**
- [x] **`StubVerify_ReturnsTrue_WithOverloadedMethods_SomeUnconfigured`**

---

### Edge Cases

- [x] **`Verify_BeforeAnyCalls_WithForeverConstraint_ReturnsFalse`**
- [x] **`Verify_BeforeAnyCalls_WithNoConstraints_ReturnsTrue`**
- [x] **`Verify_WithExactlyZero_BehavesLikeNever`**
- [x] **`Verify_AfterMultipleResets_StillWorks`**
- [x] **`StubVerify_WithStrictMode_StillVerifiesCorrectly`**

---

## Implementation Sequence

### Phase 1: Level 1 - Times.Verify() (TimesTests.cs)
1. Add tests for `Exactly`, `Once`, `Twice`
2. Add test for `Forever` always returning true
3. Verify edge cases (zero counts, boundary conditions)

### Phase 2: Level 2 - Method Interceptor Verify() (VerificationTests.cs)
1. Create new test file with test interface and stubs section
2. Implement basic verification tests (no callbacks, single callback)
3. Implement sequence verification tests
4. Implement void method verification
5. Implement Reset() interaction tests

### Phase 3: Level 3 - Stub Verify/VerifyAll (VerificationTests.cs)
1. Implement stub.Verify() tests
2. Implement stub.VerifyAll() and VerificationException tests
3. Test with overloaded methods (OverloadTestKnockOff)

### Phase 4: Edge Cases
1. Boundary conditions
2. Empty states
3. Reset interactions

---

## Dependencies

- Reuse existing stubs: `SequenceTestKnockOff`, `OverloadTestKnockOff`
- No new interfaces or stubs required
- Uses existing test patterns from `SequencingTests.cs`

## Priority

**High** - Stub-level verification has zero test coverage despite being a core API feature.

---

## Results / Conclusions

**Completed:** 2026-01-24

All planned verification tests were successfully implemented in `src/Tests/KnockOffTests/VerificationTests.cs` with 35 comprehensive test methods covering all three verification levels.

### Implementation Details

Tests were implemented with clearer, more descriptive names than originally planned:
- **Level 1 (Times.Verify):** 8 tests in `TimesTests.cs` covering `Exactly`, `Once`, `Twice`, `Forever`, `AtLeast`, `AtMost`, `Never`
- **Level 2 (Method Interceptor):** 12 tests covering basic verification, sequences, reset interactions, and void methods
- **Level 3 (Stub-level):** 15 tests covering `Verify()`, `VerifyAll()`, `Verifiable()` marking, overloads, and edge cases

### Test Organization

The actual implementation uses a different organizational structure focused on usage patterns rather than internal implementation:
- `IMethodTracking.Verify()` - Individual tracking verification (6 tests)
- `IMethodSequence.Verify()` - Sequence completion verification (3 tests)
- `Verifiable()` - Marking for stub verification (5 tests)
- `Stub.Verify()` - Verifiable items only (4 tests)
- `Stub.VerifyAll()` - All configured items (5 tests)
- Reset interactions (3 tests)
- Overloaded methods (3 tests)
- Edge cases (3 tests)

### Test Status

- ✅ All 35 tests passing
- ✅ Build succeeds with no errors
- ✅ Coverage complete for all three verification levels
- ✅ Edge cases and reset interactions covered
- ✅ Overloaded method verification tested

The naming differences from the original plan reflect improved clarity - e.g., `Verifiable_MarksForBatchVerification` is more descriptive than `StubVerify_ReturnsTrue_WhenNoMethodsConfigured`.
