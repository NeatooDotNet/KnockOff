# Todo: Update Documentation Samples to Use Verify()/Verifiable() API

## Status: Completed

## Priority: High

## Summary

Update all documentation samples to prefer Verify() and Verifiable() over direct CallCount assertions. The Verifiable() API was implemented in v10.6.0 but documentation samples still use the old CallCount = 1 pattern.

## Problem

The new Verify()/Verifiable() API provides:
- Throwable verification (exceptions vs manual assertions)
- Fluent chaining with OnCall()
- Batch verification via `.Verifiable()` + `stub.Verify()`
- Better error messages via VerificationException

But all current samples still use:
```csharp
Assert.Equal(1, tracking.CallCount);  // Old pattern
```

Instead of:
```csharp
tracking.Verify(Times.Once);          // New pattern
stub.Method.OnCall(cb).Verifiable();  // Mark for batch verify
stub.Verify();                        // Batch check
```

## Scope

### Sample Files Updated (11 files)
Located in `src/Tests/KnockOff.Documentation.Samples/`:
1. VerificationSamples.cs - Added 7 new test cases for all verification patterns
2. MethodsSamples.cs - `tracking.Verify(Times.Exactly(N))`
3. GettingStartedSamples.cs - `.Verifiable()` + `stub.Verify()`
4. ReadmeSamples.cs - `.Verifiable()` + `stub.Verify()`
5. PatternsSamples.cs - All three patterns use `.Verifiable()`
6. InterceptorApiSamples.cs - `tracking.Verify(Times.Once)`
7. AsyncSamples.cs - `.Verifiable()` + `stub.Verify()`
8. GenericMethodsSamples.cs - `tracking.Verify(Times.Exactly(N))`
9. AttributeOptionsSamples.cs - `.Verifiable()` + `stub.Verify()`
10. UserMethodsSamples.cs - `stub.Method.Verify(Times.Once)`
11. MoqMigrationSamples.cs - Moq → KnockOff migration examples

### Markdown Docs Updated (10 files)
Located in `docs/`:
- getting-started.md - Updated standalone/inline examples
- guides/verification.md - Complete rewrite with Quick Start, Direct Verification, Marked Verification sections
- guides/methods.md - New "Using Verify()" and "Using Verifiable()" sections
- guides/properties.md - Added `VerifyGet()`, `VerifySet()`, `MarkVerifiableGet()`, `MarkVerifiableSet()`
- guides/stub-patterns.md - All three patterns now use `.Verify()`
- guides/async-patterns.md - Replaced `WasCalled` with `tracking.Verify()`
- guides/generic-methods.md - Updated to use `Verify(Times.Exactly(N))`
- guides/user-methods.md - Updated to use `.Verify()` methods
- reference/interceptor-api.md - Added Verification Methods tables
- migration/from-moq.md - Updated Quick Reference table, Step 5 comparison

## Acceptance Criteria

- [x] All samples use Verify()/Verifiable() instead of CallCount = 1
- [x] CallCount only used when specifically demonstrating call counting
- [x] All three patterns covered (Stand-Alone, Inline Interface, Inline Class)
- [x] Samples compile and tests pass (131 tests passed)
- [ ] MarkdownSnippets sync properly (needs manual run)

## Related

- Plan: [docs-verify-verifiable.md](../plans/docs-verify-verifiable.md)
- Design: [verifiable-api-design.md](../plans/verifiable-api-design.md)
- Implementation: [verification-api-enhancement.md](../plans/completed/verification-api-enhancement.md)
