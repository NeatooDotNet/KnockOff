/// <summary>
/// Code samples for docs/reference/interceptor-api.md
///
/// Snippets in this file:
/// - docs:interceptor-api:method-interceptor-examples
/// - docs:interceptor-api:property-interceptor-examples
/// - docs:interceptor-api:indexer-interceptor-examples
/// - docs:interceptor-api:event-interceptor-examples
/// - docs:interceptor-api:async-method-examples
/// - docs:interceptor-api:generic-method-interceptor-examples
///
/// Corresponding tests: InterceptorApiSamplesTests.cs
/// </summary>

using System.Text.Json;

namespace KnockOff.Documentation.Samples.Reference;

// ============================================================================
// Domain Types
// ============================================================================

public class ApiUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ApiOrder
{
    public int Id { get; set; }
}

public class ApiEntity
{
    public int Id { get; set; }
}

// ============================================================================
// Method Interceptor - Interface and Stub
// ============================================================================

public interface IApiMethodService
{
    void Initialize();
    ApiUser GetById(int id);
    void Log(string level, string message);
}

[KnockOff]
public partial class ApiMethodServiceKnockOff : IApiMethodService { }

// ============================================================================
// Property Interceptor - Interface and Stub
// ============================================================================

public interface IApiPropertyService
{
    string Name { get; set; }
}

[KnockOff]
public partial class ApiPropertyServiceKnockOff : IApiPropertyService { }

// ============================================================================
// Indexer Interceptor - Interface and Stub
// ============================================================================

public interface IApiIndexerStore
{
    string? this[string key] { get; set; }
}

[KnockOff]
public partial class ApiIndexerStoreKnockOff : IApiIndexerStore { }

// ============================================================================
// Event Interceptor - Interface and Stub
// ============================================================================

public interface IApiEventSource
{
    event EventHandler<string> DataReceived;
    event EventHandler Completed;
    event Action<int> ProgressChanged;
    event Action<string, int> DataUpdated;
}

[KnockOff]
public partial class ApiEventSourceKnockOff : IApiEventSource { }

// ============================================================================
// Async Method Interceptor - Interface and Stub
// ============================================================================

public class DbException : Exception
{
    public DbException(string message) : base(message) { }
}

public interface IApiAsyncRepository
{
    Task<ApiUser?> GetByIdAsync(int id);
    Task<int> SaveAsync(ApiEntity entity);
}

[KnockOff]
public partial class ApiAsyncRepositoryKnockOff : IApiAsyncRepository { }

// ============================================================================
// Generic Method Interceptor - Interface and Stub
// ============================================================================

public interface IApiSerializer
{
    T Deserialize<T>(string json);
    TOut Convert<TIn, TOut>(TIn input);
}

