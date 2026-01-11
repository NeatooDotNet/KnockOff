# Customization Patterns

KnockOff provides **two complementary patterns** for customizing stub behavior. This is a key differentiator from Moq.

## The Duality

| Pattern | When | Scope | Precedence |
|---------|------|-------|------------|
| User Methods | Compile-time | All tests | 2nd |
| Callbacks | Runtime | Per-test | 1st |

## Pattern 1: User-Defined Methods

Define protected methods in your stub class matching interface method signatures.

### Basic Example

snippet: skill-customization-patterns-user-method-basic

### Async Methods

snippet: skill-customization-patterns-user-method-async

### Rules

| Rule | Description |
|------|-------------|
| Access modifier | Must be `protected` |
| Name | Must match interface method name exactly |
| Parameters | Must match types and count |
| Return type | Must match (including Task/ValueTask) |
| Applies to | Methods only (not properties/indexers) |

### When to Use

- Same behavior needed across all tests using this stub
- Shared test fixtures
- Simple, deterministic returns
- No access to test-specific state needed

## Pattern 2: Callbacks

Set delegates on interface spy interceptors at runtime for per-test customization.

### Method Callbacks

Use `OnCall =` property assignment:

snippet: skill-customization-patterns-callback-method

### Property Callbacks

snippet: skill-customization-patterns-callback-property

### Indexer Callbacks

snippet: skill-customization-patterns-callback-indexer

### Overloaded Method Callbacks

When an interface has overloaded methods, each overload has its own interceptor with a numeric suffix (1-based):

snippet: skill-customization-patterns-callback-overloads

### Out Parameter Callbacks

Methods with `out` parameters require explicit delegate type:

snippet: skill-customization-patterns-callback-out-params

**Tracking**: Out parameters are NOT tracked (they're outputs). Only input parameters appear in `LastCallArg`/`LastCallArgs`.

### Ref Parameter Callbacks

Methods with `ref` parameters also require explicit delegate type. Ref parameters ARE tracked (input value before modification).

snippet: skill-customization-patterns-callback-ref-params

### Callback Signatures

| Member | Callback | Signature |
|--------|----------|-----------|
| Method (no params) | `OnCall =` | `Action<TKnockOff>` or `Func<TKnockOff, R>` |
| Method (with params) | `OnCall =` | `Action<TKnockOff, T1, T2, ...>` or `Func<TKnockOff, T1, T2, ..., R>` |
| Property getter | `OnGet =` | `Func<TKnockOff, T>` |
| Property setter | `OnSet =` | `Action<TKnockOff, T>` |
| Indexer getter | `OnGet =` | `Func<TKnockOff, TKey, TValue>` |
| Indexer setter | `OnSet =` | `Action<TKnockOff, TKey, TValue>` |

### When to Use

- Different behavior needed per test
- Dynamic returns based on arguments
- Side effects (capturing, logging)
- Access to Spy state (check if other methods called)
- Temporarily override user method

## Priority Order

```
+---------------------------------------------+
| 1. CALLBACK (if set)                        |
|    -> Takes precedence, stops here          |
+---------------------------------------------+
| 2. USER METHOD (if defined)                 |
|    -> Methods only, stops here              |
+---------------------------------------------+
| 3. DEFAULT                                  |
|    -> Properties: backing field             |
|    -> Methods: null (nullable) or           |
|               throw (non-nullable) or       |
|               silent (void)                 |
|    -> Indexers: backing dict -> null/throw  |
+---------------------------------------------+
```

### Priority in Action

snippet: skill-customization-patterns-priority-in-action

## Reset Behavior

`Reset()` clears:
- Call tracking (`CallCount`, `LastCallArg`/`LastCallArgs`)
- Callbacks (`OnCall`, `OnGet`, `OnSet`)

Does NOT clear:
- Backing fields for properties
- Backing dictionaries for indexers

snippet: skill-customization-patterns-reset-behavior

## Combining Both Patterns

snippet: skill-customization-patterns-combining-patterns

## Decision Guide

| Question | Recommendation |
|----------|----------------|
| Same behavior in all tests? | User method |
| Different per test? | Callback |
| Need to capture call info? | Either (tracking always available) |
| Shared across test classes? | User method |
| Complex conditional logic? | Callback |
| Just static value? | User method (simpler) |
| Need to verify side effects? | Callback |
