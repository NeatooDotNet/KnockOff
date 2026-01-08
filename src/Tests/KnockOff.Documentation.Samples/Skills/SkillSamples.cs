/// <summary>
/// Code samples for ~/.claude/skills/knockoff/SKILL.md
///
/// Snippets in this file:
/// - skill:SKILL:duality-pattern
/// - skill:SKILL:quick-start-interface
/// - skill:SKILL:quick-start-stub
/// - skill:SKILL:quick-start-test
/// - skill:SKILL:interface-access
/// - skill:SKILL:multiple-interfaces
/// - skill:SKILL:oncall-patterns
/// - skill:SKILL:oncall-out-ref
/// - skill:SKILL:smart-defaults
/// - skill:SKILL:handler-types
/// - skill:SKILL:customization-user-method
/// - skill:SKILL:customization-callbacks-method
/// - skill:SKILL:customization-callbacks-property
/// - skill:SKILL:customization-callbacks-indexer
/// - skill:SKILL:priority-order
/// - skill:SKILL:verification-call-tracking
/// - skill:SKILL:verification-property-tracking
/// - skill:SKILL:verification-indexer-tracking
/// - skill:SKILL:backing-properties
/// - skill:SKILL:backing-indexers
/// - skill:SKILL:pattern-conditional
/// - skill:SKILL:pattern-exceptions
/// - skill:SKILL:pattern-sequential
/// - skill:SKILL:pattern-async
/// - skill:SKILL:pattern-events
/// - skill:SKILL:pattern-generics
/// - skill:SKILL:pattern-overloads
/// - skill:SKILL:pattern-nested
/// - skill:SKILL:pattern-out-params
/// - skill:SKILL:pattern-ref-params
///
/// Corresponding tests: SkillSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Skill Samples
// ============================================================================

public class SkUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SkOrder
{
    public int Id { get; set; }
}

public class SkConfig
{
    public int Timeout { get; set; }
}

// ============================================================================
// Duality Pattern
// ============================================================================

public interface ISkService
{
    int GetValue(int id);
}

#region skill:SKILL:duality-pattern
// Pattern 1: User method (compile-time default)
[KnockOff]
public partial class SkServiceKnockOff : ISkService
{
    protected int GetValue(int id) => id * 2;  // Default for all tests
}

// Pattern 2: Callback (runtime override)
// knockOff.ISkService.GetValue.OnCall = (ko, id) => id * 100;  // Override for this test
#endregion

// ============================================================================
// Quick Start
// ============================================================================

#region skill:SKILL:quick-start-interface
public interface ISkDataService
{
    string Name { get; set; }
    string? GetDescription(int id);
    int GetCount();
}
#endregion

#region skill:SKILL:quick-start-stub
[KnockOff]
public partial class SkDataServiceKnockOff : ISkDataService
{
    private readonly int _count;

    public SkDataServiceKnockOff(int count = 42) => _count = count;

    // Define behavior for non-nullable method
    protected int GetCount() => _count;

    // GetDescription not defined - returns null by default
}
#endregion

// Test usage is demonstrated in test file

// ============================================================================
// Interface Access
// ============================================================================

public interface ISkUserService
{
    SkUser GetUser(int id);
}

public interface ISkPropertyStore
{
    SkUser? this[string key] { get; set; }
}

public interface ISkEventSource
{
    event EventHandler<string> DataReceived;
}

#region skill:SKILL:interface-access
[KnockOff]
public partial class SkExampleKnockOff : ISkUserService, ISkPropertyStore, ISkEventSource { }

// Access patterns:
// knockOff.ISkUserService.GetUser       // Method handler
// knockOff.ISkPropertyStore.StringIndexer // Indexer handler
// knockOff.ISkEventSource.DataReceived  // Event handler
#endregion

// ============================================================================
// Multiple Interfaces
// ============================================================================

public interface ISkRepository
{
    void Save(object entity);
}

public interface ISkUnitOfWork
{
    void Commit();
}

#region skill:SKILL:multiple-interfaces
[KnockOff]
public partial class SkDataContextKnockOff : ISkRepository, ISkUnitOfWork { }

// Access via interface properties:
// knockOff.ISkRepository.Save.WasCalled
// knockOff.ISkUnitOfWork.Commit.WasCalled

