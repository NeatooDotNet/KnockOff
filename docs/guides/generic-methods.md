# Working with Generic Methods

Generic methods present unique challenges when stubbing. Unlike non-generic methods where you configure a single behavior, generic methods can be called with different type arguments—each potentially requiring different configuration and verification.

KnockOff solves this with the `.Of<T>()` accessor pattern, giving you type-specific control while maintaining aggregate tracking across all type arguments.

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

## Type-Specific Configuration

Use `.Of<T>()` to configure behavior for a specific type argument.

<!-- snippet: generic-configure-single -->
```cs
[Fact]
public void ConfigureSingleType_WithOfT()
{
    var stub = new RepositoryStub();

    // Configure behavior for User type
    stub.GetById.Of<User>().OnCall((ko, id) =>
        new User { Id = id, Name = "Test User" });

    IRepository repository = stub;
    var user = repository.GetById<User>(42);

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("Test User", user.Name);
}
```
<!-- endSnippet -->

You can configure multiple types independently:

<!-- snippet: generic-configure-multiple -->
```cs
[Fact]
public void ConfigureMultipleTypes_IndependentCallbacks()
{
    var stub = new RepositoryStub();

    // Configure different behavior for each type
    stub.GetById.Of<User>().OnCall((ko, id) =>
        new User { Id = id, Name = "User" });

    stub.GetById.Of<Order>().OnCall((ko, id) =>
        new Order { Id = id, Amount = 99.99m });

    IRepository repository = stub;

    var user = repository.GetById<User>(1);
    var order = repository.GetById<Order>(2);

    Assert.Equal("User", user?.Name);
    Assert.Equal(99.99m, order?.Amount);
}
```
<!-- endSnippet -->

## Type-Specific Verification

After execution, verify calls per type using the same `.Of<T>()` accessor:

<!-- snippet: generic-verify-typed -->
```cs
[Fact]
public void VerifyTypedCalls_WithTimesConstraint()
{
    var stub = new RepositoryStub();

    var tracking = stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });

    IRepository repository = stub;

    repository.GetById<User>(1);
    repository.GetById<User>(2);

    // Verify calls for specific type using Times
    tracking.Verify(Times.Exactly(2));
    Assert.Equal(2, stub.GetById.Of<User>().LastCallArg);
}
```
<!-- endSnippet -->

For aggregate tracking across all types, use the base properties:

<!-- snippet: generic-verify-aggregate -->
```cs
[Fact]
public void VerifyAggregateCalls_VerifyPerType()
{
    var stub = new RepositoryStub();

    var userTracking = stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });
    var orderTracking = stub.GetById.Of<Order>().OnCall((ko, id) => new Order { Id = id });

    IRepository repository = stub;

    repository.GetById<User>(1);
    repository.GetById<User>(2);
    repository.GetById<Order>(3);

    // Verify each type was called using tracking
    userTracking.Verify(Times.Exactly(2));
    orderTracking.Verify(Times.Once);
}
```
<!-- endSnippet -->

## Multiple Type Parameters

For methods with multiple type parameters, use `.Of<T1, T2, ...>()`:

<!-- snippet: generic-multi-param -->
```cs
[Fact]
public void MultipleTypeParameters_OfT1T2()
{
    var stub = new ConverterStub();

    // Configure for string -> int conversion
    stub.Convert.Of<string, int>().OnCall((ko, source) =>
        int.Parse(source));

    // Configure for int -> string conversion
    stub.Convert.Of<int, string>().OnCall((ko, source) =>
        source.ToString());

    IConverter converter = stub;

    var intResult = converter.Convert<string, int>("42");
    var strResult = converter.Convert<int, string>(100);

    Assert.Equal(42, intResult);
    Assert.Equal("100", strResult);
}
```
<!-- endSnippet -->

## Inspecting Called Type Arguments

Use `CalledTypeArguments` to see which type combinations were actually invoked:

<!-- snippet: generic-called-types -->
```cs
[Fact]
public void CalledTypeArguments_TracksUsedTypes()
{
    var stub = new RepositoryStub();

    stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });
    stub.GetById.Of<Order>().OnCall((ko, id) => new Order { Id = id });

    IRepository repository = stub;

    repository.GetById<User>(1);
    repository.GetById<Order>(2);

    // CalledTypeArguments contains all types used
    var types = stub.GetById.CalledTypeArguments;
    Assert.Equal(2, types.Count);
    Assert.Contains(typeof(User), types);
    Assert.Contains(typeof(Order), types);
}
```
<!-- endSnippet -->

This is particularly useful when verifying that specific types were or were not used during test execution.

## Resetting State

Reset type-specific state using `.Of<T>().Reset()`:

<!-- snippet: generic-reset-typed -->
```cs
[Fact]
public void ResetTyped_ClearsOnlySpecificType()
{
    var stub = new RepositoryStub();

    stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });
    stub.GetById.Of<Order>().OnCall((ko, id) => new Order { Id = id });

    IRepository repository = stub;

    repository.GetById<User>(1);
    repository.GetById<Order>(2);

    // Reset only User-specific state
    stub.GetById.Of<User>().Reset();

    Assert.Equal(0, stub.GetById.Of<User>().CallCount);
    Assert.Equal(1, stub.GetById.Of<Order>().CallCount);
}
```
<!-- endSnippet -->

To reset all type arguments at once, call `Reset()` on the base interceptor:

<!-- snippet: generic-reset-all -->
```cs
[Fact]
public void ResetAll_ClearsAllTypeSpecificState()
{
    var stub = new RepositoryStub();

    stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });
    stub.GetById.Of<Order>().OnCall((ko, id) => new Order { Id = id });

    IRepository repository = stub;

    repository.GetById<User>(1);
    repository.GetById<Order>(2);

    // Reset all type-specific state
    stub.GetById.Reset();

    Assert.Equal(0, stub.GetById.TotalCallCount);
    Assert.Empty(stub.GetById.CalledTypeArguments);
}
```
<!-- endSnippet -->

## Complete Example

Here's a full test demonstrating generic method stubbing for a serializer/deserializer:

<!-- snippet: generic-complete-example -->
```cs
[Fact]
public void Serializer_FullGenericWorkflow()
{
    var stub = new SerializerStub();

    // Configure Serialize for different types
    var serializeUserTracking = stub.Serialize.Of<User>().OnCall((ko, obj) =>
        $"{{\"Id\":{obj.Id},\"Name\":\"{obj.Name}\"}}");

    var serializeOrderTracking = stub.Serialize.Of<Order>().OnCall((ko, obj) =>
        $"{{\"Id\":{obj.Id},\"Amount\":{obj.Amount}}}");

    // Configure Deserialize
    var deserializeUserTracking = stub.Deserialize.Of<User>().OnCall((ko, data) =>
        new User { Id = 1, Name = "Deserialized User" });

    var deserializeOrderTracking = stub.Deserialize.Of<Order>().OnCall((ko, data) =>
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
}
```
<!-- endSnippet -->

## Key Takeaways

- **`.Of<T>()`** provides type-specific access to `OnCall`, `CallCount`, `LastCallArg`, and `Reset()`
- **Base properties** (`TotalCallCount`, `WasCalled`, `CalledTypeArguments`) aggregate across all types
- **Multiple type parameters** use `.Of<T1, T2, ...>()` matching the method signature
- **Reset behavior** differs: `.Of<T>().Reset()` is type-specific, `.Reset()` clears everything
- **Type discovery** via `CalledTypeArguments` shows which types were actually used

Generic methods work seamlessly with all KnockOff patterns—Stand-Alone, Inline Interface, and Inline Class. The `.Of<T>()` API remains consistent regardless of how you declare your stub.