[KnockOff]
public partial class ApiSerializerKnockOff : IApiSerializer { }

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating interceptor APIs.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class InterceptorApiUsageExamples
{
    public static void MethodInterceptorExamples()
    {
        var knockOff = new ApiMethodServiceKnockOff();
        IApiMethodService service = knockOff;

        // Call some methods to set up state
        service.Initialize();
        service.GetById(42);
        service.Log("error", "Failed");

        #region interceptor-api-method-interceptor-examples
        // Void method, no params
        Assert.True(knockOff.Initialize.WasCalled);
        knockOff.Initialize.OnCall = (ko) => { /* custom */ };

        // Return method, single param
        Assert.Equal(42, knockOff.GetById.LastCallArg);
        knockOff.GetById.OnCall = (ko, id) => new ApiUser { Id = id };

        // Void method, multiple params
        var args = knockOff.Log.LastCallArgs;
        Assert.Equal("error", args?.level);
        Assert.Equal("Failed", args?.message);

        knockOff.Log.OnCall = (ko, level, message) =>
        {
            Console.WriteLine($"[{level}] {message}");
        };
        #endregion
    }

    public static void PropertyInterceptorExamples()
    {
        var knockOff = new ApiPropertyServiceKnockOff();
        IApiPropertyService service = knockOff;
        var capturedValues = new List<string>();

        // Set up state
        _ = service.Name;
        _ = service.Name;
        _ = service.Name;
        service.Name = "First";
        service.Name = "LastValue";

        #region interceptor-api-property-interceptor-examples
        // Track property access
        Assert.Equal(3, knockOff.Name.GetCount);
        Assert.Equal(2, knockOff.Name.SetCount);
        Assert.Equal("LastValue", knockOff.Name.LastSetValue);

        // Override getter
        knockOff.Name.OnGet = (ko) => "Always this";

        // Override setter (capture without storing)
        knockOff.Name.OnSet = (ko, value) =>
        {
            capturedValues.Add(value);
            // Value does NOT go to backing field
        };

        // Reset
        knockOff.Name.Reset();
        Assert.Equal(0, knockOff.Name.GetCount);
        #endregion
    }

    public static void IndexerExamples()
    {
        var knockOff = new ApiIndexerStoreKnockOff();
        IApiIndexerStore store = knockOff;
        var value1 = "Value1";
        var value2 = "Value2";
        var specialValue = "Special";
        var newValue = "NewValue";

        #region interceptor-api-indexer-interceptor-examples
        // Pre-populate backing
        knockOff.Indexer.Backing["Key1"] = value1;
        knockOff.Indexer.Backing["Key2"] = value2;

        // Track access
        _ = store["Key1"];
        _ = store["Key2"];
        Assert.Equal(2, knockOff.Indexer.GetCount);
        Assert.Equal("Key2", knockOff.Indexer.LastGetKey);

        // Dynamic getter
        knockOff.Indexer.OnGet = (ko, key) =>
        {
            if (key == "special") return specialValue;
            return ko.Indexer.Backing.GetValueOrDefault(key);
        };

        // Track setter
        store["NewKey"] = newValue;
        Assert.Equal("NewKey", knockOff.Indexer.LastSetEntry?.Key);

        // Interceptor setter
        knockOff.Indexer.OnSet = (ko, key, value) =>
        {
            // Custom logic
            // Value does NOT go to backing dictionary
        };
        #endregion
    }

    public static void EventInterceptorExamples()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;
        EventHandler<string> handler = (sender, data) => { };

        #region interceptor-api-event-interceptor-examples
        // Subscribe tracking
        source.DataReceived += handler;
        Assert.Equal(1, knockOff.DataReceived.AddCount);
        Assert.True(knockOff.DataReceived.HasSubscribers);

        // Raise event (EventHandler<T> requires sender)
        knockOff.DataReceived.Raise(null, "test data");

        // EventHandler (non-generic)
        knockOff.Completed.Raise(null, EventArgs.Empty);

        // Action with params
        knockOff.ProgressChanged.Raise(75);
        knockOff.DataUpdated.Raise("key", 42);

        // Unsubscribe tracking
        source.DataReceived -= handler;
        Assert.Equal(1, knockOff.DataReceived.RemoveCount);
        Assert.False(knockOff.DataReceived.HasSubscribers);

        // Reset clears counts AND handlers
        knockOff.DataReceived.Reset();
        Assert.Equal(0, knockOff.DataReceived.AddCount);
        Assert.Equal(0, knockOff.DataReceived.RemoveCount);
        #endregion
    }

    public static void AsyncMethodExamples()
    {
        var knockOff = new ApiAsyncRepositoryKnockOff();

        #region interceptor-api-async-method-examples
        knockOff.GetByIdAsync.OnCall = (ko, id) =>
            Task.FromResult<ApiUser?>(new ApiUser { Id = id });

        knockOff.SaveAsync.OnCall = (ko, entity) =>
            Task.FromException<int>(new DbException("Failed"));
        #endregion
    }

    public static void GenericMethodInterceptorExamples()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        // Make some calls to set up state
        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) =>
            JsonSerializer.Deserialize<ApiUser>(json)!;

        service.Deserialize<ApiUser>("{\"Id\":1}");
        service.Deserialize<ApiUser>("{\"Id\":2}");
        service.Deserialize<ApiOrder>("{\"Id\":3}");
        service.Deserialize<ApiOrder>("{\"Id\":4}");
        service.Deserialize<ApiOrder>("{\"Id\":5}");

        #region interceptor-api-generic-method-interceptor-examples
        // Configure per type
        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) =>
            JsonSerializer.Deserialize<ApiUser>(json)!;

        // Per-type tracking
        Assert.Equal(2, knockOff.Deserialize.Of<ApiUser>().CallCount);
        Assert.Equal("{\"Id\":2}", knockOff.Deserialize.Of<ApiUser>().LastCallArg);

        // Aggregate tracking
        Assert.Equal(5, knockOff.Deserialize.TotalCallCount);
        Assert.True(knockOff.Deserialize.WasCalled);

        // See which types were called
        var types = knockOff.Deserialize.CalledTypeArguments;
        // [typeof(ApiUser), typeof(ApiOrder)]

        // Multiple type parameters
        knockOff.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

        // Reset single type
        knockOff.Deserialize.Of<ApiUser>().Reset();

        // Reset all types
        knockOff.Deserialize.Reset();
        #endregion
    }
}

// Minimal Assert class for compilation (tests use xUnit)
// Using 'file' modifier to avoid conflicts with xUnit.Assert in test projects
file static class Assert
{
    public static void True(bool condition) { }
    public static void False(bool condition) { }
    public static void Equal<T>(T expected, T actual) { }
}