// Use AsXxx() for explicit casting:
// ISkRepository repo = knockOff.AsSkRepository();
// ISkUnitOfWork uow = knockOff.AsSkUnitOfWork();
#endregion

// ============================================================================
// OnCall Patterns
// ============================================================================

public interface ISkOnCallService
{
    void Clear();
    SkUser GetById(int id);
    List<SkUser> Find(string name, bool active);
    void Save(object entity);
}

#region skill:SKILL:oncall-patterns
[KnockOff]
public partial class SkOnCallKnockOff : ISkOnCallService { }

// No parameters
// knockOff.ISkOnCallService.Clear.OnCall = (ko) => { };

// Single parameter
// knockOff.ISkOnCallService.GetById.OnCall = (ko, id) => new SkUser { Id = id };

// Multiple parameters - individual params, not tuples
// knockOff.ISkOnCallService.Find.OnCall = (ko, name, active) =>
//     users.Where(u => u.Name == name && u.Active == active).ToList();

// Void method
// knockOff.ISkOnCallService.Save.OnCall = (ko, entity) => { /* logic */ };
#endregion

// ============================================================================
// Out/Ref Parameter Callbacks
// ============================================================================

public interface ISkParser
{
    bool TryParse(string input, out int result);
}

#region skill:SKILL:oncall-out-ref
[KnockOff]
public partial class SkParserKnockOff : ISkParser { }

// Out/Ref parameters - use explicit delegate type:
// knockOff.ISkParser.TryParse.OnCall =
//     (ISkParser_TryParseHandler.TryParseDelegate)((ko, string input, out int result) =>
//     {
//         return int.TryParse(input, out result);
//     });
#endregion

// ============================================================================
// Smart Defaults
// ============================================================================

public interface ISkSmartDefaultService
{
    int GetCount();
    List<string> GetItems();
    IList<string> GetIList();
    string? GetOptional();
    IDisposable GetDisposable();
}

#region skill:SKILL:smart-defaults
[KnockOff]
public partial class SkSmartDefaultKnockOff : ISkSmartDefaultService { }

// var knockOff = new SkSmartDefaultKnockOff();
// ISkSmartDefaultService service = knockOff;

// No configuration needed:
// var count = service.GetCount();       // 0 (value type)
// var items = service.GetItems();       // new List<string>() (has new())
// var list = service.GetIList();        // new List<string>() (IList<T> -> List<T>)
// var optional = service.GetOptional(); // null (nullable ref)

// Only throws for types that can't be safely defaulted:
// service.GetDisposable();  // throws - can't instantiate IDisposable
#endregion

// ============================================================================
// Customization - User Methods
// ============================================================================

public interface ISkRepoService
{
    SkUser? GetById(int id);
    Task<SkUser?> GetByIdAsync(int id);
}

#region skill:SKILL:customization-user-method
[KnockOff]
public partial class SkRepoKnockOff : ISkRepoService
{
    protected SkUser? GetById(int id) => new SkUser { Id = id };
    protected Task<SkUser?> GetByIdAsync(int id) => Task.FromResult<SkUser?>(new SkUser { Id = id });
}
#endregion

// ============================================================================
// Customization - Callbacks
// ============================================================================

public interface ISkCallbackService
{
    void DoWork();
    SkUser GetById(int id);
    List<SkUser> Search(string query, int limit, int offset);
    string Name { get; set; }
    SkUser? CurrentUser { get; set; }
}

public interface ISkCallbackPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill:SKILL:customization-callbacks-method
[KnockOff]
public partial class SkCallbackMethodKnockOff : ISkCallbackService { }

// Void method
// knockOff.ISkCallbackService.DoWork.OnCall = (ko) => { /* custom logic */ };

// Return method (single param)
// knockOff.ISkCallbackService.GetById.OnCall = (ko, id) =>
//     new SkUser { Id = id, Name = "Mocked" };

// Return method (multiple params) - individual parameters
// knockOff.ISkCallbackService.Search.OnCall = (ko, query, limit, offset) =>
//     results.Skip(offset).Take(limit).ToList();
#endregion

#region skill:SKILL:customization-callbacks-property
// knockOff.ISkCallbackService.CurrentUser.OnGet = (ko) =>
//     new SkUser { Name = "TestUser" };

