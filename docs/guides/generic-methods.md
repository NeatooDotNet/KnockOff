[Home](../../README.md) / [Guides](../guides/) / Generic Methods

# Working with Generic Methods

Generic methods present unique challenges when stubbing. Unlike non-generic methods where you configure a single behavior, generic methods can be called with different type arguments—each potentially requiring different configuration and verification.

KnockOff solves this with the `.Of<T>()` accessor pattern, giving you type-specific control while maintaining aggregate tracking across all type arguments.

**Critical concept**: Use `.Of<T>()` to access type-specific configuration and verification for generic methods. Base properties like `CalledTypeArguments` track calls across all type arguments.

**OnCall and verification**: The `.Of<T>().OnCall()` method configures the callback for a specific type argument and returns an `IMethodTracking` object for verification. Use `tracking.Verify(Times)` to verify call counts. Each type argument has independent configuration—configuring `.Of<User>().OnCall(...)` does not affect `.Of<Order>().OnCall(...)`. Note: Generic method typed handlers use `Return` (not `Returns`/`Execute`) — this is a separate API from the main method interceptor configuration.

---

## The Challenge

Consider a generic repository method:

<!-- snippet: generic-interface-definition -->
```cs
public interface IRepository
{
    T? GetById<T>(int id) where T : class, new();
}
```
<!-- endSnippet -->

In tests, you might call `GetById<User>(1)` and `GetById<Order>(2)`. These are the same method but with different type arguments. You need to:
- Configure different return values for each type
- Verify calls per type (how many times was `GetById<User>` called?)
- Track aggregate calls (how many times was `GetById` called with any type?)

---

## Type-Specific Configuration

Use `.Of<T>()` to access the type-specific interceptor, then call `Return` to configure behavior for that type argument.

### Return Signature and Return Value

The `Return` method accepts a callback matching the method signature and returns `IMethodTracking` for verification:
- **Callback parameters**: Match the original method parameters
- **Callback return type**: Matches the method's return type with the specific type argument substituted
- **Return value**: `IMethodTracking` object providing `.Verify(Times)` (see [Verification Guide](verification.md))

**Key point**: `Return` is type-specific—each type argument needs its own configuration. The returned `IMethodTracking` object is used to verify calls for that specific type argument.

<!-- snippet: generic-configure-single -->
```cs
// Configure behavior for User type
stub.GetById.Of<User>().Return((id) =>
    new User { Id = id, Name = "Test User" });
```
<!-- endSnippet -->

You can configure multiple types independently. Each `Return` is specific to its type argument:

<!-- snippet: generic-configure-multiple -->
```cs
// Configure different behavior for each type
stub.GetById.Of<User>().Return((id) =>
    new User { Id = id, Name = "User" });

stub.GetById.Of<Order>().Return((id) =>
    new Order { Id = id, Amount = 99.99m });
```
<!-- endSnippet -->

---

## Type-Specific Verification

After execution, verify calls per type using the same `.Of<T>()` accessor. The `Return` method returns an `IMethodTracking` object that provides verification capabilities.

<!-- snippet: generic-verify-typed -->
```cs
// Verify calls for specific type using Times
tracking.Verify(Times.Exactly(2));
```
<!-- endSnippet -->

You can verify calls for multiple types independently:

<!-- snippet: generic-verify-aggregate -->
```cs
// Verify each type was called independently
userTracking.Verify(Times.Exactly(2));
orderTracking.Verify(Times.Once);
```
<!-- endSnippet -->

---

## Multiple Type Parameters

For methods with multiple type parameters, use `.Of<T1, T2, ...>()`:

<!-- snippet: generic-multi-param -->
```cs
// Configure for string -> int conversion
stub.Convert.Of<string, int>().Return((source) =>
    int.Parse(source));

// Configure for int -> string conversion
stub.Convert.Of<int, string>().Return((source) =>
    source.ToString());
```
<!-- endSnippet -->

---

## Inspecting Called Type Arguments

Use `CalledTypeArguments` to see which type combinations were actually invoked:

<!-- snippet: generic-called-types -->
```cs
// CalledTypeArguments contains all types used
var types = stub.GetById.CalledTypeArguments;
```
<!-- endSnippet -->

This is particularly useful when verifying that specific types were or were not used during test execution.

---

## Resetting State

Reset type-specific state using `.Of<T>().Reset()`:

<!-- snippet: generic-reset-typed -->
```cs
// Reset only User-specific state
stub.GetById.Of<User>().Reset();

stub.GetById.Of<User>().Verify(Times.Never);
stub.GetById.Of<Order>().Verify(Times.Once);
```
<!-- endSnippet -->

To reset all type arguments at once, call `Reset()` on the base interceptor:

<!-- snippet: generic-reset-all -->
```cs
// Reset all type-specific state
stub.GetById.Reset();

stub.GetById.Of<User>().Verify(Times.Never);
stub.GetById.Of<Order>().Verify(Times.Never);
```
<!-- endSnippet -->

---

## Complete Example

Here's a full test demonstrating generic method stubbing for a serializer/deserializer:

<!-- snippet: generic-complete-example -->
```cs
// Configure Serialize for different types
var serializeUserTracking = stub.Serialize.Of<User>().Return((obj) =>
    $"{{\"Id\":{obj.Id},\"Name\":\"{obj.Name}\"}}");

var serializeOrderTracking = stub.Serialize.Of<Order>().Return((obj) =>
    $"{{\"Id\":{obj.Id},\"Amount\":{obj.Amount}}}");

// Configure Deserialize
var deserializeUserTracking = stub.Deserialize.Of<User>().Return((data) =>
    new User { Id = 1, Name = "Deserialized User" });

var deserializeOrderTracking = stub.Deserialize.Of<Order>().Return((data) =>
    new Order { Id = 2, Amount = 50.00m });
```
<!-- endSnippet -->

---

## Key Takeaways

- **`.Of<T>()`** provides type-specific access to the interceptor for a specific type argument
- **`Return`** configures the callback for that type—parameters match the method signature, return type matches with type arguments substituted
- **`Return` returns `IMethodTracking`** enabling verification via `.Verify(Times)`
- **Base properties** (`CalledTypeArguments`, `Reset()`) track and manage calls across all types
- **Multiple type parameters** use `.Of<T1, T2, ...>()` matching the method signature
- **Verification** uses `tracking.Verify(Times)` for type-specific call count assertions
- **Reset behavior** differs: `.Of<T>().Reset()` is type-specific, `.Reset()` clears everything
- **Type discovery** via `CalledTypeArguments` shows which types were actually used

Generic methods work seamlessly with all KnockOff patterns—Stand-Alone, Inline Interface, and Inline Class. The `.Of<T>()` API remains consistent regardless of how you declare your stub.

---

Next: [Advanced Callbacks](advanced-callbacks.md) for complex callback scenarios and state management.

---

**UPDATED:** 2026-01-25
