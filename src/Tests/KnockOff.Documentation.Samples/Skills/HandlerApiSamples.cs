/// <summary>
/// Code samples for ~/.claude/skills/knockoff/interceptor-api.md
///
/// Snippets in this file:
/// - skill:interceptor-api:method-interceptor-example
/// - skill:interceptor-api:property-interceptor-example
/// - skill:interceptor-api:indexer-interceptor-example
/// - skill:interceptor-api:event-interceptor-example
/// - skill:interceptor-api:overload-interceptor-example
/// - skill:interceptor-api:out-param-callback
/// - skill:interceptor-api:ref-param-callback
/// - skill:interceptor-api:ref-param-tracking
/// - skill:interceptor-api:async-interceptor-example
/// - skill:interceptor-api:generic-interceptor-example
/// - skill:interceptor-api:smart-defaults-example
///
/// Corresponding tests: HandlerApiSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Handler API Samples
// ============================================================================

public class HaUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class HaEntity
{
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Method Handler Example
// ============================================================================

public interface IHaService
{
    void Initialize();
    HaUser GetById(int id);
    HaEntity Create(string name, int value);
}

#region skill:interceptor-api:method-interceptor-example
[KnockOff]
public partial class HaServiceKnockOff : IHaService { }

// Void method, no params
// Assert.True(knockOff.Initialize.WasCalled);
// knockOff.Initialize.OnCall = (ko) => { };

// Return method, single param
// Assert.Equal(42, knockOff.GetById2.LastCallArg);
// knockOff.GetById2.OnCall = (ko, id) => new HaUser { Id = id };

// Multiple params
// var args = knockOff.Create.LastCallArgs;
// Assert.Equal("Test", args?.name);
// knockOff.Create.OnCall = (ko, name, value) =>
//     new HaEntity { Name = name };
#endregion

// ============================================================================
// Property Handler Example
// ============================================================================

public interface IHaPropertyService
{
    string Name { get; set; }
}

#region skill:interceptor-api:property-interceptor-example
[KnockOff]
public partial class HaPropertyServiceKnockOff : IHaPropertyService { }

// Tracking
// Assert.Equal(3, knockOff.Name.GetCount);
// Assert.Equal(2, knockOff.Name.SetCount);
// Assert.Equal("Last", knockOff.Name.LastSetValue);

// Callbacks
// knockOff.Name.OnGet = (ko) => "Always This";
// knockOff.Name.OnSet = (ko, value) => capturedValue = value;

// Backing field (direct access)
// knockOff.NameBacking = "Pre-populated";
#endregion

// ============================================================================
// Indexer Handler Example
// ============================================================================

public interface IHaPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill:interceptor-api:indexer-interceptor-example
[KnockOff]
public partial class HaPropertyStoreKnockOff : IHaPropertyStore { }

// Pre-populate backing
// knockOff.StringIndexerBacking["Key1"] = value1;
// knockOff.StringIndexerBacking["Key2"] = value2;

// Track access
// _ = store["Key1"];
// _ = store["Key2"];
// Assert.Equal(2, knockOff.StringIndexer.GetCount);
// Assert.Equal("Key2", knockOff.StringIndexer.LastGetKey);

// Dynamic getter
// knockOff.StringIndexer.OnGet = (ko, key) =>
// {
//     if (key == "special") return specialValue;
//     return ko.StringIndexerBacking.GetValueOrDefault(key);
// };

// Track setter
// store["NewKey"] = newValue;
// Assert.Equal("NewKey", knockOff.StringIndexer.LastSetEntry?.key);

// Intercept setter
// knockOff.StringIndexer.OnSet = (ko, key, value) =>
// {
//     // Custom logic
//     // Value does NOT go to backing dictionary
// };
#endregion

// ============================================================================
// Event Handler Example
// ============================================================================

public interface IHaEventSource
{
    event EventHandler<string> DataReceived;
    event EventHandler Completed;
    event Action<int> ProgressChanged;
    event Action<string, int> DataUpdated;
}

#region skill:interceptor-api:event-interceptor-example
[KnockOff]
public partial class HaEventSourceKnockOff : IHaEventSource { }

// Subscribe tracking
// source.DataReceived += handler;
// Assert.Equal(1, knockOff.DataReceived.SubscribeCount);
// Assert.True(knockOff.DataReceived.HasSubscribers);

// Raise event
// knockOff.DataReceived.Raise("test data");
// Assert.True(knockOff.DataReceived.WasRaised);
// Assert.Equal("test data", knockOff.DataReceived.LastRaiseArgs?.e);

// EventHandler (non-generic)
// knockOff.Completed.Raise(); // null sender, EventArgs.Empty

// Action with params
// knockOff.ProgressChanged.Raise(75);
// knockOff.DataUpdated.Raise("key", 42);

// All raises
// var allRaises = knockOff.DataReceived.AllRaises;
// Assert.Equal(3, allRaises.Count);

// Reset vs Clear
// knockOff.DataReceived.Reset();  // Clears tracking, keeps handlers
// knockOff.DataReceived.Clear();  // Clears tracking AND handlers
#endregion

// ============================================================================
// Overload Handler Example
// ============================================================================

public interface IHaOverloadService
{
    void Process(string data);
    void Process(string data, int priority);
    int Calculate(int value);
    int Calculate(int a, int b);
}

#region skill:interceptor-api:overload-interceptor-example
[KnockOff]
public partial class HaOverloadServiceKnockOff : IHaOverloadService { }

// var knockOff = new HaOverloadServiceKnockOff();
// IHaOverloadService service = knockOff;

// Each overload tracked separately
// service.Process("a");
// service.Process("b", 1);
// Assert.Equal(1, knockOff.Process1.CallCount);  // Process(string)
// Assert.Equal(1, knockOff.Process2.CallCount);  // Process(string, int)

// Each overload has its own callback
// knockOff.Process1.OnCall = (ko, data) => { };
// knockOff.Process2.OnCall = (ko, data, priority) => { };

// Return methods
// knockOff.Calculate1.OnCall = (ko, value) => value * 2;
// knockOff.Calculate2.OnCall = (ko, a, b) => a + b;

// Assert.Equal(10, service.Calculate(5));    // Calculate1
// Assert.Equal(8, service.Calculate(3, 5));  // Calculate2
#endregion

// ============================================================================
// Out Parameter Callback
// ============================================================================

public interface IHaParser
{
    bool TryParse(string input, out int result);
    void GetData(out string name, out int count);
}

#region skill:interceptor-api:out-param-callback
[KnockOff]
public partial class HaParserKnockOff : IHaParser { }

// Explicit delegate type required
// knockOff.TryParse.OnCall =
//     (TryParseInterceptor.TryParseDelegate)((ko, string input, out int result) =>
//     {
//         result = int.Parse(input);
//         return true;
//     });

// Void with multiple out params
// knockOff.GetData.OnCall =
//     (GetDataInterceptor.GetDataDelegate)((ko, out string name, out int count) =>
//     {
//         name = "Test";
//         count = 42;
//     });
#endregion

// ============================================================================
// Ref Parameter Callback
// ============================================================================

public interface IHaProcessor
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
}

