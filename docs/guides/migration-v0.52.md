# Migration Guide: v0.52.0 (IntelliSense API Redesign)

**Version:** 0.52.0 (Breaking Change)
**Date:** 2026-02-18

---

## Summary

v0.52.0 is a comprehensive API redesign focused on IntelliSense clarity. The pre-compiled interceptor types introduced in v0.48-v0.51 achieved significant build time reductions but made IntelliSense tooltips unintelligible -- users saw walls of generic type noise instead of parameter names. This release replaces those generic base types with fully generated interceptor classes that provide clean, typed method signatures in IntelliSense.

The API naming is also unified: `Return` is strictly for values, `Call` is for callbacks. Named tuples provide parameter names in IntelliSense for methods with 2+ parameters. Overloaded methods now use a single interceptor property instead of numbered slots.

---

## Breaking Changes

### 1. `Returns(callback)` / `Return(callback)` --> `Call(callback)`

Any callback (lambda/delegate) passed to configure method behavior now uses `Call()` instead of `Return()` or the older `Returns()`.

**Before:**
```csharp
// Old: Returns or Return with a callback
stub.GetUser.Returns((id) => new User { Id = id });
stub.GetUser.Return((id) => new User { Id = id });
stub.Add.Returns((a, b) => a + b);
```

**After:**
```csharp
// New: Call for callbacks
stub.GetUser.Call((id) => new User { Id = id });
stub.Add.Call(args => args.a + args.b);
```

### 2. `Return(value)` stays `Return(value)` -- no change

Setting a constant return value is unchanged:

```csharp
// Unchanged
stub.GetUser.Return(new User { Id = 1 });
stub.Add.Return(42);
stub.Add.Return(1, 2, 3);  // Params sequence also unchanged
```

### 3. `Returns(value)` --> `Return(value)`

If you were still using the old plural `Returns`, update to the singular `Return`:

**Before:**
```csharp
stub.Add.Returns(42);
```

**After:**
```csharp
stub.Add.Return(42);
```

### 4. `Execute(callback)` / `OnCall(callback)` --> `Call(callback)`

The older `Execute` and `OnCall` method names for void method callbacks are now `Call`:

**Before:**
```csharp
stub.Reset.Execute(() => resetCount++);
stub.Update.OnCall((user) => saved = user);
```

**After:**
```csharp
stub.Reset.Call(() => resetCount++);
stub.Update.Call((user) => saved = user);
```

### 5. `ThenReturns(value)` --> `ThenReturn(value)`

**Before:**
```csharp
stub.Add.Call(_ => 1).ThenReturns(2).ThenReturns(3);
```

**After:**
```csharp
stub.Add.Call(_ => 1).ThenReturn(2).ThenReturn(3);
```

### 6. `ThenReturns(callback)` / `ThenExecute(callback)` --> `ThenCall(callback)`

**Before:**
```csharp
stub.Add.Call(_ => 1).ThenReturns(_ => 2);
stub.Reset.Call(() => log.Add("first")).ThenExecute(() => log.Add("second"));
```

**After:**
```csharp
stub.Add.Call(_ => 1).ThenCall(_ => 2);
stub.Reset.Call(() => log.Add("first")).ThenCall(() => log.Add("second"));
```

### 7. Callback signatures for 2+ parameters now use named tuples

Methods with 2 or more parameters now receive arguments as a single named tuple parameter instead of individual parameters.

**Before:**
```csharp
// Old: individual parameters
stub.Add.Call((a, b) => a + b);
stub.Process.Call((id, name, value) => $"{id}-{name}-{value}");
```

**After:**
```csharp
// New: named tuple parameter with field access
stub.Add.Call(args => args.a + args.b);
stub.Process.Call(args => $"{args.id}-{args.name}-{args.value}");
```

The tuple field names match the original method parameter names, so IntelliSense shows meaningful names.

**0-1 parameter methods are unchanged:**
```csharp
// No parameters - unchanged
stub.Reset.Call(() => resetCount++);

// Single parameter - unchanged
stub.GetUser.Call((id) => new User { Id = id });
```

### 8. When chain predicates for 2+ parameters use named tuples

**Before:**
```csharp
stub.Add.When((a, b) => a > 0 && b > 0).Return(42);
```

