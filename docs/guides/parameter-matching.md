# Parameter-Specific Matching with When()

The `When()` API provides parameter-specific matching for method calls. Instead of configuring behavior for all calls to a method, you match specific argument values or patterns and configure different responses for each.

**Core benefit:** Configure different behavior based on what arguments are passed, without writing complex callback logic.

---

## The Problem: One Callback For All Arguments

When using `Return(callback)` or `Return(value)`, every call to the method uses the same callback or value:

<!-- snippet: when-problem-one-callback-all-args -->
```cs
// Without When(): callback must handle all argument combinations
stub.Calculate.Call(args =>
{
    // Complex branching logic inside callback
    if (args.a == 5 && args.b == 10)
        return 50;
    else if (args.a == 1 && args.b == 2)
        return 100;
    else if (args.a > 100)
        return 999;
    else
        return args.a + args.b;
});
```
<!-- endSnippet -->

The callback must inspect parameters and branch on logic. When() solves this by matching parameters before invoking callbacks.

---

## The Solution: Match Then Respond

`When()` separates parameter matching from response configuration:

<!-- snippet: when-solution-match-then-respond -->
```cs
// With When(): match arguments, then configure response
stub.Calculate.When(5, 10).Return(50);
```
<!-- endSnippet -->

The API reads clearly: "When called with these arguments, return this value."

---

## Basic Usage: Return Methods

### Value Matching

Match exact parameter values with `When()`. For methods with 2+ parameters, arguments are passed as a ValueTuple -- note the double parentheses `When((a, b))`. Single-parameter methods use `When(value)` directly. The tuple approach allows KnockOff to use a single pre-compiled generic interceptor type for all parameter counts, dramatically reducing generated code and build times.

<!-- snippet: when-basic-value-matching -->
```cs
// Configure different returns for different argument values
stub.Add.When(1, 2).Return(100);
stub.Add.When(3, 4).Return(200);
```
<!-- endSnippet -->

When arguments match, the configured return value is used. When they don't match, the call falls through to the next configured behavior (see Fallback Behavior section).

### Predicate Matching

Match based on conditions using a predicate:

<!-- snippet: when-basic-predicate-matching -->
```cs
// Match based on condition
stub.Add.When(args => args.a > 10).Return(999);
```
<!-- endSnippet -->

The predicate receives all method parameters and returns `true` for a match.

### Single-Parameter Methods

When() works with any parameter count:

<!-- snippet: when-single-parameter -->
```cs
// When() works with any parameter count
stub.Transform.When("hello").Return("HELLO");
```
<!-- endSnippet -->

---

## Void Methods: Call() Instead of Return()

Void methods have nothing to return, so the API differs slightly.

### Basic When() For Tracking

Use `When()` alone to track parameter-specific calls:

<!-- snippet: when-void-tracking-only -->
```cs
// When() alone tracks parameter-specific calls
var chain = stub.Process.When(1, 2);
```
<!-- endSnippet -->

The returned chain tracks how many times this specific parameter combination was called.

### When().Call() For Callbacks

Add a callback with `.Call()` if you need side effects:

<!-- snippet: when-void-with-callback -->
```cs
// Call() adds callback for side effects
stub.Process.When(1, 2).Call(args => calls.Add((args.a, args.b)));
```
<!-- endSnippet -->

`.Call()` is optional—use it only when you need to execute logic for specific parameter values.

### Predicate Matching With Void Methods

Predicates work the same as return methods:

<!-- snippet: when-void-predicate -->
```cs
// Predicate matching works the same for void methods
stub.Process.When(args => args.a > 10).Call(args => matched.Add((args.a, args.b)));
```
<!-- endSnippet -->

---

## Chaining Multiple Matchers

### ThenWhen() For Sequential Matching

Chain multiple matchers with `ThenWhen()`:

<!-- snippet: when-chaining-thenwhen -->
```cs
// Chain matchers with ThenWhen()
stub.Add
    .When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenWhen(args => args.a > 100).Return(999);
```
<!-- endSnippet -->

Matchers are consumed in order:
1. First call matching `(1, 2)` consumes first matcher, returns `100`
2. Second call matching `(3, 4)` consumes second matcher, returns `200`
3. Third call matching predicate consumes third matcher, returns `999`
4. Subsequent calls repeat the last matcher

**Key behavior:** The chain advances only when a matcher is consumed. Non-matching calls fall through without advancing.

### Multiple When() Calls

Calling `When()` multiple times adds to the same chain:

