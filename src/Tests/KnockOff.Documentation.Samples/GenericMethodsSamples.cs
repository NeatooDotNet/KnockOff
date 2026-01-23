using KnockOff;

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
        stub.GetById.Of<User>().OnCall((id) =>
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
        stub.GetById.Of<User>().OnCall((id) =>
            new User { Id = id, Name = "User" });

        stub.GetById.Of<Order>().OnCall((id) =>
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
    public void VerifyTypedCalls_WithTimesConstraint()
    {
        var stub = new RepositoryStub();

        var tracking = stub.GetById.Of<User>().OnCall((id) => new User { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<User>(2);

        // Verify calls for specific type using Times
        tracking.Verify(Times.Exactly(2));
        Assert.Equal(2, stub.GetById.Of<User>().LastCallArg);
    }
    #endregion
}

public class VerifyAggregateCallsTests
{
    #region generic-verify-aggregate
    [Fact]
    public void VerifyAggregateCalls_VerifyPerType()
    {
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

        stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
        stub.GetById.Of<Order>().OnCall((id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<Order>(2);

        // Reset only User-specific state
        stub.GetById.Of<User>().Reset();

        stub.GetById.Of<User>().Verify(Times.Never);
        stub.GetById.Of<Order>().Verify(Times.Once);
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
    }
    #endregion
}
