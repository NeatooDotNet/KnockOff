using KnockOff;
using KnockOff.Documentation.Samples; // For User type

namespace KnockOff.Documentation.Samples.SkillContent;

// =============================================================================
// Types for SKILL.md Content Samples
// =============================================================================

public interface ICalc
{
    int Add(int a, int b);
    int GetNext();
}

public interface IEventService
{
    event EventHandler? Started;
    event EventHandler<DataEventArgs>? DataReceived;
}

public class DataEventArgs(string data) : EventArgs
{
    public string Data { get; } = data;
}

public interface ISvc
{
    string GetValue(string key);
    User? GetUser(int id);
    Task<User?> GetUserAsync(int id);
    Task SaveAsync(User user);
    void Save(User user);
    void Process(int id);
    void Log(string message);
    void Update(int id, string name);
}

public interface IPropSvc
{
    string Name { get; set; }
    DateTime Timestamp { get; }
    int Counter { get; }
}

public interface IDict
{
    string? this[string key] { get; set; }
}

public interface IGenericRepo
{
    T? GetById<T>(int id) where T : class;
}

public class ServiceBase
{
    public virtual void Initialize() { }
}

public delegate int SkillArithmeticOp(int a, int b);

public interface IUserRepo
{
    User? GetById(int id);
}

public interface IUserService
{
    int Count { get; }
    string Name { get; set; }
}

public interface ISourceRepo
{
    User? GetById(int id);
    void Save(User user);
}

// =============================================================================
// Stubs
// =============================================================================

[KnockOff]
public partial class CalcStub : ICalc { }

[KnockOff]
public partial class EventServiceStub : IEventService { }

[KnockOff]
public partial class SvcStub : ISvc { }

[KnockOff]
public partial class PropSvcStub : IPropSvc { }

[KnockOff]
public partial class DictStub : IDict { }

[KnockOff]
public partial class GenericRepoStub : IGenericRepo { }

[KnockOff]
public partial class SkSourceDelegationStub : ISourceRepo { }

// Class stub
[KnockOff<ServiceBase>]
public partial class ClassStubHost { }

// Generic interface for closed generic example
public interface IRepository<T> where T : class
{
    T? GetById(int id);
}

[KnockOff<IRepository<User>>]
public partial class ClosedGenericHost { }

// Delegate stub
[KnockOff<SkillArithmeticOp>]
public partial class DelegateHost { }

// Strict mode stub
[KnockOff(Strict = true)]
public partial class StrictStub : ISvc { }

// User method stubs
#region skill-user-method-define
[KnockOff]
public partial class SkUserMethodRepoStub : IUserRepo { }

public partial class SkUserMethodRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

// User property stubs
#region skill-user-property-define
[KnockOff]
public partial class SkUserPropServiceStub : IUserService { }

public partial class SkUserPropServiceStub
{
    private int _count;

    // Override virtual property with underscore suffix - compiler enforces signature!
    protected override int Count_ => _count;

    public void SetCount(int value) => _count = value;
}
#endregion

// Source delegation
public class RealSourceRepo : ISourceRepo
{
    public User? GetById(int id) => new User { Id = id, Name = "Real" };
    public void Save(User user) { }
}

// =============================================================================
// Gotcha #1: Sequences REPEAT Last Value
// =============================================================================

public class SequenceGotchaTests
{
    [Fact]
    public void SequenceExhaustion()
    {
        var stub = new CalcStub();
        ICalc calc = stub;

        #region skill-gotcha-sequence-exhaustion
        stub.Add.Return(1, 999);
        calc.Add(0, 0); // Returns 1
        calc.Add(0, 0); // Returns 999
        calc.Add(0, 0); // Returns 999 (repeats last value!)

        // Use ThenDefault() to return default(T) instead of repeating
        stub.Add.Return(1, 999).ThenDefault();
        calc.Add(0, 0); // Returns 1
        calc.Add(0, 0); // Returns 999
        calc.Add(0, 0); // Returns 0 (default - ThenDefault() terminates with default)
        #endregion
    }
}

