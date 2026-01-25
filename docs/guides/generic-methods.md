[Home](../../README.md) / [Guides](../guides/) / Generic Methods

# Working with Generic Methods

Generic methods present unique challenges when stubbing. Unlike non-generic methods where you configure a single behavior, generic methods can be called with different type arguments—each potentially requiring different configuration and verification.

KnockOff solves this with the `.Of<T>()` accessor pattern, giving you type-specific control while maintaining aggregate tracking across all type arguments.

**Critical concept**: Use `.Of<T>()` to access type-specific configuration and verification for generic methods. Base properties like `CalledTypeArguments` track calls across all type arguments.

**OnCall and verification**: The `OnCall` method configures the callback for a specific type argument and returns an `IMethodTracking` object for verification. Use `tracking.Verify(Times)` to verify call counts. Each type argument has independent OnCall configuration—configuring `.Of<User>().OnCall(...)` does not affect `.Of<Order>().OnCall(...)`.

---

## The Challenge

Consider a generic repository method:

```csharp
public interface IRepository
{
    T GetById<T>(int id) where T : class;
}
```

In tests, you might call `GetById<User>(1)` and `GetById<Order>(2)`. These are the same method but with different type arguments. You need to:
- Configure different return values for each type
- Verify calls per type (how many times was `GetById<User>` called?)
- Track aggregate calls (how many times was `GetById` called with any type?)

---

## Type-Specific Configuration

Use `.Of<T>()` to access the type-specific interceptor, then call `OnCall` to configure behavior for that type argument.

### OnCall Signature and Return Value

The `OnCall` method accepts a callback matching the method signature and returns `IMethodTracking` for verification:
- **Callback parameters**: Match the original method parameters
- **Callback return type**: Matches the method's return type with the specific type argument substituted
- **Return value**: `IMethodTracking` object providing `.Verify(Times)` (see [Verification Guide](verification.md))

**Key point**: `OnCall` is type-specific—each type argument needs its own configuration. The returned `IMethodTracking` object is used to verify calls for that specific type argument.

<!-- snippet: generic-configure-single -->
```cs
var stub = new RepositoryStub();

// Configure behavior for User type
stub.GetById.Of<User>().OnCall((id) =>
    new User { Id = id, Name = "Test User" });

IRepository repository = stub;
var user = repository.GetById<User>(42);

Assert.NotNull(user);
Assert.Equal(42, user.Id);
Assert.Equal("Test User", user.Name);
```
<!-- endSnippet -->

You can configure multiple types independently. Each `OnCall` is specific to its type argument:

<!-- snippet: generic-configure-multiple -->
```cs
var stub = new RepositoryStub();

// Configure different behavior for each type
stub.GetById.Of<User>().OnCall((id) =>
    new User { Id = id, Name = "User" });

stub.GetById.Of<Order>().OnCall((id) =>
    new Order { Id = id, Amount = 99.99m });

IRepository repository = stub;

var user = repository.GetById<User>(1);
var order = repository.GetById<Order>(2);

Assert.Equal("User", user?.Name);
Assert.Equal(99.99m, order?.Amount);
```
<!-- endSnippet -->

---

## Type-Specific Verification

After execution, verify calls per type using the same `.Of<T>()` accessor. The `OnCall` method returns an `IMethodTracking` object that provides verification capabilities.

<!-- snippet: generic-verify-typed -->
```cs
var stub = new RepositoryStub();

var tracking = stub.GetById.Of<User>().OnCall((id) => new User { Id = id });

IRepository repository = stub;

repository.GetById<User>(1);
repository.GetById<User>(2);

// Verify calls for specific type using Times
tracking.Verify(Times.Exactly(2));
Assert.Equal(2, stub.GetById.Of<User>().LastCallArg);
```
<!-- endSnippet -->

You can verify calls for multiple types independently:

<!-- snippet: generic-verify-aggregate -->
```cs
var stub = new RepositoryStub();

var userTracking = stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
var orderTracking = stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });

IRepository repository = stub;

repository.GetById<User>(1);
repository.GetById<User>(2);
repository.GetById<Order>(3);

// Verify each type was called using tracking
userTracking.Verify(Times.Exactly(2));
orderTracking.Verify(Times.Once);
```
<!-- endSnippet -->

---

## Multiple Type Parameters

For methods with multiple type parameters, use `.Of<T1, T2, ...>()`:

<!-- snippet: generic-multi-param -->
```cs
var stub = new ConverterStub();

// Configure for string -> int conversion
stub.Convert.Of<string, int>().OnCall((source) =>
    int.Parse(source));

// Configure for int -> string conversion
stub.Convert.Of<int, string>().OnCall((source) =>
    source.ToString());

IConverter converter = stub;

var intResult = converter.Convert<string, int>("42");
var strResult = converter.Convert<int, string>(100);

Assert.Equal(42, intResult);
Assert.Equal("100", strResult);
```
<!-- endSnippet -->

---

## Inspecting Called Type Arguments

Use `CalledTypeArguments` to see which type combinations were actually invoked:

<!-- snippet: generic-called-types -->
```cs
var stub = new RepositoryStub();

stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });

IRepository repository = stub;

repository.GetById<User>(1);
repository.GetById<Order>(2);

// CalledTypeArguments contains all types used
var types = stub.GetById.CalledTypeArguments;
Assert.Equal(2, types.Count);
Assert.Contains(typeof(User), types);
Assert.Contains(typeof(Order), types);
```
<!-- endSnippet -->

This is particularly useful when verifying that specific types were or were not used during test execution.

---

## Resetting State

Reset type-specific state using `.Of<T>().Reset()`:

<!-- snippet: generic-reset-typed -->
```cs
var stub = new RepositoryStub();

stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });

IRepository repository = stub;

repository.GetById<User>(1);
repository.GetById<Order>(2);

// Reset only User-specific state
stub.GetById.Of<User>().Reset();

stub.GetById.Of<User>().Verify(Times.Never);
stub.GetById.Of<Order>().Verify(Times.Once);
```
<!-- endSnippet -->

To reset all type arguments at once, call `Reset()` on the base interceptor:

<!-- snippet: generic-reset-all -->
```cs
var stub = new RepositoryStub();

stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });

IRepository repository = stub;

repository.GetById<User>(1);
repository.GetById<Order>(2);

// Reset all type-specific state
stub.GetById.Reset();

// Verify no calls after reset using Times.Never
stub.GetById.Of<User>().Verify(Times.Never);
stub.GetById.Of<Order>().Verify(Times.Never);
Assert.Empty(stub.GetById.CalledTypeArguments);
```
<!-- endSnippet -->

---

## Complete Example

Here's a full test demonstrating generic method stubbing for a serializer/deserializer:

<!-- snippet: generic-complete-example -->
```cs
var stub = new SerializerStub();

// Configure Serialize for different types
var serializeUserTracking = stub.Serialize.Of<User>().OnCall((obj) =>
    $"{{\"Id\":{obj.Id},\"Name\":\"{obj.Name}\"}}");

var serializeOrderTracking = stub.Serialize.Of<Order>().OnCall((obj) =>
    $"{{\"Id\":{obj.Id},\"Amount\":{obj.Amount}}}");

// Configure Deserialize
var deserializeUserTracking = stub.Deserialize.Of<User>().OnCall((data) =>
    new User { Id = 1, Name = "Deserialized User" });

var deserializeOrderTracking = stub.Deserialize.Of<Order>().OnCall((data) =>
    new Order { Id = 2, Amount = 50.00m });

ISerializer serializer = stub;

// Execute serialization
var userJson = serializer.Serialize(new User { Id = 1, Name = "Alice" });
var orderJson = serializer.Serialize(new Order { Id = 2, Amount = 99.99m });

// Execute deserialization
var user = serializer.Deserialize<User>(userJson);
var order = serializer.Deserialize<Order>(orderJson);

// Verify per-type calls with Times
serializeUserTracking.Verify(Times.Once);
serializeOrderTracking.Verify(Times.Once);
deserializeUserTracking.Verify(Times.Once);
deserializeOrderTracking.Verify(Times.Once);

// Verify called type arguments
Assert.Contains(typeof(User), stub.Serialize.CalledTypeArguments);
Assert.Contains(typeof(Order), stub.Serialize.CalledTypeArguments);
```
<!-- endSnippet -->

---

## Key Takeaways

- **`.Of<T>()`** provides type-specific access to the interceptor for a specific type argument
- **`OnCall`** configures the callback for that type—parameters match the method signature, return type matches with type arguments substituted
- **`OnCall` returns `IMethodTracking`** enabling verification via `.Verify(Times)`
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
