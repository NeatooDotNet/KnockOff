# Rocks Edge Case Integration

**Goal:** Port edge case tests from `Rocks.Analysis.IntegrationTests` into a new `KnockOff.Analysis.Tests` project. Each file gets triaged, rewritten for KnockOff's 9 patterns, and bugs get fixed.

**Source:** `Rocks/src/Rocks.Analysis.IntegrationTests/` (63 files, ~425 tests)

## Workflow Per File

1. Triage — confirm applicability rating
2. Map — which KnockOff patterns (1-9) and member types (Methods/Properties/Indexers/Events) apply
3. Rewrite — translate Rocks API to KnockOff API for each applicable pattern
4. Fix — fix any compiler errors or behavioral bugs exposed
5. Check off

---

## Group 1: Interface Tests (11 files, 126 tests)

All High applicability. Primary patterns: 1, 2, 5, 8.

| Status | File | Tests | Members | Priority | Notes |
|--------|------|-------|---------|----------|-------|
| [ ] | InterfaceMethodReturnTests.cs | 17 | Methods | Critical | Return values, callbacks, call counts |
| [ ] | InterfaceMethodVoidTests.cs | 11 | Methods | Critical | Void execution, callbacks, verification |
| [ ] | InterfacePropertyTests.cs | 13 | Properties, Events | Critical | Get/Set/Init variants, event raising |
| [ ] | InterfaceIndexerTests.cs | 32 | Indexers, Events | Critical | Multi-param indexers, all accessor variants |
| [ ] | InterfaceGenericMethodTests.cs | 16 | Methods | Critical | Type params, method generics, nullable |
| [ ] | InterfaceGenericPropertyTests.cs | 10 | Properties | Critical | Generic properties, init variants |
| [ ] | InterfaceGenericEventsTests.cs | 1 | Events | Important | Generic event raising (sparse) |
| [ ] | InterfaceGenericIndexerTests.cs | 16 | Indexers | Critical | Generic indexers, multi-overloads |
| [ ] | InterfaceMethodReturnWithEventsTests.cs | 4 | Methods, Events | Important | Method + event composition |
| [ ] | InterfaceMethodVoidWithEventsTests.cs | 4 | Methods, Events | Important | Void method + event composition |
| [ ] | InterfaceStaticVirtualTests.cs | 2 | Methods, Properties | Low | Static virtuals excluded (C# 11) |

## Group 2: Class Tests (11 files, ~132 tests)

All High applicability. Primary patterns: 3, 4, 6, 9.

| Status | File | Tests | Members | Priority | Notes |
|--------|------|-------|---------|----------|-------|
| [ ] | ClassMethodReturnTests.cs | 18 | Methods | Critical | Return values, callbacks, call counts |
| [ ] | ClassMethodVoidTests.cs | 15 | Methods | Critical | Void execution, callbacks, verification |
| [ ] | ClassPropertyTests.cs | 13 | Properties, Events | Critical | Get/Set/Init, event raising |
| [ ] | ClassIndexerTests.cs | 32 | Indexers, Events | Critical | Multi-param, all accessor variants |
| [ ] | ClassGenericMethodTests.cs | 19 | Methods | Critical | Type params, method generics |
| [ ] | ClassGenericPropertyTests.cs | 10 | Properties | Critical | Generic properties, init |
| [ ] | ClassGenericEventsTests.cs | 1 | Events | Important | Generic event raising (sparse) |
| [ ] | ClassGenericIndexerTests.cs | 15 | Indexers | Critical | Generic indexers, multi-overloads |
| [ ] | ClassMethodReturnWithEventsTests.cs | 5 | Methods, Events | Important | Method + event composition |
| [ ] | ClassMethodVoidWithEventsTests.cs | 4 | Methods, Events | Important | Void method + event composition |
| [ ] | ClassConstructorTests.cs | 6 | Constructors | Important | ref/out/params, required members |

## Group 3: Abstract Class Tests (11 files, 116 tests)

All High applicability. Primary patterns: 3, 4, 6, 9.

| Status | File | Tests | Members | Priority | Notes |
|--------|------|-------|---------|----------|-------|
| [ ] | AbstractClassMethodReturnTests.cs | 15 | Methods | Critical | Return values, callbacks, call counts |
| [ ] | AbstractClassMethodVoidTests.cs | 12 | Methods | Critical | Void execution, callbacks |
| [ ] | AbstractClassPropertyTests.cs | 11 | Properties, Events | Critical | Get/Set/Init, event raising |
| [ ] | AbstractClassIndexerTests.cs | 26 | Indexers | Critical | Multi-param, all accessor variants |
| [ ] | AbstractClassGenericMethodTests.cs | 16 | Methods | Critical | Type params, method generics |
| [ ] | AbstractClassGenericPropertyTests.cs | 10 | Properties | Critical | Generic properties, init |
| [ ] | AbstractClassGenericEventsTests.cs | 2 | Events | Important | Generic event raising |
| [ ] | AbstractClassGenericIndexerTests.cs | 16 | Indexers | Critical | Generic indexers |
| [ ] | AbstractClassMethodReturnWithEventsTests.cs | 5 | Methods, Events | Important | Method + event composition |
| [ ] | AbstractClassMethodVoidWithEventsTests.cs | 5 | Methods, Events | Important | Void method + event composition |
| [ ] | AbstractClassConstructorTests.cs | 4 | Constructors | Important | Public/protected constructors |

## Group 4: Feature-Specific Tests (20 files, ~100 tests)

Mixed applicability. These test specific C# language features across interface/class boundaries.

| Status | File | Tests | Applicability | Members | Priority | Notes |
|--------|------|-------|---------------|---------|----------|-------|
| [ ] | GenericTests.cs | 5 | High | Methods | Critical | Generic overloads, type constraints |
| [ ] | ConstraintTests.cs | 1 | High | Methods | Critical | where T : class (sparse, expand) |
| [ ] | ExplicitInterfaceImplementationTests.cs | 9 | High | Methods, Props, Indexers | Critical | Duplicate member names across interfaces |
| [ ] | EventTests.cs | 1 | High | Events | Critical | Event definition and raising |
| [ ] | OptionalArgumentsTests.cs | 6 | High | Methods, Indexers | Critical | Default values, nullable context |
| [ ] | ParamsTests.cs | 7 | High | Methods, Indexers | Critical | params array, ReadOnlySpan params |
| [ ] | AsynchronousTests.cs | 4 | High | Methods | Critical | Task, ValueTask, IAsyncEnumerable |
| [ ] | RefStructTests.cs | 10 | High | Methods, Properties | Critical | Span<T>, ReadOnlySpan<T>, scoped |
| [ ] | RecordTests.cs | 2 | High | Methods | Important | Record types as stub sources |
| [ ] | RequiredInitPropertyTests.cs | 7 | High | Properties | Important | required + init (C# 11) |
| [ ] | VirtualsWithImplementationsTests.cs | 6 | High | Methods, Props, Indexers | Important | Default interface members, virtual base calls |
| [ ] | OpenGenericsTests.cs | 4 | High | Methods | Important | Open generic interfaces |
| [ ] | AllowNullTests.cs | 4 | Medium | Properties | Nice-to-have | [AllowNull] attribute |
| [ ] | AttributeTests.cs | 4 | Medium | Properties, Methods | Nice-to-have | [MemberNotNullWhen], [Conditional], etc. |
| [ ] | VisibilityTests.cs | 4 | Medium | Methods | Nice-to-have | Internal types as parameters |
| [ ] | NonPublicMemberTests.cs | 1 | Medium | Methods | Nice-to-have | Protected abstract methods |
| [ ] | HttpMessageHandlerTests.cs | 1 | Medium | Methods | Nice-to-have | Real-world abstract class |
| [ ] | DoesNotReturnTests.cs | 8 | Low | Methods | Defer | [DoesNotReturn] attribute |
| [ ] | PointerTests.cs | 13 | Low | Methods | Defer | Unsafe code, function pointers |
| [ ] | PartialTests.cs | 4 | Skip | N/A | Skip | Rocks-specific [RockPartial] |

## Group 5: Rocks-Specific / Infrastructure (10 files, 51 tests)

Mixed applicability. Underlying edge cases may still be valuable.

| Status | File | Tests | Applicability | Members | Priority | Notes |
|--------|------|-------|---------------|---------|----------|-------|
| [ ] | MethodMemberTests.cs | 23 | High | Methods | Critical | ref/out/in params, ref returns, 20+ params |
| [ ] | ExpectationExceptionTests.cs | 7 | High | Methods, Props, Indexers | Important | Verification system edge cases |
| [ ] | VerificationTests.cs | 6 | High | Methods, Properties | Important | Callback exceptions, verification errors |
| [ ] | ArgTests.cs | 8 | Medium | Methods, Indexers | Nice-to-have | Argument matching concepts |
| [ ] | MultipleRockCallsTests.cs | 2 | Medium | Methods | Nice-to-have | Multiple stubs of same type |
| [ ] | RockContextTests.cs | 4 | Medium | Methods | Nice-to-have | Verification lifecycle |
| [ ] | ShimTests.cs | 1 | Medium | Methods | Nice-to-have | Interface inheritance |
| [ ] | MockDefinitions.cs | 0 | Low | N/A | Skip | Assembly attribute manifest |
| [ ] | Shared.cs | 0 | Skip | N/A | Skip | NUnit config only |
| [ ] | AnalyzerTests.cs | 0 | Skip | N/A | Skip | Rocks analyzer infrastructure |

---

## Priority Summary

| Priority | File Count | Test Count | Description |
|----------|-----------|------------|-------------|
| Critical | ~30 | ~280 | Core stub behavior across all member types |
| Important | ~13 | ~75 | Generics, events, constructors, verification |
| Nice-to-have | ~10 | ~30 | Attributes, visibility, argument matching |
| Defer | 2 | 21 | Pointers, [DoesNotReturn] |
| Skip | 5 | 4 | Rocks-specific, infrastructure |

## Rocks API to KnockOff Translation Reference

| Rocks Concept | KnockOff Equivalent |
|----------------|---------------------|
| `RockContext` + `Create<T>()` | `new StubType()` |
| `Make<T>()` (no expectations) | `new StubType()` with no setup |
| `.Setups.Method()` | `stub.Method.Return(value)` / `stub.Method.Call(callback)` |
| `.Setups.Prop.Gets()` | `stub.Prop.Get(value)` |
| `.Setups.Prop.Sets(value)` | `stub.Prop.Set(callback)` |
| `.Setups[key].Gets()` | `stub.Indexer.Get(callback)` |
| `.ReturnValue(val)` | `.Return(val)` |
| `.Callback(fn)` | `.Call(fn)` |
| `.ExpectedCallCount(n)` | `stub.Method.Verify(n)` |
| `.Verify` | `stub.Method.Verify()` |
| `ExpectationException` | Strict mode behavior |
| `VerificationException` | `stub.Method.Verify()` failure |
| `.Instance()` | `stub` (patterns 1,2,5,7,8) or `stub.Object` (patterns 3,4,6,9) |
| `Arg.Any<T>()` | No equivalent yet |
| `.RaiseMyEvent()` | No equivalent yet |