// knockOff.ISkCallbackService.CurrentUser.OnSet = (ko, value) =>
// {
//     capturedUser = value;
//     // Note: Value does NOT go to backing field
// };
#endregion

#region skill:SKILL:customization-callbacks-indexer
[KnockOff]
public partial class SkCallbackIndexerKnockOff : ISkCallbackPropertyStore { }

// knockOff.ISkCallbackPropertyStore.StringIndexer.OnGet = (ko, key) => key switch
// {
//     "admin" => adminConfig,
//     "guest" => guestConfig,
//     _ => null
// };

// knockOff.ISkCallbackPropertyStore.StringIndexer.OnSet = (ko, key, value) =>
// {
//     // Custom logic
//     // Note: Value does NOT go to backing dictionary
// };
#endregion

// ============================================================================
// Priority Order
// ============================================================================

public interface ISkPriorityService
{
    int Calculate(int x);
}

[KnockOff]
public partial class SkPriorityServiceKnockOff : ISkPriorityService
{
    protected int Calculate(int x) => x * 2;  // User method
}

// Priority order usage:
// var knockOff = new SkPriorityServiceKnockOff();
// ISkPriorityService service = knockOff;
// No callback -> uses user method: service.Calculate(5) returns 10
// Callback -> overrides: knockOff.ISkPriorityService.Calculate.OnCall = (ko, x) => x * 100;
// Reset -> back to user method: knockOff.ISkPriorityService.Calculate.Reset();

// ============================================================================
// Verification Patterns
// ============================================================================

public interface ISkVerificationService
{
    SkUser GetUser(int id);
    void Create(string name, int value);
    string Name { get; set; }
}

public interface ISkVerificationPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill:SKILL:verification-call-tracking
[KnockOff]
public partial class SkVerificationKnockOff : ISkVerificationService { }

// Basic
// Assert.True(knockOff.ISkVerificationService.GetUser.WasCalled);
// Assert.Equal(3, knockOff.ISkVerificationService.GetUser.CallCount);

// Arguments (single param)
// Assert.Equal(42, knockOff.ISkVerificationService.GetUser.LastCallArg);

// Arguments (multiple params - named tuple)
// var args = knockOff.ISkVerificationService.Create.LastCallArgs;
// Assert.Equal("Test", args?.name);
// Assert.Equal(100, args?.value);

// Destructuring
// if (knockOff.ISkVerificationService.Create.LastCallArgs is var (name, value))
// {
//     Assert.Equal("Test", name);
// }
#endregion

#region skill:SKILL:verification-property-tracking
// Assert.Equal(2, knockOff.ISkVerificationService.Name.GetCount);
// Assert.Equal(3, knockOff.ISkVerificationService.Name.SetCount);
// Assert.Equal("LastValue", knockOff.ISkVerificationService.Name.LastSetValue);
#endregion

#region skill:SKILL:verification-indexer-tracking
[KnockOff]
public partial class SkVerificationIndexerKnockOff : ISkVerificationPropertyStore { }

// Assert.Equal("key1", knockOff.ISkVerificationPropertyStore.StringIndexer.LastGetKey);

// var setEntry = knockOff.ISkVerificationPropertyStore.StringIndexer.LastSetEntry;
// Assert.Equal("key", setEntry?.key);
// Assert.Equal(value, setEntry?.value);
#endregion

// ============================================================================
// Backing Storage
// ============================================================================

public interface ISkBackingService
{
    string Name { get; set; }
}

public interface ISkBackingPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill:SKILL:backing-properties
[KnockOff]
public partial class SkBackingServiceKnockOff : ISkBackingService { }

// Direct access to backing field (interface-prefixed)
// knockOff.ISkBackingService_NameBacking = "Pre-populated value";

// Without OnGet, getter returns backing field
// Assert.Equal("Pre-populated value", service.Name);
#endregion

#region skill:SKILL:backing-indexers
[KnockOff]
public partial class SkBackingPropertyStoreKnockOff : ISkBackingPropertyStore { }

// Pre-populate backing dictionary (interface-prefixed)
// knockOff.ISkBackingPropertyStore_StringIndexerBacking["key1"] = value1;
// knockOff.ISkBackingPropertyStore_StringIndexerBacking["key2"] = value2;