#region skill:interceptor-api:ref-param-callback
[KnockOff]
public partial class HaProcessorKnockOff : IHaProcessor { }

// Explicit delegate type required
// knockOff.Increment.OnCall =
//     (IncrementInterceptor.IncrementDelegate)((ko, ref int value) =>
//     {
//         value = value * 2;  // Modify the ref param
//     });

// Mixed regular + ref params
// knockOff.TryUpdate.OnCall =
//     (TryUpdateInterceptor.TryUpdateDelegate)((ko, string key, ref string value) =>
//     {
//         value = value.ToUpper();
//         return true;
//     });
#endregion

#region skill:interceptor-api:ref-param-tracking
// int x = 5;
// processor.Increment(ref x);

// Assert.Equal(10, x);  // Modified
// Assert.Equal(5, knockOff.Increment.LastCallArg);  // Original input value
#endregion

// ============================================================================
// Async Handler Example
// ============================================================================

public interface IHaAsyncRepository
{
    Task<HaUser?> GetByIdAsync(int id);
    Task<int> SaveAsync(object entity);
}

#region skill:interceptor-api:async-interceptor-example
[KnockOff]
public partial class HaAsyncRepositoryKnockOff : IHaAsyncRepository { }

// knockOff.GetByIdAsync2.OnCall = (ko, id) =>
//     Task.FromResult<HaUser?>(new HaUser { Id = id });

// knockOff.SaveAsync.OnCall = (ko, entity) =>
//     Task.FromException<int>(new DbException("Failed"));
#endregion

// ============================================================================
// Generic Handler Example
// ============================================================================

public interface IHaSerializer
{
    T Deserialize<T>(string json);
    TOut Convert<TIn, TOut>(TIn input);
}

#region skill:interceptor-api:generic-interceptor-example
[KnockOff]
public partial class HaSerializerKnockOff : IHaSerializer { }

// Configure per type
// knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) =>
//     JsonSerializer.Deserialize<HaUser>(json)!;

// Per-type tracking
// Assert.Equal(2, knockOff.Deserialize.Of<HaUser>().CallCount);
// Assert.Equal("{...}", knockOff.Deserialize.Of<HaUser>().LastCallArg);

// Aggregate tracking
// Assert.Equal(5, knockOff.Deserialize.TotalCallCount);
// var types = knockOff.Deserialize.CalledTypeArguments;

// Multiple type parameters
// knockOff.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

// Reset single type vs all types
// knockOff.Deserialize.Of<HaUser>().Reset();  // Single type
// knockOff.Deserialize.Reset();              // All types
#endregion

// ============================================================================
// Smart Defaults Example
// ============================================================================

public interface IHaDefaultsService
{
    int GetCount();
    List<string> GetItems();
    IList<string> GetIList();
    string? GetOptional();
    IDisposable GetDisposable();
}

#region skill:interceptor-api:smart-defaults-example
[KnockOff]
public partial class HaDefaultsServiceKnockOff : IHaDefaultsService { }

// Examples of smart defaults
// service.GetCount();       // 0 (int)
// service.GetItems();       // new List<string>()
// service.GetIList();       // new List<string>() (from IList<string>)
// service.GetOptional();    // null (nullable ref)
// service.GetDisposable();  // throws (can't instantiate interface)

// Task<T> applies smart default to inner type
// await service.GetListAsync();  // Task.FromResult(new List<string>())
#endregion