// =============================================================================
// Gotcha #2: Events Use Raise() Method
// =============================================================================

public class EventGotchaTests
{
    [Fact]
    public void EventRaiseMethod()
    {
        var stub = new EventServiceStub();
        IEventService svc = stub;
        svc.Started += (s, e) => { };

        #region skill-gotcha-event-raise
        // Events use .Raise() method:
        stub.Started.Raise(stub, EventArgs.Empty);
        #endregion
    }

    [Fact]
    public void EventInterceptorNaming()
    {
        var stub = new EventServiceStub();

        #region skill-gotcha-event-naming
        // Event interceptors use the event name directly:
        stub.Started.VerifyAdd(Called.Never);
        stub.DataReceived.VerifyAdd(Called.Never);
        #endregion
    }
}

// =============================================================================
// Gotcha #4: Class Stubs Use .Object Property
// =============================================================================

public partial class ClassStubHost
{
    [Fact]
    public void ClassStubObject()
    {
        #region skill-gotcha-class-object
        // WRONG: ServiceBase service = stub;
        // RIGHT:
        var stub = new Stubs.ServiceBase();
        ServiceBase service = stub.Object;
        service.Initialize();
        #endregion
    }
}

// =============================================================================
// Gotcha #5: Closed Generic Stubs Use Simple Names
// =============================================================================

public partial class ClosedGenericHost
{
    [Fact]
    public void ClosedGenericNames()
    {
        #region skill-gotcha-closed-generic
        // For [KnockOff<IRepository<User>>]:
        var stub = new Stubs.IRepository();  // NOT Stubs.IRepository<User>
        #endregion

        stub.GetById.Return(new User { Id = 1 });
    }
}

// =============================================================================
// Gotcha #6: Called.Between() Does NOT Exist
// =============================================================================

public class TimesGotchaTests
{
    [Fact]
    public void TimesBetweenNotExist()
    {
        var stub = new SvcStub();
        stub.Save.Call((u) => { });
        ISvc svc = stub;
        svc.Save(new User { Id = 1 });

        #region skill-gotcha-times-between
        // WRONG: Called.Between(1, 5)
        // RIGHT: Use separate constraints
        stub.Save.Verify(Called.AtLeast(1));
        stub.Save.Verify(Called.AtMost(5));
        #endregion
    }
}

// =============================================================================
// Gotcha #7: Return(value) vs Return(callback) Mutual Exclusivity
// =============================================================================

public class ReturnOverloadsTests
{
    [Fact]
    public void ReturnOverloads_LastOneWins()
    {
        var stub = new SvcStub();

        #region skill-gotcha-returns-vs-oncall
        stub.GetValue.Return("fixed");           // Sets constant value
        stub.GetValue.Return((id) => $"val-{id}"); // REPLACES constant, now dynamic
        #endregion

        ISvc svc = stub;
        Assert.Equal("val-key1", svc.GetValue("key1"));
    }
}

// =============================================================================
// Gotcha #8: Set Does NOT Auto-Update Getter
// =============================================================================

public class OnSetGotchaTests
{
    [Fact]
    public void OnSetNoAutoUpdate()
    {
        var stub = new PropSvcStub();
        IPropSvc service = stub;

        #region skill-gotcha-onset-no-auto-update
        stub.Name.Set((v) => { /* tracks value */ });
        service.Name = "test";
        // Getter still returns default! Set doesn't update Get
        // To link them: stub.Name.Set((v) => stub.Name.Get(v));
        #endregion
    }
}

// =============================================================================
// Method Configuration
// =============================================================================

public class MethodConfigTests
{
    [Fact]
    public void MethodReturns()
    {
        var stub = new SvcStub();

        #region skill-method-returns
        stub.GetUser.Return(new User { Id = 1, Name = "Alice" });
        #endregion

        ISvc svc = stub;
        Assert.Equal("Alice", svc.GetUser(1)!.Name);
    }