<!-- snippet: when-multiple-calls -->
```cs
// Multiple When() calls build the same chain
stub.Add.When(1, 2).Return(100);
stub.Add.When(2, 3).Return(200);
stub.Add.When(3, 4).Return(300);
```
<!-- endSnippet -->

This is equivalent to chaining with `ThenWhen()`.

---

## Terminal Matchers

### ThenCall() - Unconditional Fallback

Use `ThenCall()` to add an unconditional matcher that repeats forever:

<!-- snippet: when-thencall-terminal -->
```cs
// ThenCall() is an unconditional terminal matcher
stub.Add
    .When(1, 2).Return(100)
    .ThenCall(args => args.a + args.b);
```
<!-- endSnippet -->

`ThenCall()` always matches and repeats for all subsequent calls. It's a terminal operation—you cannot chain more matchers after it.

**When to use ThenCall():**
- Provide a fallback after matching specific cases
- Use actual computation logic after exhausting specific returns

### ThenNone() - Exhaust and Fall Through

Use `ThenNone()` to close the chain and fall through:

<!-- snippet: when-thennone-exhaust -->
```cs
// ThenNone() closes the chain and falls through
stub.Add.When(1, 2).Return(100).ThenNone();
stub.Add.Return(999);
```
<!-- endSnippet -->

After `ThenNone()` is reached, the When chain is exhausted. Calls fall through to the next configured behavior (`Return`, `Call`, or default).

**When to use ThenNone():**
- Explicitly mark when specific matching stops
- Return to default behavior after handling specific cases

---

## Fallback Behavior

When no When() matcher matches, the call falls through to other configured behavior in priority order.

### Priority Order

1. **When()** - Highest priority when matched
2. **Sequence (Return(callback).ThenReturn(callback))** - Next priority
3. **Return(value)** - Simple return value
4. **Return(callback) / Call(callback)** - General callback
5. **Default** - `default(T)` in non-strict mode, exception in strict mode

### Falling Through To Return()

<!-- snippet: when-fallback-returns -->
```cs
// When() falls through to Return() when no match
stub.Add.When(1, 2).Return(100);
stub.Add.Return(999);
```
<!-- endSnippet -->

### Falling Through To Return(callback)

<!-- snippet: when-fallback-oncall -->
```cs
// When() falls through to Return() when no match
stub.Add.When(1, 2).Return(100);
stub.Add.Call(args => args.a * args.b);
```
<!-- endSnippet -->

### No Fallback Configured

<!-- snippet: when-fallback-none-nonstrict -->
```cs
// Non-strict mode: unmatched calls return default
stub.Strict = false;
stub.Add.When(1, 2).Return(100);
```
<!-- endSnippet -->

In strict mode, unmatched calls throw:

<!-- snippet: when-fallback-none-strict -->
```cs
// Strict mode: unmatched calls throw
stub.Strict = true;
stub.Add.When(1, 2).Return(100);
```
<!-- endSnippet -->

---

## Combining With Sequences

When() has higher priority than sequences created via `Return(callback).ThenReturn(callback)`:

<!-- snippet: when-priority-over-sequence -->
```cs
// Sequence configured via Return().ThenReturn()
stub.Add.Call(_ => 1).ThenReturn(_ => 2);

// When() has higher priority
stub.Add.When(1, 2).Return(100);
```
<!-- endSnippet -->

When a When() matcher matches, the sequence is not consulted. When no When() matcher matches, the sequence is used.

---

## Verification

### Chain Completion

Call `.Verify()` on the returned chain to verify it reached a terminal state:

<!-- snippet: when-verification-complete -->
```cs
// Chain with ThenCall terminal
var chain = stub.Add
    .When(1, 2).Return(100)
    .ThenCall(_ => 999);
```
<!-- endSnippet -->

Verification passes if the chain reaches:
- A `ThenCall()` terminal
- A `ThenNone()` terminal
- The last matcher in the chain (which repeats)

### Incomplete Chain Throws

<!-- snippet: when-verification-incomplete -->
```cs
// Chain with multiple matchers
var chain = stub.Add
    .When(1, 2).Return(100)
    .ThenWhen(2, 3).Return(200);
```
<!-- endSnippet -->

### Batch Verification With Verifiable()

Mark a chain for batch verification:

<!-- snippet: when-verifiable-batch -->
```cs
// Mark chain for batch verification
stub.Add
    .When(1, 2).Return(100)
    .ThenCall(_ => 999)
    .Verifiable();
```
<!-- endSnippet -->

