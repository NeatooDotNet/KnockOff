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
/// - skill:SKILL:property-value-pattern (SkillSamplesTests.cs)
/// - skill:SKILL:customization-callbacks-property (SkillSamplesTests.cs)
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

#region skill-SKILL-duality-pattern
// Pattern 1: User method (compile-time default)
[KnockOff]
public partial class SkServiceKnockOff : ISkService
{
    protected int GetValue(int id) => id * 2;  // Default for all tests
}
#endregion

// ============================================================================
// Quick Start
// ============================================================================

#region skill-SKILL-quick-start-interface
public interface ISkDataService
{
    string Name { get; set; }
    string? GetDescription(int id);
    int GetCount();
}
#endregion

#region skill-SKILL-quick-start-stub
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

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
#region skill-SKILL-interface-access
[KnockOff]
public partial class SkUserServiceKnockOff : ISkUserService { }

[KnockOff]
public partial class SkPropertyStoreKnockOff : ISkPropertyStore { }

[KnockOff]
public partial class SkEventSourceKnockOff : ISkEventSource { }

// Access patterns with flat API (v11.x):
// userKnockOff.GetUser             // Method handler
// storeKnockOff.StringIndexer      // Indexer handler
// eventKnockOff.DataReceivedInterceptor // Event handler
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

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
#region skill-SKILL-multiple-interfaces
[KnockOff]
public partial class SkRepositoryKnockOff : ISkRepository { }

[KnockOff]
public partial class SkUnitOfWorkKnockOff : ISkUnitOfWork { }

// Access patterns with flat API (v11.x):
// repoKnockOff.Save.WasCalled
// uowKnockOff.Commit.WasCalled
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

#region skill-SKILL-oncall-patterns
[KnockOff]
public partial class SkOnCallKnockOff : ISkOnCallService { }
#endregion

// ============================================================================
// Out/Ref Parameter Callbacks
// ============================================================================

public interface ISkParser
{
    bool TryParse(string input, out int result);
}

#region skill-SKILL-oncall-out-ref
[KnockOff]
public partial class SkParserKnockOff : ISkParser { }
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

#region skill-SKILL-smart-defaults
[KnockOff]
public partial class SkSmartDefaultKnockOff : ISkSmartDefaultService { }
#endregion

// ============================================================================
// Customization - User Methods
// ============================================================================

public interface ISkRepoService
{
    SkUser? GetById(int id);
    Task<SkUser?> GetByIdAsync(int id);
}

#region skill-SKILL-customization-user-method
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

#region skill-SKILL-customization-callbacks-method
[KnockOff]
public partial class SkCallbackMethodKnockOff : ISkCallbackService { }
#endregion

// skill:SKILL:property-value-pattern sourced from SkillSamplesTests.cs
// skill:SKILL:customization-callbacks-property sourced from SkillSamplesTests.cs

#region skill-SKILL-customization-callbacks-indexer
[KnockOff]
public partial class SkCallbackIndexerKnockOff : ISkCallbackPropertyStore { }
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
// Callback -> overrides: knockOff.Calculate2.OnCall = (ko, x) => x * 100;
// Reset -> back to user method: knockOff.Calculate2.Reset();

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

#region skill-SKILL-verification-call-tracking
[KnockOff]
public partial class SkVerificationKnockOff : ISkVerificationService { }
#endregion

// skill:SKILL:verification-property-tracking sourced from SkillSamplesTests.cs

#region skill-SKILL-verification-indexer-tracking
[KnockOff]
public partial class SkVerificationIndexerKnockOff : ISkVerificationPropertyStore { }
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

#region skill-SKILL-backing-properties
[KnockOff]
public partial class SkBackingServiceKnockOff : ISkBackingService { }
#endregion

#region skill-SKILL-backing-indexers
[KnockOff]
public partial class SkBackingPropertyStoreKnockOff : ISkBackingPropertyStore { }
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
#region skill-SKILL-pattern-conditional
[KnockOff]
public partial class SkPatternServiceKnockOff : ISkPatternService { }
#endregion

// skill:SKILL:pattern-exceptions sourced from SkillSamplesTests.cs
// skill:SKILL:pattern-sequential sourced from SkillSamplesTests.cs

// Async Methods
public interface ISkAsyncPatternRepository
{
    Task<SkUser?> GetUserAsync(int id);
    Task<int> SaveAsync(object entity);
}

#region skill-SKILL-pattern-async
[KnockOff]
public partial class SkAsyncPatternRepositoryKnockOff : ISkAsyncPatternRepository { }
#endregion

// Events
public interface ISkEventPatternSource
{
    event EventHandler<string> DataReceived;
    event Action<int> ProgressChanged;
}

#region skill-SKILL-pattern-events
[KnockOff]
public partial class SkEventPatternSourceKnockOff : ISkEventPatternSource { }
#endregion

// Generics
public interface ISkGenericSerializer
{
    T Deserialize<T>(string json);
    TOut Convert<TIn, TOut>(TIn input);
}

#region skill-SKILL-pattern-generics
[KnockOff]
public partial class SkGenericSerializerKnockOff : ISkGenericSerializer { }
#endregion

// Overloads
public interface ISkOverloadedService
{
    void Process(string data);
    void Process(string data, int priority);
    void Process(string data, int priority, bool async);
}

#region skill-SKILL-pattern-overloads
[KnockOff]
public partial class SkOverloadedServiceKnockOff : ISkOverloadedService { }
#endregion