    [Fact]
    public void MethodReturn()
    {
        var stub = new SvcStub();

        #region skill-method-oncall
        // With arguments
        stub.GetUser.Return((id) => new User { Id = id, Name = $"User{id}" });

        // Void methods
        stub.Save.Call((user) => { /* side effects */ });

        // Async methods - auto-wrapped, no Task.FromResult needed
        stub.GetUserAsync.Return((id) => new User { Id = id });  // Returns Task<User>
        stub.SaveAsync.Call((user) => { });  // Returns Task.CompletedTask
        #endregion

        ISvc svc = stub;
        Assert.Equal("User42", svc.GetUser(42)!.Name);
    }

    [Fact]
    public void MethodSequences()
    {
        var stub = new CalcStub();
        ICalc svc = stub;

        #region skill-method-sequences
        // Concise value sequences (preferred)
        stub.GetNext.Return(1, 2, 3);
        // After third call, repeats 3 (NSubstitute-like behavior)

        // Mix callbacks with value sequences
        stub.Add.Return((a, b) => a + b).ThenReturn(100, 200);
        // First: computed, then 100, 200, 200...

        // Use ThenDefault() to return default(T) instead of repeating:
        stub.GetNext.Return(1, 2).ThenDefault();
        #endregion

        Assert.Equal(1, svc.GetNext());
        Assert.Equal(2, svc.GetNext());
        Assert.Equal(0, svc.GetNext()); // default after ThenDefault
    }

    [Fact]
    public void MethodWhen()
    {
        var stub = new SvcStub();
        var adminUser = new User { Id = 42, Name = "Admin" };
        var regularUser = new User { Id = 1, Name = "Regular" };
        var premiumUser = new User { Id = 101, Name = "Premium" };
        ISvc svc = stub;

        #region skill-method-when
        // Value matching
        stub.GetUser.When(42).Return(adminUser);
        stub.GetUser.When(1).Return(regularUser);

        // Predicate matching
        stub.GetUser.When(id => id < 0).Return(null);

        // Chaining
        stub.GetUser
            .When(42).Return(adminUser)
            .ThenWhen(id => id > 100).Return(premiumUser)
            .ThenWhen(id => id > 0).Return(regularUser);

        // Void methods use Call instead of Return
        stub.Log.When("error").Call((msg) => { /* handle */ });
        #endregion

        Assert.Equal("Admin", svc.GetUser(42)!.Name);
    }
}

// =============================================================================
// Property Configuration
// =============================================================================

public class PropertyConfigTests
{
    [Fact]
    public void PropertyConfig()
    {
        var stub = new PropSvcStub();
        var capturedValues = new List<string>();

        #region skill-property-config
        // Static value
        stub.Name.Get("TestName");

        // Dynamic callback
        stub.Timestamp.Get(() => DateTime.UtcNow);

        // Setter interception
        stub.Name.Set((value) => capturedValues.Add(value));

        // Sequences
        stub.Counter.Get(() => 1).ThenGet(() => 2).ThenGet(() => 3);
        #endregion

        IPropSvc svc = stub;
        Assert.Equal("TestName", svc.Name);
        svc.Name = "updated";
        Assert.Single(capturedValues);
    }
}

// =============================================================================
// Indexer Configuration
// =============================================================================

public class IndexerConfigTests
{
    [Fact]
    public void IndexerConfig()
    {
        var stub = new DictStub();

        #region skill-indexer-config
        // Use Backing dictionary for simple cases
        stub.Indexer.Backing["key1"] = "value1";
        stub.Indexer.Backing["key2"] = "value2";

        // Or use callbacks
        stub.Indexer.Get((key) => $"computed-{key}");
        stub.Indexer.Set((key, value) => { /* handle */ });

        // Note: Get/Set override Backing - they don't work together
        #endregion

        IDict dict = stub;
        Assert.Equal("computed-key1", dict["key1"]); // Get overrides Backing
    }
}

// =============================================================================
// Event Configuration
// =============================================================================

public class EventConfigTests
{
    [Fact]
    public void EventConfig()
    {
        var stub = new EventServiceStub();
        IEventService svc = stub;
        svc.DataReceived += (sender, args) => { };

        #region skill-event-config
        // Events use Raise() method
        stub.DataReceived.Raise(stub, new DataEventArgs("test-data"));

        // Verify subscriptions
        stub.DataReceived.VerifyAdd(Called.Once);
        stub.DataReceived.VerifyRemove(Called.Never);
        #endregion
    }
}