**After:**
```csharp
stub.Add.When(args => args.a > 0 && args.b > 0).Return(42);
```

When with exact value matching is unchanged:
```csharp
// Still works the same
stub.Add.When(1, 2).Return(100);
stub.GetUser.When(42).Return(user);
```

### 9. Overloaded methods: single property, no more slots

Overloaded methods now use a single interceptor property. Lambda parameter types disambiguate which overload is being configured.

**Before:**
```csharp
// Old: numbered slots
stub.Process.Returns("result for string");
stub.Process2.Returns(42);  // int overload
```

**After:**
```csharp
// New: single property, disambiguated by lambda parameter types
stub.Process.Call((string input) => "result for string");
stub.Process.Call(((int id, string name) args) => 42);
```

For overloaded methods, `Return(value)` is not available at the interceptor level (it would be ambiguous). Use `Call(callback)` with explicit parameter types instead:

```csharp
// For a constant return, use Call with a constant lambda
stub.Format.Call((string input) => "constant");
stub.Format.Call(((string input, FormatOptions options) args) => "constant");
```

### 10. Tracking handles for overloaded methods

Each `Call()` on an overloaded method returns an overload-specific tracking handle:

```csharp
var tracking1 = stub.Format.Call((string input) => input);
var tracking2 = stub.Format.Call(((string input, FormatOptions options) args) => args.input);

IFormatter formatter = stub;
formatter.Format("hello");
formatter.Format("hello", new FormatOptions());

tracking1.Verify(Called.Once);   // Only counts single-param calls
tracking2.Verify(Called.Once);   // Only counts two-param calls
stub.Format.Verify(Called.Exactly(2));  // Total across all overloads
```

---

## Before/After Examples

### Basic method stubbing (non-void, single parameter)

**Before:**
```csharp
stub.GetUser.Returns((id) => new User { Id = id });
// or
stub.GetUser.Return((id) => new User { Id = id });
```

**After:**
```csharp
stub.GetUser.Call((id) => new User { Id = id });
```

### Void method callbacks

**Before:**
```csharp
stub.Reset.Execute(() => resetCount++);
stub.Update.OnCall((user) => saved = user);
```

**After:**
```csharp
stub.Reset.Call(() => resetCount++);
stub.Update.Call((user) => saved = user);
```

### 2+ parameter methods (tuple change)

**Before:**
```csharp
stub.Add.Returns((a, b) => a + b);
stub.Divide.Returns((a, b) => {
    if (b == 0) throw new DivideByZeroException();
    return a / b;
});
```

**After:**
```csharp
stub.Add.Call(args => args.a + args.b);
stub.Divide.Call(args => {
    if (args.b == 0) throw new DivideByZeroException();
    return args.a / args.b;
});
```

### Sequences

**Before:**
```csharp
stub.Add.Call(_ => 1).ThenReturns(_ => 2).ThenReturns(_ => 3);
stub.Add.Call(_ => 1).ThenReturns(2).ThenReturns(3);
stub.Reset.Execute(() => log.Add("first")).ThenExecute(() => log.Add("second"));
```

**After:**
```csharp
stub.Add.Call(_ => 1).ThenCall(_ => 2).ThenCall(_ => 3);
stub.Add.Call(_ => 1).ThenReturn(2).ThenReturn(3);
stub.Reset.Call(() => log.Add("first")).ThenCall(() => log.Add("second"));
```

Note: `Return(1, 2, 3)` params syntax is unchanged. `Return(1).ThenReturn(2).ThenReturn(3)` is also unchanged.

### When chains

**Before:**
```csharp
stub.Add.When((a, b) => a > 0).Return(42);
stub.Add.When(1, 2).Return(100);
```

**After:**
```csharp
stub.Add.When(args => args.a > 0).Return(42);
stub.Add.When(1, 2).Return(100);  // Exact match unchanged
```

### Overloaded methods

**Before:**
```csharp
// Numbered slots
stub.Format.Returns((input) => input.ToUpper());
stub.Format2.Returns((input, maxLen) => input[..maxLen]);
```

**After:**
```csharp
// Single property, lambda types disambiguate
stub.Format.Call((string input) => input.ToUpper());
stub.Format.Call(((string input, int maxLength) args) => args.input[..args.maxLength]);
```