// Without OnGet, getter checks backing dictionary
// Assert.Equal(value1, store["key1"]);
#endregion

// ============================================================================
// Common Patterns
// ============================================================================

public interface ISkPatternService
{
    SkUser? GetUser(int id);
    void Connect();
    Task<int> SaveAsync(object entity);
    int GetNext();
}

// Conditional Returns
#region skill:SKILL:pattern-conditional
[KnockOff]
public partial class SkPatternServiceKnockOff : ISkPatternService { }

// knockOff.ISkPatternService.GetUser.OnCall = (ko, id) => id switch
// {
//     1 => new SkUser { Name = "Admin" },
//     2 => new SkUser { Name = "Guest" },
//     _ => null
// };
#endregion

// Throwing Exceptions
#region skill:SKILL:pattern-exceptions
// knockOff.ISkPatternService.Connect.OnCall = (ko) =>
//     throw new TimeoutException("Connection failed");

// knockOff.ISkPatternService.SaveAsync.OnCall = (ko, entity) =>
//     Task.FromException<int>(new DbException("Save failed"));
#endregion

// Sequential Returns
#region skill:SKILL:pattern-sequential
// var results = new Queue<int>([1, 2, 3]);
// knockOff.ISkPatternService.GetNext.OnCall = (ko) => results.Dequeue();
#endregion

// Async Methods
public interface ISkAsyncPatternRepository
{
    Task<SkUser?> GetUserAsync(int id);
    Task<int> SaveAsync(object entity);
}

#region skill:SKILL:pattern-async
[KnockOff]
public partial class SkAsyncPatternRepositoryKnockOff : ISkAsyncPatternRepository { }

// knockOff.ISkAsyncPatternRepository.GetUserAsync.OnCall = (ko, id) =>
//     Task.FromResult<SkUser?>(new SkUser { Id = id });

// knockOff.ISkAsyncPatternRepository.SaveAsync.OnCall = (ko, entity) =>
//     Task.FromResult(1);
#endregion

// Events
public interface ISkEventPatternSource
{
    event EventHandler<string> DataReceived;
    event Action<int> ProgressChanged;
}

#region skill:SKILL:pattern-events
[KnockOff]
public partial class SkEventPatternSourceKnockOff : ISkEventPatternSource { }

// var knockOff = new SkEventPatternSourceKnockOff();
// ISkEventPatternSource source = knockOff;

// Subscribe tracking
// source.DataReceived += (s, e) => Console.WriteLine(e);
// Assert.Equal(1, knockOff.ISkEventPatternSource.DataReceived.SubscribeCount);
// Assert.True(knockOff.ISkEventPatternSource.DataReceived.HasSubscribers);

// Raise events from tests
// knockOff.ISkEventPatternSource.DataReceived.Raise("test data");
// Assert.True(knockOff.ISkEventPatternSource.DataReceived.WasRaised);
// Assert.Equal(1, knockOff.ISkEventPatternSource.DataReceived.RaiseCount);

// Action-style events
// knockOff.ISkEventPatternSource.ProgressChanged.Raise(75);

// Reset vs Clear
// knockOff.ISkEventPatternSource.DataReceived.Reset();  // Clears tracking, keeps handlers
// knockOff.ISkEventPatternSource.DataReceived.Clear();  // Clears tracking AND handlers
#endregion

// Generics
public interface ISkGenericSerializer
{
    T Deserialize<T>(string json);
    TOut Convert<TIn, TOut>(TIn input);
}

#region skill:SKILL:pattern-generics
[KnockOff]
public partial class SkGenericSerializerKnockOff : ISkGenericSerializer { }

// var knockOff = new SkGenericSerializerKnockOff();
// ISkGenericSerializer service = knockOff;

// Configure behavior per type argument
// knockOff.ISkGenericSerializer.Deserialize.Of<SkUser>().OnCall = (ko, json) =>
//     JsonSerializer.Deserialize<SkUser>(json)!;

// knockOff.ISkGenericSerializer.Deserialize.Of<SkOrder>().OnCall = (ko, json) =>
//     new SkOrder { Id = 123 };

