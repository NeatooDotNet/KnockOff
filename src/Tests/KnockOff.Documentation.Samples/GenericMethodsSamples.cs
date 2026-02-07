using KnockOff;

namespace KnockOff.Documentation.Samples.GenericMethods;

// =============================================================================
// Interfaces for Generic Method Samples
// =============================================================================

#region generic-interface-definition
public interface IRepository
{
    T? GetById<T>(int id) where T : class, new();
}
#endregion

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
    [Fact]
    public void ConfigureSingleType_WithOfT()
    {
        var stub = new RepositoryStub();

        #region generic-configure-single
        // Configure behavior for User type
        stub.GetById.Of<User>().Return((id) =>
            new User { Id = id, Name = "Test User" });
        #endregion

        IRepository repository = stub;
        var user = repository.GetById<User>(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("Test User", user.Name);
    }
}

public class ConfigureMultipleTypesTests
{
    [Fact]
    public void ConfigureMultipleTypes_IndependentCallbacks()
    {
        var stub = new RepositoryStub();

        #region generic-configure-multiple
        // Configure different behavior for each type
        stub.GetById.Of<User>().Return((id) =>
            new User { Id = id, Name = "User" });

        stub.GetById.Of<Order>().Return((id) =>
            new Order { Id = id, Amount = 99.99m });
        #endregion

        IRepository repository = stub;

        var user = repository.GetById<User>(1);
        var order = repository.GetById<Order>(2);

        Assert.Equal("User", user?.Name);
        Assert.Equal(99.99m, order?.Amount);
    }
}

// =============================================================================
// Type-Specific Verification
// =============================================================================

public class VerifyTypedCallsTests
{
    [Fact]
    public void VerifyTypedCalls_WithTimesConstraint()
    {
        var stub = new RepositoryStub();

        var tracking = stub.GetById.Of<User>().Return((id) => new User { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<User>(2);

        #region generic-verify-typed
        // Verify calls for specific type using Times
        tracking.Verify(Times.Exactly(2));
        #endregion
        Assert.Equal(2, stub.GetById.Of<User>().LastArg);
    }
}

public class VerifyAggregateCallsTests
{
    [Fact]
    public void VerifyAggregateCalls_VerifyPerType()
    {
        var stub = new RepositoryStub();

        var userTracking = stub.GetById.Of<User>().Return((id) => new User { Id = id });
        var orderTracking = stub.GetById.Of<Order>().Return((id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<User>(2);
        repository.GetById<Order>(3);

        #region generic-verify-aggregate
        // Verify each type was called independently
        userTracking.Verify(Times.Exactly(2));
        orderTracking.Verify(Times.Once);
        #endregion
    }
}

// =============================================================================
// Multiple Type Parameters
// =============================================================================

public class MultipleTypeParametersTests
{
    [Fact]
    public void MultipleTypeParameters_OfT1T2()
    {
        var stub = new ConverterStub();

        #region generic-multi-param
        // Configure for string -> int conversion
        stub.Convert.Of<string, int>().Return((source) =>
            int.Parse(source));

        // Configure for int -> string conversion
        stub.Convert.Of<int, string>().Return((source) =>
            source.ToString());
        #endregion

        IConverter converter = stub;

        var intResult = converter.Convert<string, int>("42");
        var strResult = converter.Convert<int, string>(100);

        Assert.Equal(42, intResult);
        Assert.Equal("100", strResult);
    }
}

// =============================================================================
// Called Type Arguments
// =============================================================================

public class CalledTypeArgumentsTests
{
    [Fact]
    public void CalledTypeArguments_TracksUsedTypes()
    {
        var stub = new RepositoryStub();

        stub.GetById.Of<User>().Return((id) => new User { Id = id });
        stub.GetById.Of<Order>().Return((id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<Order>(2);

        #region generic-called-types
        // CalledTypeArguments contains all types used
        var types = stub.GetById.CalledTypeArguments;
        #endregion
        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(User), types);
        Assert.Contains(typeof(Order), types);
    }
}

// =============================================================================
// Resetting State
// =============================================================================

public class ResetTypedTests
{
    [Fact]
    public void ResetTyped_ClearsOnlySpecificType()
    {
        var stub = new RepositoryStub();

        stub.GetById.Of<User>().Return((id) => new User { Id = id });
        stub.GetById.Of<Order>().Return((id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<Order>(2);

        #region generic-reset-typed
        // Reset only User-specific state
        stub.GetById.Of<User>().Reset();

        stub.GetById.Of<User>().Verify(Times.Never);
        stub.GetById.Of<Order>().Verify(Times.Once);
        #endregion
    }
}

public class ResetAllTests
{
    [Fact]
    public void ResetAll_ClearsAllTypeSpecificState()
    {
        var stub = new RepositoryStub();

        stub.GetById.Of<User>().Return((id) => new User { Id = id });
        stub.GetById.Of<Order>().Return((id) => new Order { Id = id });

        IRepository repository = stub;

        repository.GetById<User>(1);
        repository.GetById<Order>(2);

        #region generic-reset-all
        // Reset all type-specific state
        stub.GetById.Reset();

        stub.GetById.Of<User>().Verify(Times.Never);
        stub.GetById.Of<Order>().Verify(Times.Never);
        #endregion
        Assert.Empty(stub.GetById.CalledTypeArguments);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteGenericExampleTests
{
    [Fact]
    public void Serializer_FullGenericWorkflow()
    {
        var stub = new SerializerStub();

        #region generic-complete-example
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
        #endregion

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
}
