# Method Interceptors Reference

Method interceptors track calls, capture arguments, and configure return values for stubbed methods. Each method on the stubbed interface or class gets a corresponding interceptor property.

## Class Stub Method Behavior

Class stubs (Patterns 3, 4, 6, 9) have a different default behavior than interface stubs. For unconfigured virtual methods, the base class implementation is called automatically. This is the equivalent of Moq's `CallBase = true`, but it is the default -- no opt-in required.

| Member Kind | Unconfigured Behavior | Configured Behavior |
|-------------|----------------------|---------------------|
| **Virtual method** | Calls base class implementation | Interceptor handles it (base is NOT called) |
| **Abstract method** | Returns `default(T)` (or throws in strict mode) | Interceptor handles it |

When you configure a method with Return, Call, or When, the interceptor takes full control and the base implementation is not invoked. Only unconfigured members fall through to the base.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `Call` with an `Action`:

<!-- snippet: methods-oncall-void -->
```cs
// Return for void methods uses Action<...params>
stub.LogMessage.Call((message) => logged.Add(message));
```
<!-- endSnippet -->

### Methods with Return Values

#### Using a Callback

Configure methods that return values using `Return` with a `Func`:

<!-- snippet: methods-oncall-return -->
```cs
// Return with return value: Func<...params, TReturn>
stub.GetUserName.Return((userId) => "TestUser");
```
<!-- endSnippet -->

#### Using a Fixed Value

For simple scenarios where the return value does not depend on arguments, use the value overload:

<!-- snippet: methods-oncall-value -->
```cs
// Returns - simpler syntax when you don't need callback logic
stub.GetUserName.Return("StaticUser");
```
<!-- endSnippet -->

#### When to Use Value, Params Sequence, or Callback

<!-- snippet: methods-oncall-value-vs-callback -->
```cs
// Use VALUE when returning a fixed result:
stub.GetUserName.Return("Alice");

// Use CALLBACK when you need:
// - Dynamic values based on arguments
// - Side effects
// - Conditional logic
stub.GetUserName.Return((userId) => userId > 100 ? "Admin" : "User");

// Both return tracking objects for verification
```
<!-- endSnippet -->

| Scenario | Recommended Syntax |
|----------|-------------------|
| Fixed value (always same) | `Return(value)` |
| Constant sequence | `Return(first, second, ...)` |
| Dynamic based on args | `Return((args) => computed)` |
| Callback then constants | `Return(cb).ThenReturn(x, y, z)` |

### Methods with Multiple Parameters

Methods with multiple parameters include all parameters in the callback:

<!-- snippet: methods-oncall-multi-param -->
```cs
// All method parameters are passed to the callback in order
stub.ValidateCredentials.Return((username, password) =>
    username == "admin" && password == "secret");
```
<!-- endSnippet -->

---

## Methods with ref, out, and in Parameters

KnockOff generates **custom delegate types** for methods with `ref`, `out`, or `in` parameters. It does NOT use `Action<>` or `Func<>` (which cannot express ref kinds in C#).

Given an interface with ref-kind parameters:

<!-- snippet: refout-simple-interface -->
```cs
public interface IRefOutService
{
    void RefArgument(ref int a);
    void OutArgument(out int a);
    void InArgument(in int a);
}
```
<!-- endSnippet -->

KnockOff generates named delegates per-method (e.g., `delegate void RefArgumentDelegate(ref int a)`). These replace `Action<>` and `Func<>`, which cannot express `ref`, `out`, or `in` modifiers in C#.

### Configuring ref/out Methods

Callbacks must match the delegate signature including ref/out modifiers:

<!-- snippet: refout-configuring-callbacks -->
```cs
// out parameter -- callback must use 'out'
stub.OutArgument.Call((out int a) => { a = 42; });

// ref parameter -- callback must use 'ref'
stub.RefArgument.Call((ref int a) => { a = a + 1; });
```
<!-- endSnippet -->

### Generic Methods with ref/out Parameters

Generic methods with ref/out parameters also use custom delegates. The `Of<T>()` typed handler provides the correctly-typed delegate:

<!-- snippet: refout-generic-methods -->
```cs
// Generic methods with ref/out use custom delegates via Of<T>()
stub.OutArgumentsWithGenerics.Of<string, int>().Call((string a, out int b) => { b = 99; });
```
<!-- endSnippet -->

### Key Points

- Custom delegates are generated per-method -- never `Action<ref T>` or `Func<out T>`
- Works across all 9 patterns (standalone, inline, class, open generic)
- Both generic and non-generic methods are supported
- `in` parameters are preserved in indexer signatures and method signatures

---

## Capturing Arguments

### Single Parameter Methods

Access the last call's argument using `LastArg`:

<!-- snippet: methods-capture-single -->
```cs
// LastArg captures the most recent call's argument
int capturedId = tracking.LastArg;
```
<!-- endSnippet -->

### Multiple Parameter Methods

Access arguments using the `LastArgs` named tuple:

<!-- snippet: methods-capture-multiple -->
```cs
// LastArgs is a named tuple with all parameters
var (username, password) = tracking.LastArgs!.Value;
```
<!-- endSnippet -->

---

## Handling Overloaded Methods

When an interface has overloaded methods, KnockOff distinguishes them by the callback signature. The fully-typed lambda tells KnockOff which overload to configure:

<!-- snippet: methods-overloads -->
```cs
// Fully-typed lambda tells KnockOff which overload to configure
stub.Find.Return(() => new List<User>());
stub.Find.Return((int id) => new User { Id = id, Name = "ById" });
stub.Find.Return((string name) => new User { Id = 1, Name = name });
```
<!-- endSnippet -->

**Important:** The callback signature determines which overload is configured. Use explicit types in lambdas when parameter types are ambiguous.

---

## Resetting Interceptors

Clear tracking state while preserving configured callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
// Reset clears call count and captured arguments, but preserves callbacks
stub.ProcessData.Reset();
```
<!-- endSnippet -->

**Reset() clears:** Call count, captured arguments (LastArg/LastArgs), sequence index, source delegation, When chain position.

**Reset() preserves:** Return/Call callbacks, sequence structure, When chain structure, Verifiable marking.

---

## Quick Reference

| Task | Code |
|------|------|
| Configure void method | `stub.Method.Call((args) => { })` |
| Configure method with callback | `stub.Method.Return((args) => returnValue)` |
| Configure method with value | `stub.Method.Return(fixedValue)` |
| Configure async Task<T> (auto-wrap) | `stub.AsyncMethod.Return(value)` |
| Verify method was called | `stub.Method.Verify()` |
| Verify call count | `stub.Method.Verify(Called.Exactly(n))` |
| Get last single arg | `stub.Method.LastArg` |
| Get last multiple args | `stub.Method.LastArgs` (named tuple) |
| Reset interceptor | `stub.Method.Reset()` |

**Cross-cutting features** covered in dedicated reference files:
- Sequences: `Return(1, 2, 3)`, `ThenReturn()`, `ThenDefault()` → see **sequences.md**
- When chains: `When(args).Return(value)`, `ThenWhen()` → see **when-chains.md**
- Verification: `Verify()`, `Verifiable()`, `VerifyAll()` → see **verification.md**
- Async auto-wrapping: three-tier pattern → see **async-methods.md**
- Generic methods: `Of<T>()` pattern → see **generic-methods.md**
- Stub overrides: underscore-suffix pattern → see **stub-overrides.md**