// Per-type call tracking
// service.Deserialize<SkUser>("{...}");
// service.Deserialize<SkUser>("{...}");
// service.Deserialize<SkOrder>("{...}");

// Assert.Equal(2, knockOff.ISkGenericSerializer.Deserialize.Of<SkUser>().CallCount);
// Assert.Equal(1, knockOff.ISkGenericSerializer.Deserialize.Of<SkOrder>().CallCount);

// Aggregate tracking across all type arguments
// Assert.Equal(3, knockOff.ISkGenericSerializer.Deserialize.TotalCallCount);
// Assert.True(knockOff.ISkGenericSerializer.Deserialize.WasCalled);

// See which types were called
// var types = knockOff.ISkGenericSerializer.Deserialize.CalledTypeArguments;
// // [typeof(SkUser), typeof(SkOrder)]

// Multiple type parameters
// knockOff.ISkGenericSerializer.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;
#endregion

// Overloads
public interface ISkOverloadedService
{
    void Process(string data);
    void Process(string data, int priority);
    void Process(string data, int priority, bool async);
}

#region skill:SKILL:pattern-overloads
[KnockOff]
public partial class SkOverloadedServiceKnockOff : ISkOverloadedService { }

// var knockOff = new SkOverloadedServiceKnockOff();
// ISkOverloadedService service = knockOff;

// Each overload has its own handler (1-based numbering)
// knockOff.ISkOverloadedService.Process1.CallCount;  // Calls to Process(string)
// knockOff.ISkOverloadedService.Process2.CallCount;  // Calls to Process(string, int)
// knockOff.ISkOverloadedService.Process3.CallCount;  // Calls to Process(string, int, bool)

// Set callbacks for each overload
// knockOff.ISkOverloadedService.Process1.OnCall = (ko, data) => { /* 1-param */ };
// knockOff.ISkOverloadedService.Process2.OnCall = (ko, data, priority) => { /* 2-param */ };
// knockOff.ISkOverloadedService.Process3.OnCall = (ko, data, priority, async) => { /* 3-param */ };
#endregion

// Nested Classes
#region skill:SKILL:pattern-nested
public partial class SkUserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class SkRepoNestedKnockOff : ISkRepository { }

    // In test method:
    // var knockOff = new SkRepoNestedKnockOff();
    // ...
}
#endregion

// Out Parameters
public interface ISkOutParamParser
{
    bool TryParse(string input, out int result);
    void GetData(out string name, out int count);
}

#region skill:SKILL:pattern-out-params
[KnockOff]
public partial class SkOutParamParserKnockOff : ISkOutParamParser { }

// var knockOff = new SkOutParamParserKnockOff();
// ISkOutParamParser parser = knockOff;

// Callback requires explicit delegate type for out/ref
// knockOff.ISkOutParamParser.TryParse.OnCall =
//     (ISkOutParamParser_TryParseHandler.TryParseDelegate)((ko, string input, out int result) =>
//     {
//         if (int.TryParse(input, out result))
//             return true;
//         result = 0;
//         return false;
//     });

// Call the method
// var success = parser.TryParse("42", out var value);
// Assert.True(success);
// Assert.Equal(42, value);

// Tracking only includes INPUT params (not out params)
// Assert.Equal("42", knockOff.ISkOutParamParser.TryParse.LastCallArg);
// Assert.Equal(1, knockOff.ISkOutParamParser.TryParse.CallCount);
#endregion

// Ref Parameters
public interface ISkRefProcessor
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
}

#region skill:SKILL:pattern-ref-params
[KnockOff]
public partial class SkRefProcessorKnockOff : ISkRefProcessor { }

// var knockOff = new SkRefProcessorKnockOff();
// ISkRefProcessor processor = knockOff;

// Callback can modify ref params - explicit delegate type required
// knockOff.ISkRefProcessor.Increment.OnCall =
//     (ISkRefProcessor_IncrementHandler.IncrementDelegate)((ko, ref int value) =>
//     {
//         value = value * 2;  // Double it
//     });

// int x = 5;
// processor.Increment(ref x);
// Assert.Equal(10, x);  // Modified by callback

// Tracking captures INPUT value (before modification)
// Assert.Equal(5, knockOff.ISkRefProcessor.Increment.LastCallArg);
#endregion