### Parameter-Specific Verification (Void Methods)

For void methods, verify specific parameter calls with `Called`:

<!-- snippet: when-void-verify-times -->
```cs
// Track specific parameter combination
var chain = stub.Process.When(1, 2);
```
<!-- endSnippet -->

This verifies how many times the specific parameter combination was called, independent of chain completion.

---

## Resetting

### Chain Reset

Call `Reset()` on the returned chain to restart from the beginning:

<!-- snippet: when-reset-chain -->
```cs
// Chain tracks position - Reset() restarts from beginning
var chain = stub.Add
    .When(1, 2).Return(100)
    .ThenCall(_ => 999);
```
<!-- endSnippet -->

Reset clears:
- Chain position (HEAD)
- Call counts for all matchers

### Interceptor Reset

Calling `Reset()` on the interceptor also resets the When chain:

<!-- snippet: when-reset-interceptor -->
```cs
// Interceptor Reset() also resets When chain
stub.Add
    .When(1, 2).Return(100)
    .ThenCall(_ => 999);
```
<!-- endSnippet -->

---

## Async Methods

For async methods, `Return()` automatically wraps the value with `Task.FromResult()`:

<!-- snippet: when-async-autowrap -->
```cs
// Return() auto-wraps with Task.FromResult()
stub.GetAsync.When("hello").Return("HELLO");
```
<!-- endSnippet -->

`ThenCall()` still uses the full delegate type:

<!-- snippet: when-async-thencall -->
```cs
// Return() auto-wraps, ThenCall uses full delegate
stub.GetAsync
    .When("first").Return("FIRST")
    .ThenCall(s => Task.FromResult(s.ToUpper()));
```
<!-- endSnippet -->

---

## When To Use When()

Choose `When()` over `Return(callback)` when you have these scenarios:

| Use When() | Use Return(callback) |
|------------|--------------|
| Multiple specific argument combinations with different responses | Single callback handles all arguments |
| Sequential behavior (first call does X, second call does Y) | Same behavior for every call |
| Clear separation of matching logic from response logic | Complex computation based on arguments |
| Testing retry logic, state transitions, fallback chains | Simple mocking of a dependency |

**Example: Retry Logic**

<!-- snippet: when-usecase-retry -->
```cs
// Simulate a service that fails once then succeeds
stub.FetchData
    .When("user:123").Return((string?)null)
    .ThenWhen("user:123").Return("{ \"name\": \"Alice\" }");
```
<!-- endSnippet -->

**Example: State Transitions**

For parameterless methods, use `Return(callback).ThenReturn(callback)` sequences instead of When() (When() requires parameters):

<!-- snippet: when-usecase-state-transitions -->
```cs
// For parameterless methods, use Return().ThenReturn() sequences
stub.GetStatus
    .Call(() => "Pending")
    .ThenReturn(() => "Processing")
    .ThenReturn(() => "Complete");
```
<!-- endSnippet -->

---

## Complete Example

This example demonstrates When() in a realistic test scenario:

<!-- snippet: when-complete-example -->
```cs
// Payment gateway with different responses for specific amounts
stub.ProcessPayment
    .When(100m).Return(new WhenPaymentResult { Success = true, Message = "Payment processed" })
    .ThenWhen(0m).Return(new WhenPaymentResult { Success = false, Message = "Invalid amount" })
    .ThenWhen((amt) => amt > 1000m).Return(new WhenPaymentResult { RequiresApproval = true })
    .ThenCall((amt) => new WhenPaymentResult { Success = true, Message = $"Processed ${amt}" })
    .Verifiable();
```
<!-- endSnippet -->

---

## Key Takeaways

1. **When() matches parameters first** - Response is configured only after matching
2. **Return methods use Return()** - `When(args).Return(value)`
3. **Void methods use Call()** - `When(args).Call(callback)` or just `When(args)` for tracking
4. **Chain with ThenWhen()** - Add sequential matchers
5. **Terminate with ThenCall() or ThenNone()** - ThenCall repeats, ThenNone exhausts
6. **Fallback to other config** - When() has highest priority, falls through when no match
7. **Verify completion** - `.Verify()` checks chain reached terminal state
8. **Reset restarts chain** - Both chain and interceptor Reset() restart matching

---

**Next Steps:**
- [Method Configuration Guide](methods.md) - Return, Call, and sequences
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete When() API reference

---

**UPDATED:** 2026-02-18