### Tracking handles

**Before:**
```csharp
var tracking = stub.Add.Returns((a, b) => a + b);
calc.Add(1, 2);
var (a, b) = tracking.LastArgs;
```

**After:**
```csharp
var tracking = stub.Add.Call(args => args.a + args.b);
calc.Add(1, 2);
var (a, b) = tracking.LastArgs;  // Still a named tuple
```

---

## What Didn't Change

These APIs remain the same:

- **Properties:** `stub.Name.Get("value")`, `stub.Name.Set(v => ...)`, `stub.Name.VerifyGet()`, `stub.Name.VerifySet()`
- **Indexers:** `stub.Indexer["key"].Returns("value")`, `stub.Indexer.Get(key => ...)`, `stub.Indexer.Set(entry => ...)`
- **Events:** `stub.Changed.Raise(...)`, `stub.Changed.VerifyAdd()`, `stub.Changed.VerifyRemove()`, `stub.Changed.HasSubscribers`
- **Ref/out parameters:** Still use delegate fallback syntax with `ref`/`out` keywords
- **0-1 parameter methods:** Lambda signature unchanged (`() => ...` or `(id) => ...`)
- **Return(value):** Constant value configuration unchanged
- **Return(first, params rest):** Params sequence syntax unchanged
- **When(exactValues):** Exact value matching unchanged
- **Verify/Verifiable:** Verification API unchanged
- **LastArg/LastArgs:** Argument capture unchanged
- **Reset():** Reset behavior unchanged
- **Source delegation:** `stub.Source(realImpl)` unchanged
- **Strict mode:** `stub.Strict = true` unchanged

---

## New Features

### XML comments on all generated methods

Every generated interceptor method now includes XML documentation comments. IntelliSense shows the original method signature, parameter descriptions, and async wrapping behavior:

```
/// <summary>Configures callback for Add(int a, int b).</summary>
public AddInterceptor Call(Func<(int a, int b), int> callback)

/// <summary>Sets return value for GetUser(int id).</summary>
public GetUserInterceptor Return(User? value)

/// <summary>Configures callback for GetAsync(string input). Result auto-wrapped in Task.</summary>
public GetAsyncInterceptor Call(Func<string, string?> callback)
```

If the original interface/class method had XML param docs, those are migrated to the generated methods.

### Named tuples for 2+ parameter callbacks

Callbacks for methods with 2+ parameters receive a named tuple. IntelliSense shows field names matching the original method parameters:

```csharp
stub.Add.Call(args => args.a + args.b);
//                         ^       ^
//         IntelliSense shows: int a, int b
```

This replaces the previous pattern of individual parameters that lost their names in generic type noise.

### Fully generated interceptor classes

Each method gets its own generated interceptor class (e.g., `AddInterceptor`, `GetUserInterceptor`). These inherit from a non-generic `MethodInterceptorRuntime` base class, so IntelliSense tooltips show clean class names instead of `MethodInterceptor<Func<int, int, int>, (int a, int b), int>`.

### Single property for overloaded methods

Overloaded methods share a single interceptor property. C# overload resolution on the `Call()` lambda signature determines which overload is being configured. No more `stub.Method2`, `stub.Method3` slot naming.

---

## Quick Find-and-Replace

For most codebases, these replacements cover the majority of changes:

| Find | Replace | Notes |
|------|---------|-------|
| `.Returns(` (with callback) | `.Call(` | Only when passing a lambda/delegate |
| `.Return(` (with callback) | `.Call(` | Only when passing a lambda/delegate |
| `.Execute(` | `.Call(` | Void method callbacks |
| `.OnCall(` | `.Call(` | Void method callbacks |
| `.ThenReturns(` (with callback) | `.ThenCall(` | Only when passing a lambda/delegate |
| `.ThenReturns(` (with value) | `.ThenReturn(` | Only when passing a constant value |
| `.ThenExecute(` | `.ThenCall(` | Void method sequence callbacks |

For the tuple migration (2+ params), there is no simple find-and-replace. Each callback must be updated from individual parameters to tuple field access:
- `(a, b) => a + b` becomes `args => args.a + args.b`
- `(id, name, value) => ...` becomes `args => args.id + args.name + args.value`

The compiler will flag every incompatible call site, so you can migrate incrementally.
