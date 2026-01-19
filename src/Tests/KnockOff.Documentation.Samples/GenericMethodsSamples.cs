namespace KnockOff.Documentation.Samples.GenericMethods;

// =============================================================================
// Interfaces for Generic Method Samples
// =============================================================================

public interface IRepository
{
    T? GetById<T>(int id) where T : class, new();
}

public interface IConverter
{
    TTarget Convert<TSource, TTarget>(TSource source);
}

public interface ISerializer
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string data) where T : new();
}

// =============================================================================
// Stubs for Generic Method Samples
// =============================================================================

[KnockOff]
public partial class RepositoryStub : IRepository { }

[KnockOff]
public partial class ConverterStub : IConverter { }

[KnockOff]
public partial class SerializerStub : ISerializer { }

// =============================================================================
// Type-Specific Configuration
// =============================================================================

public class ConfigureSingleTypeTests
{
    #region generic-configure-single
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
    #endregion
}

public class ConfigureMultipleTypesTests
{
    #region generic-configure-multiple
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
    #endregion
}

// =============================================================================
// Type-Specific Verification
// =============================================================================

public class VerifyTypedCallsTests
{
    #region generic-verify-typed
    [Fact]
    public void VerifyTypedCalls_CallCountPerType()
    {
        var stub = new RepositoryStub();

        stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<User>(2);

        // Verify calls for specific type
        Assert.Equal(2, stub.GetById.Of<User>().CallCount);
        Assert.Equal(2, stub.GetById.Of<User>().LastCallArg);
    }
    #endregion
}

public class VerifyAggregateCallsTests
{
    #region generic-verify-aggregate
    [Fact]
    public void VerifyAggregateCalls_TotalCallCount()
    {
        var stub = new RepositoryStub();

        stub.GetById.Of<User>().OnCall((ko, id) => new User { Id = id });
        stub.GetById.Of<Order>().OnCall((ko, id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<User>(2);
        repository.GetById<Order>(3);

        // TotalCallCount aggregates across all type arguments
        Assert.Equal(3, stub.GetById.TotalCallCount);
        Assert.True(stub.GetById.WasCalled);
    }
    #endregion
}

// =============================================================================
// Multiple Type Parameters
// =============================================================================

public class MultipleTypeParametersTests
{
    #region generic-multi-param
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
    #endregion
}

// =============================================================================
// Called Type Arguments
// =============================================================================

public class CalledTypeArgumentsTests
{
    #region generic-called-types
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
    #endregion
}

// =============================================================================
// Resetting State
// =============================================================================

public class ResetTypedTests
{
    #region generic-reset-typed
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
    #endregion
}

public class ResetAllTests
{
    #region generic-reset-all
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
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteGenericExampleTests
{
    #region generic-complete-example
    [Fact]
    public void Serializer_FullGenericWorkflow()
    {
        var stub = new SerializerStub();

        // Configure Serialize for different types
        stub.Serialize.Of<User>().OnCall((ko, obj) =>
            $"{{\"Id\":{obj.Id},\"Name\":\"{obj.Name}\"}}");

        stub.Serialize.Of<Order>().OnCall((ko, obj) =>
            $"{{\"Id\":{obj.Id},\"Amount\":{obj.Amount}}}");

        // Configure Deserialize
        stub.Deserialize.Of<User>().OnCall((ko, data) =>
            new User { Id = 1, Name = "Deserialized User" });

        stub.Deserialize.Of<Order>().OnCall((ko, data) =>
            new Order { Id = 2, Amount = 50.00m });

        ISerializer serializer = stub;

        // Execute serialization
        var userJson = serializer.Serialize(new User { Id = 1, Name = "Alice" });
        var orderJson = serializer.Serialize(new Order { Id = 2, Amount = 99.99m });

        // Execute deserialization
        var user = serializer.Deserialize<User>(userJson);
        var order = serializer.Deserialize<Order>(orderJson);

        // Verify per-type calls
        Assert.Equal(1, stub.Serialize.Of<User>().CallCount);
        Assert.Equal(1, stub.Serialize.Of<Order>().CallCount);
        Assert.Equal(1, stub.Deserialize.Of<User>().CallCount);
        Assert.Equal(1, stub.Deserialize.Of<Order>().CallCount);

        // Verify aggregate totals
        Assert.Equal(2, stub.Serialize.TotalCallCount);
        Assert.Equal(2, stub.Deserialize.TotalCallCount);

        // Verify called type arguments
        Assert.Contains(typeof(User), stub.Serialize.CalledTypeArguments);
        Assert.Contains(typeof(Order), stub.Serialize.CalledTypeArguments);
    }
    #endregion
}