// =============================================================================
// Generic Methods
// =============================================================================

public class GenericMethodTests
{
    [Fact]
    public void GenericMethods()
    {
        var stub = new GenericRepoStub();
        IGenericRepo repo = stub;

        #region skill-generic-methods
        // Use .Of<T>() for type-specific configuration
        stub.GetById.Of<User>().Return((id) => new User { Id = id });
        stub.GetById.Of<Product>().Return((id) => new Product { Id = id });

        // Verify by type
        stub.GetById.Of<User>().Verify(Called.Never);
        stub.GetById.Of<Product>().Verify(Called.Never);
        #endregion

        repo.GetById<User>(1);
        repo.GetById<User>(2);
        repo.GetById<Product>(1);
        stub.GetById.Of<User>().Verify(Called.Exactly(2));
        stub.GetById.Of<Product>().Verify(Called.Once);
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// =============================================================================
// Delegate Configuration
// =============================================================================

public partial class DelegateHost
{
    [Fact]
    public void DelegateConfig()
    {
        #region skill-delegate-config
        var stub = new Stubs.SkillArithmeticOp();

        // Returns (value or callback)
        stub.Interceptor.Return(42);
        stub.Interceptor.Return((a, b) => a + b);

        // Sequences
        stub.Interceptor.Return(10, 20, 30);

        // When chains
        stub.Interceptor.When(1, 2).Return(100)
            .ThenWhen(3, 4).Return(200);

        // Async auto-wrapping (for delegates returning Task<T>)
        // stub.Interceptor.Return(42);              // auto-wraps in Task.FromResult
        // stub.Interceptor.Return((int x) => x * 2); // simplified, auto-wrapped

        // Verification (fresh stub for clean tracking)
        var verifyStub = new Stubs.SkillArithmeticOp();
        verifyStub.Interceptor.Return((a, b) => a + b);
        SkillArithmeticOp op = verifyStub;
        op(1, 2);
        verifyStub.Interceptor.Verify(Called.Once);
        Assert.Equal((1, 2), verifyStub.Interceptor.LastArgs);

        // Strict mode
        stub.Strict = true;

        // Implicit conversion to delegate type
        SkillArithmeticOp opRef = stub;
        #endregion
    }
}

// =============================================================================
// Verification
// =============================================================================

public class VerificationTests
{
    [Fact]
    public void IndividualVerification()
    {
        var stub = new SvcStub();
        ISvc svc = stub;

        #region skill-verify-individual
        var tracking = stub.Save.Call((user) => { });
        // ... exercise stub ...
        #endregion

        svc.Save(new User { Id = 1 });
        svc.Save(new User { Id = 2 });
        tracking.Verify(Called.Exactly(2));
    }

    [Fact]
    public void BatchVerification()
    {
        var stub = new SvcStub();
        ISvc svc = stub;

        #region skill-verify-batch
        stub.GetUser.Return((id) => new User { Id = id }).Verifiable();
        stub.Save.Call((u) => { }).Verifiable(Called.Once);
        // ... exercise stub ...
        #endregion

        svc.GetUser(1);
        svc.Save(new User { Id = 1 });
        stub.Verify();  // Checks all Verifiable() members
    }
}

// =============================================================================
// Argument Capture
// =============================================================================

public class ArgumentCaptureTests
{
    [Fact]
    public void ArgCapture()
    {
        var stub = new SvcStub();
        ISvc service = stub;

        #region skill-arg-capture
        // Single parameter - LastArg
        var getTracking = stub.GetUser.Return((id) => new User { Id = id });
        service.GetUser(42);
        Assert.Equal(42, getTracking.LastArg);

        // Multiple parameters - LastArgs tuple
        var updateTracking = stub.Update.Call((id, name) => { });
        service.Update(1, "Alice");
        var (id, name) = updateTracking.LastArgs;
        #endregion

        Assert.Equal(1, id);
        Assert.Equal("Alice", name);
    }
}

// =============================================================================
// Strict Mode
// =============================================================================

public class StrictModeConfigTests
{
    [Fact]
    public void StrictModeConfig()
    {
        #region skill-strict-mode
        // Per-stub
        // [KnockOff(Strict = true)]
        // public partial class StrictStub : IService { }

        // Or at runtime
        var stub = new SvcStub();
        stub.Strict();

        // Assembly-wide default
        // [assembly: KnockOffStrict]
        #endregion

        ISvc svc = stub;
        Assert.Throws<StubException>(() => svc.GetUser(1));
    }
}

// =============================================================================
// User Methods
// =============================================================================

public class UserMethodTests
{
    [Fact]
    public void UserMethodReturnOverride()
    {
        #region skill-user-method-oncall
        var stub = new SkUserMethodRepoStub();
        IUserRepo repo = stub;

        // Without Returns: user method is called
        var user1 = repo.GetById(1);  // Returns User { Id = 1, Name = "Default" }

        // With Returns: callback supersedes user method (clean interceptor name)
        stub.GetById.Return(id => new User { Id = id, Name = "Override" });
        var user2 = repo.GetById(2);  // Returns User { Id = 2, Name = "Override" }
        #endregion

        Assert.Equal("Default", user1!.Name);
        Assert.Equal("Override", user2!.Name);
    }

    [Fact]
    public void UserMethodReturns()
    {
        var stub = new SkUserMethodRepoStub();

        #region skill-user-method-returns
        stub.GetById.Return(new User { Id = 99, Name = "Fixed" });
        #endregion

        IUserRepo repo = stub;
        Assert.Equal("Fixed", repo.GetById(1)!.Name);
    }

    [Fact]
    public void UserMethodReturnsCombined()
    {
        var stub = new SvcStub();

        #region methods-user-returns-constant-combined
        stub.GetUser.Return(new User { Id = 99, Name = "Fixed" });
        stub.GetUserAsync.Return(new User { Id = 1 });  // Auto-wrapped in Task.FromResult
        #endregion

        ISvc svc = stub;
        Assert.Equal("Fixed", svc.GetUser(99)!.Name);
    }

    [Fact]
    public async Task UserMethodAsyncReturns()
    {
        var stub = new SvcStub();

        #region skill-user-method-async-returns
        // Returns auto-wraps in Task.FromResult
        stub.GetUserAsync.Return(new User { Id = 1 });
        #endregion

        ISvc svc = stub;
        var user = await svc.GetUserAsync(1);
        Assert.Equal(1, user!.Id);
    }

    [Fact]
    public void UserMethodTracking()
    {
        var stub = new SkUserMethodRepoStub();
        IUserRepo repo = stub;

        #region skill-user-method-tracking
        stub.GetById.Return(id => new User { Id = id });
        repo.GetById(42);

        stub.GetById.Verify(Called.Once);
        Assert.Equal(42, stub.GetById.LastArg);
        #endregion
    }

    [Fact]
    public void UserMethodReset()
    {
        var stub = new SkUserMethodRepoStub();
        IUserRepo repo = stub;

        #region skill-user-method-reset
        stub.GetById.Return(id => new User { Id = id });
        repo.GetById(1);
        stub.GetById.Verify(Called.Once);

        stub.GetById.Reset();
        stub.GetById.Verify(Called.Never);  // Tracking cleared

        repo.GetById(2);  // Still uses Returns callback
        #endregion

        Assert.Equal(2, stub.GetById.LastArg);
    }
}

// =============================================================================
// User Properties
// =============================================================================

public class UserPropertyTests
{
    [Fact]
    public void UserPropertyOnGetOverride()
    {
        #region skill-user-property-onget
        var stub = new SkUserPropServiceStub();
        stub.SetCount(42);
        IUserService service = stub;

        // Without Get: user property is called
        var count1 = service.Count;  // Returns 42 (from Count_ override)

        // With Get: Get supersedes user property (clean interceptor name)
        stub.Count.Get(999);
        var count2 = service.Count;  // Returns 999 (Get wins)
        #endregion

        Assert.Equal(42, count1);
        Assert.Equal(999, count2);
    }

    [Fact]
    public void UserPropertyTracking()
    {
        var stub = new SkUserPropServiceStub();
        stub.SetCount(42);
        IUserService service = stub;

        #region skill-user-property-tracking
        _ = service.Count;
        _ = service.Count;

        stub.Count.VerifyGet(Called.Exactly(2));
        #endregion
    }

    [Fact]
    public void UserPropertyReset()
    {
        var stub = new SkUserPropServiceStub();
        IUserService service = stub;

        #region skill-user-property-reset
        stub.Count.Get(100);
        _ = service.Count;
        stub.Count.VerifyGet(Called.Once);

        stub.Count.Reset();
        stub.Count.VerifyGet(Called.Never);  // Tracking cleared

        _ = service.Count;  // Still uses Get (returns 100)
        #endregion

        Assert.Equal(100, service.Count);
    }
}

// =============================================================================
// Source Delegation
// =============================================================================

public class SourceDelegationTests
{
    [Fact]
    public void SourceDelegation()
    {
        var realImplementation = new RealSourceRepo();
        var testUser = new User { Id = 99, Name = "Test" };

        #region skill-source-delegation
        var stub = new SkSourceDelegationStub();
        stub.Source(realImplementation);

        // Configured members override source
        stub.GetById.Return((id) => testUser);  // This wins over source

        // Reset clears tracking (counts, args, sequence position) and source delegation
        // but preserves callbacks (Return, Returns, Get, Set)
        // stub.GetById.Reset();
        #endregion

        ISourceRepo repo = stub;
        Assert.Equal("Test", repo.GetById(1)!.Name);
    }
}

// =============================================================================
// Common Mistakes
// =============================================================================

#region skill-mistake-partial
// WRONG: Compilation errors
// [KnockOff]
// public class FooStub : IFoo { }

// RIGHT:
[KnockOff]
public partial class SkillPartialDemoStub : ISvc { }
#endregion

public class CommonMistakeTests
{
    [Fact]
    public void WrongCallbackSignature()
    {
        var stub = new SvcStub();

        #region skill-mistake-wrong-signature
        // WRONG: Type mismatch
        // stub.Process.Return((string id) => { });  // Method takes int

        // RIGHT: Match signature exactly
        stub.Process.Call((int id) => { });
        #endregion
    }
}

public partial class ClassStubHost
{
    [Fact]
    public void ForgettingObject()
    {
        #region skill-mistake-forgetting-object
        // WRONG:
        // MyClass service = stub;  // Won't compile

        // RIGHT:
        var stub = new Stubs.ServiceBase();
        ServiceBase service = stub.Object;
        #endregion
    }
}

#region skill-mistake-func-action
// WRONG: KnockOff doesn't support generic delegates
// [KnockOff<Func<int, string>>]  // Won't work

// RIGHT: Define a named delegate
public delegate string SkillNamedOperation(int value);
[KnockOff<SkillNamedOperation>]
public partial class SkillNamedDelegateHost { }
#endregion

public class SequenceExhaustionMistakeTests
{
    [Fact]
    public void SequenceExhaustionMistake()
    {
        var stub = new CalcStub();
        ICalc svc = stub;

        #region skill-mistake-sequence-exhaustion
        // Sequences repeat last value by default (NSubstitute-like behavior)
        stub.GetNext.Return(1, 2);
        // After 2 calls, returns 2 (repeats last value)

        // Use ThenDefault() to return default(T) instead of repeating
        stub.GetNext.Return(1, 2).ThenDefault();
        // After 2 calls, returns 0 (default)

        // Use Strict mode to throw when sequence exhausted
        stub.Strict = true;
        stub.GetNext.Return(1, 2);
        // Third call throws StubException
        #endregion

        Assert.Equal(1, svc.GetNext());
        Assert.Equal(2, svc.GetNext());
        Assert.Throws<StubException>(() => svc.GetNext());
    }
}