// Nested Classes
#region skill-SKILL-pattern-nested
public partial class SkUserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class SkRepoNestedKnockOff : ISkRepository { }
}
#endregion

// Out Parameters
public interface ISkOutParamParser
{
    bool TryParse(string input, out int result);
    void GetData(out string name, out int count);
}

#region skill-SKILL-pattern-out-params
[KnockOff]
public partial class SkOutParamParserKnockOff : ISkOutParamParser { }
#endregion

// Ref Parameters
public interface ISkRefProcessor
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
}

#region skill-SKILL-pattern-ref-params
[KnockOff]
public partial class SkRefProcessorKnockOff : ISkRefProcessor { }
#endregion

// ============================================================================
// Quick Start - Test Usage
// ============================================================================

// quick-start-usage will be sourced from SkillSamplesTests.cs

// ============================================================================
// Inline Interface Stubs
// ============================================================================

public interface ISkInlineUserService
{
    SkUser? GetUser(int id);
}

public interface ISkInlineLogger
{
    void Log(string message);
}

#region skill-SKILL-inline-stub-pattern
[KnockOff<ISkInlineUserService>]
[KnockOff<ISkInlineLogger>]
public partial class SkInlineUserServiceTests
{
    // Generates: Stubs.ISkInlineUserService, Stubs.ISkInlineLogger
}
#endregion

// ============================================================================
// Partial Properties (C# 13+)
// ============================================================================

#region skill-SKILL-partial-properties
[KnockOff<ISkInlineUserService>]
public partial class SkPartialPropertyTests
{
    public partial Stubs.ISkInlineUserService UserStub { get; }  // Auto-instantiated
}
#endregion

// ============================================================================
// Delegate Stubs
// ============================================================================

public delegate bool SkIsUniqueRule(string value);
public delegate SkUser SkUserFactory(int id);

#region skill-SKILL-delegate-stubs
[KnockOff<SkIsUniqueRule>]
[KnockOff<SkUserFactory>]
public partial class SkValidationTests
{
    // Generates: Stubs.SkIsUniqueRule, Stubs.SkUserFactory
}
#endregion

// ============================================================================
// Class Stubs
// ============================================================================

#region skill-SKILL-class-stubs-class
public class SkEmailService
{
    public virtual void Send(string to, string subject, string body)
        => Console.WriteLine($"Sending to {to}");

    public virtual string ServerName { get; set; } = "default";
}
#endregion

#region skill-SKILL-class-stubs
[KnockOff<SkEmailService>]
public partial class SkEmailServiceTests
{
    // Generates: Stubs.SkEmailService
}
#endregion

// ============================================================================
// Class Constructor Parameters
// ============================================================================

public class SkRepository
{
    public string ConnectionString { get; }
    public SkRepository(string connectionString) => ConnectionString = connectionString;
    public virtual void Save(object entity) { }
}

#region skill-SKILL-class-constructor
[KnockOff<SkRepository>]
public partial class SkConstructorTests
{
    // Generates: Stubs.SkRepository
}
#endregion

// ============================================================================
// Abstract Classes
// ============================================================================

public abstract class SkBaseRepository
{
    public abstract string? ConnectionString { get; }
    public abstract void Save(object entity);
}

#region skill-SKILL-abstract-classes
[KnockOff<SkBaseRepository>]
public partial class SkAbstractTests
{
    // Generates: Stubs.SkBaseRepository
}
#endregion

// ============================================================================
// Non-Virtual Members
// ============================================================================

public class SkNonVirtualService
{
    public string NonVirtualProperty { get; set; } = "Original";
    public void NonVirtualMethod() => Console.WriteLine("Base called");
    public virtual string VirtualProperty { get; set; } = "Virtual";
}

#region skill-SKILL-non-virtual-members
[KnockOff<SkNonVirtualService>]
public partial class SkNonVirtualTests
{
    // Generates: Stubs.SkNonVirtualService
}
#endregion

// ============================================================================
// Stub Minimalism
// ============================================================================

public interface ISkMinimalService
{
    SkUser? GetUser(int id);
    int GetCount();
    List<SkUser> GetUsers();
}

#region skill-SKILL-stub-minimalism
// GOOD - minimal stub, most methods just work with smart defaults
[KnockOff]
public partial class SkMinimalServiceKnockOff : ISkMinimalService
{
    // Only define methods needing custom behavior
    protected SkUser? GetUser(int id) => new SkUser { Id = id };
    // GetCount returns 0, GetUsers() returns new List<SkUser>(), etc.
}
#endregion

// skill:SKILL:interceptor-reset - sourced from SkillSamplesTests.cs

// ============================================================================
// Overload - Single Method (No Suffix)
// ============================================================================

public interface ISkSingleMethodService
{
    void SendEmail(string to, string subject);
}

#region skill-SKILL-overload-no-suffix
[KnockOff]
public partial class SkSingleMethodServiceKnockOff : ISkSingleMethodService { }
#endregion

// ============================================================================
// Interface vs Class Stub Access
// ============================================================================

// Types for demonstrating access patterns
public interface ISkAccessDemoService
{
    string GetData();
}

public class SkAccessDemoEmailService
{
    public virtual void Send(string to, string body) { }
}

[KnockOff]
public partial class SkAccessDemoServiceKnockOff : ISkAccessDemoService { }

[KnockOff<SkAccessDemoEmailService>]
public partial class SkAccessDemoTests { }

// Usage examples demonstrating access patterns - sourced from SkillSamplesTests.cs
