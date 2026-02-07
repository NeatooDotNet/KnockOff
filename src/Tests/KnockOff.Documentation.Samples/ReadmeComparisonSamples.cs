using NSubstitute;
using NSubstitute.ExceptionExtensions;
using KnockOff;

namespace KnockOff.Documentation.Samples.CompareComparisons;

// =============================================================================
// Interfaces for README Comparison Samples
// These match the examples in the README side-by-side tables
// =============================================================================

public interface ICalculator
{
    int Add(int a, int b);
    Task<int> AddAsync(int a, int b);
    string Mode { get; set; }
    event EventHandler PoweringUp;
}

public interface IUserRepo
{
    User? GetUser(int id);
    Task<User?> GetUserAsync(int id);
    void Save(User user);
}

public interface ICache
{
    string this[string key] { get; set; }
}

public delegate string StringFactory(int id);

// =============================================================================
// KnockOff Stubs (prefixed with Compare to avoid naming collisions)
// =============================================================================

[KnockOff]
public partial class CompareCalculatorStub : ICalculator { }

[KnockOff]
public partial class CompareUserRepoStub : IUserRepo { }

[KnockOff]
public partial class CompareCacheStub : ICache { }

[KnockOff<StringFactory>]
public partial class CompareDelegateStubHolder { }

// =============================================================================
// README HERO EXAMPLE - "The Difference"
// =============================================================================

public class HeroExampleNSubTests
{
    [Fact]
    public void NSubstitute_ConditionalReturn()
    {
        #region readme-hero-nsub
        var repo = Substitute.For<IUserRepo>();
        repo.GetUser(Arg.Is<int>(id => id > 0)).Returns(x => new User { Id = x.Arg<int>() });
        #endregion

        // Usage
        IUserRepo repository = repo;
        var user = repository.GetUser(42);
        Assert.Equal(42, user?.Id);
    }
}

public class HeroExampleKnockOffTests
{
    [Fact]
    public void KnockOff_ConditionalReturn()
    {
        #region readme-hero-knockoff
        var stub = new CompareUserRepoStub();
        stub.GetUser.Return((id) => id > 0 ? new User { Id = id } : null);
        #endregion

        // Usage
        IUserRepo repository = stub;
        var user = repository.GetUser(42);
        Assert.Equal(42, user?.Id);
    }
}

// =============================================================================
// METHODS TABLE - Return value
// =============================================================================

public class MethodsReturnValueNSubTests
{
    [Fact]
    public void NSubstitute_ReturnValue()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Add(1, 2).Returns(3);

        Assert.Equal(3, calc.Add(1, 2));
    }
}

public class MethodsReturnValueKnockOffTests
{
    [Fact]
    public void KnockOff_ReturnValue()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return(3);

        ICalculator calc = stub;
        Assert.Equal(3, calc.Add(1, 2));
    }
}

// =============================================================================
// METHODS TABLE - Any argument
// =============================================================================

public class MethodsAnyArgNSubTests
{
    [Fact]
    public void NSubstitute_AnyArgument()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(10);

        Assert.Equal(10, calc.Add(99, 88));
        Assert.Equal(10, calc.Add(1, 2));
    }
}

public class MethodsAnyArgKnockOffTests
{
    [Fact]
    public void KnockOff_AnyArgument()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return(10);

        ICalculator calc = stub;
        Assert.Equal(10, calc.Add(99, 88));
        Assert.Equal(10, calc.Add(1, 2));
    }
}

// =============================================================================
// METHODS TABLE - Conditional
// =============================================================================

public class MethodsConditionalNSubTests
{
    [Fact]
    public void NSubstitute_Conditional()
    {
        var calc = Substitute.For<ICalculator>();
        #region readme-argmatch-nsub
        // NSubstitute - Arg.Is<T> per parameter (permanent matchers)
        calc.Add(Arg.Is<int>(a => a > 0), Arg.Any<int>())
            .Returns(x => x.ArgAt<int>(0) + x.ArgAt<int>(1));
        #endregion

        Assert.Equal(12, calc.Add(5, 7));  // 5 > 0, returns 5+7
        Assert.Equal(0, calc.Add(-1, 7));  // -1 not > 0, returns default
    }
}

public class MethodsConditionalKnockOffTests
{
    [Fact]
    public void KnockOff_Conditional()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return((a, b) => a > 0 ? a + b : 0);

        ICalculator calc = stub;
        Assert.Equal(12, calc.Add(5, 7));  // 5 > 0, returns 5+7
        Assert.Equal(0, calc.Add(-1, 7));  // -1 not > 0, returns 0
    }
}

// =============================================================================
// METHODS TABLE - Throw
// =============================================================================

public class MethodsThrowNSubTests
{
    [Fact]
    public void NSubstitute_Throw()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Add(Arg.Any<int>(), Arg.Any<int>())
            .Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => calc.Add(1, 2));
    }
}

public class MethodsThrowKnockOffTests
{
    [Fact]
    public void KnockOff_Throw()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return((a, b) => throw new InvalidOperationException());

        ICalculator calc = stub;
        Assert.Throws<InvalidOperationException>(() => calc.Add(1, 2));
    }
}

// =============================================================================
// METHODS TABLE - Callback
// =============================================================================

public class MethodsCallbackNSubTests
{
    [Fact]
    public void NSubstitute_Callback()
    {
        var calc = Substitute.For<ICalculator>();
        var log = new List<string>();

        calc.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns(3)
            .AndDoes(x => log.Add($"Add({x.ArgAt<int>(0)}, {x.ArgAt<int>(1)})"));

        var result = calc.Add(1, 2);

        Assert.Equal(3, result);
        Assert.Single(log);
    }
}

public class MethodsCallbackKnockOffTests
{
    [Fact]
    public void KnockOff_Callback()
    {
        var stub = new CompareCalculatorStub();
        var log = new List<string>();

        stub.Add.Return((a, b) => { log.Add($"Add({a}, {b})"); return 3; });

        ICalculator calc = stub;
        var result = calc.Add(1, 2);

        Assert.Equal(3, result);
        Assert.Single(log);
    }
}

// =============================================================================
// METHODS TABLE - Async
// =============================================================================

public class MethodsAsyncNSubTests
{
    [Fact]
    public async Task NSubstitute_Async()
    {
        var repo = Substitute.For<IUserRepo>();
        var user = new User { Id = 1, Name = "Test" };

        repo.GetUserAsync(1).Returns(user);

        var result = await repo.GetUserAsync(1);
        Assert.Equal("Test", result?.Name);
    }
}

public class MethodsAsyncKnockOffTests
{
    [Fact]
    public async Task KnockOff_Async()
    {
        var stub = new CompareUserRepoStub();
        var user = new User { Id = 1, Name = "Test" };

        stub.GetUserAsync.Return(user);

        IUserRepo repo = stub;
        var result = await repo.GetUserAsync(1);
        Assert.Equal("Test", result?.Name);
    }
}

// =============================================================================
// METHODS TABLE - Verify called
// =============================================================================

public class MethodsVerifyNSubTests
{
    [Fact]
    public void NSubstitute_VerifyCalled()
    {
        var calc = Substitute.For<ICalculator>();

        calc.Add(1, 2);

        calc.Received().Add(1, 2);
    }
}

public class MethodsVerifyKnockOffTests
{
    [Fact]
    public void KnockOff_VerifyCalled()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return((a, b) => a + b);

        ICalculator calc = stub;
        calc.Add(1, 2);

        stub.Add.Verify();
    }
}

// =============================================================================
// METHODS TABLE - Verify count
// =============================================================================

public class MethodsVerifyCountNSubTests
{
    [Fact]
    public void NSubstitute_VerifyCount()
    {
        var calc = Substitute.For<ICalculator>();

        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        calc.Received(3).Add(Arg.Any<int>(), Arg.Any<int>());
    }
}

public class MethodsVerifyCountKnockOffTests
{
    [Fact]
    public void KnockOff_VerifyCount()
    {
        var stub = new CompareCalculatorStub();
        stub.Add.Return((a, b) => a + b);

        ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        stub.Add.Verify(Times.Exactly(3));
    }
}

// =============================================================================
// ARGUMENT CAPTURE - Side-by-side code block
// =============================================================================

public class ArgumentCaptureNSubTests
{
    [Fact]
    public void NSubstitute_ArgMatchComparison()
    {
        var calc = Substitute.For<ICalculator>();
        var stub = new CompareCalculatorStub();

        #region readme-argmatch-nsub-matchers
        // NSubstitute - Arg.Is<T> per parameter (permanent matchers)
        calc.Add(Arg.Is<int>(a => a > 0), Arg.Any<int>()).Returns(100);
        #endregion

        #region readme-argmatch-knockoff-oncall
        // KnockOff - Returns with conditional (permanent, matches all calls)
        stub.Add.Return((a, b) => a > 0 ? 100 : 0);
        #endregion

        Assert.Equal(100, calc.Add(5, 7));
        ICalculator ko = stub;
        Assert.Equal(100, ko.Add(5, 7));
    }

    [Fact]
    public void KnockOff_WhenComparison()
    {
        var stub = new CompareCalculatorStub();
        #region readme-argmatch-knockoff-when
        // KnockOff - When() for sequential matching (first match returns 100, then falls through)
        stub.Add.When((a, b) => a > 0).Return(100).ThenCall((a, b) => a + b);
        #endregion

        ICalculator calc = stub;
        Assert.Equal(100, calc.Add(5, 7));
    }

    [Fact]
    public void KnockOff_WhenSpecificValues()
    {
        var calc = Substitute.For<ICalculator>();
        var stub = new CompareCalculatorStub();

        #region readme-argmatch-nsub-specific
        // Multiple specific values
        calc.Add(1, 2).Returns(100);
        calc.Add(3, 4).Returns(200);
        #endregion

        #region readme-argmatch-knockoff-specific
        stub.Add.When(1, 2).Return(100);
        stub.Add.When(3, 4).Return(200);
        #endregion

        Assert.Equal(100, calc.Add(1, 2));
        ICalculator ko = stub;
        Assert.Equal(100, ko.Add(1, 2));
    }

    [Fact]
    public void NSubstitute_ArgumentCapture()
    {
        var calc = Substitute.For<ICalculator>();

        #region readme-argcapture-nsub
        // NSubstitute - requires Arg.Do in setup
        int capturedA = 0, capturedB = 0;
        calc.Add(Arg.Do<int>(x => capturedA = x), Arg.Do<int>(x => capturedB = x));
        calc.Add(1, 2);
        #endregion

        Assert.Equal(1, capturedA);
        Assert.Equal(2, capturedB);
    }
}

public class ArgumentCaptureKnockOffTests
{
    [Fact]
    public void KnockOff_ArgumentCapture()
    {
        var stub = new CompareCalculatorStub();

        #region readme-argcapture-knockoff
        // KnockOff - built-in, no pre-setup
        var tracking = stub.Add.Return((a, b) => a + b);
        ICalculator calc = stub;
        calc.Add(1, 2);
        var (a, b) = tracking.LastArgs;  // Named tuple: a = 1, b = 2
        #endregion

        Assert.Equal(1, a);
        Assert.Equal(2, b);
    }
}

// =============================================================================
// PROPERTIES TABLE - Setup getter
// =============================================================================

public class PropertiesGetterNSubTests
{
    [Fact]
    public void NSubstitute_SetupGetter()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Mode.Returns("Scientific");

        Assert.Equal("Scientific", calc.Mode);
    }
}

public class PropertiesGetterKnockOffTests
{
    [Fact]
    public void KnockOff_SetupGetter()
    {
        var stub = new CompareCalculatorStub();
        stub.Mode.Get("Scientific");

        ICalculator calc = stub;
        Assert.Equal("Scientific", calc.Mode);
    }
}

// =============================================================================
// PROPERTIES TABLE - Setup setter
// =============================================================================

public class PropertiesSetterNSubTests
{
    [Fact]
    public void NSubstitute_SetupSetter()
    {
        var calc = Substitute.For<ICalculator>();
        string? captured = null;

        calc.When(x => x.Mode = Arg.Any<string>())
            .Do(x => captured = x.Arg<string>());

        calc.Mode = "Degrees";

        Assert.Equal("Degrees", captured);
    }
}

public class PropertiesSetterKnockOffTests
{
    [Fact]
    public void KnockOff_SetupSetter()
    {
        var stub = new CompareCalculatorStub();
        string? captured = null;

        stub.Mode.Set((v) => captured = v);

        ICalculator calc = stub;
        calc.Mode = "Degrees";

        Assert.Equal("Degrees", captured);
    }
}

// =============================================================================
// PROPERTIES TABLE - Verify getter
// =============================================================================

public class PropertiesVerifyGetNSubTests
{
    [Fact]
    public void NSubstitute_VerifyGetter()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Mode.Returns("test");

        _ = calc.Mode;

        _ = calc.Received().Mode;
    }
}

public class PropertiesVerifyGetKnockOffTests
{
    [Fact]
    public void KnockOff_VerifyGetter()
    {
        var stub = new CompareCalculatorStub();
        stub.Mode.Get("test");

        ICalculator calc = stub;
        _ = calc.Mode;

        stub.Mode.VerifyGet();
    }
}

// =============================================================================
// PROPERTIES TABLE - Verify setter
// =============================================================================

public class PropertiesVerifySetNSubTests
{
    [Fact]
    public void NSubstitute_VerifySetter()
    {
        var calc = Substitute.For<ICalculator>();

        calc.Mode = "Scientific";

        calc.Received().Mode = "Scientific";
    }
}

public class PropertiesVerifySetKnockOffTests
{
    [Fact]
    public void KnockOff_VerifySetter()
    {
        var stub = new CompareCalculatorStub();

        ICalculator calc = stub;
        calc.Mode = "Scientific";

        stub.Mode.VerifySet();
    }
}

// =============================================================================
// PROPERTIES TABLE - Verify count
// =============================================================================

public class PropertiesVerifyCountNSubTests
{
    [Fact]
    public void NSubstitute_VerifyGetCount()
    {
        var calc = Substitute.For<ICalculator>();
        calc.Mode.Returns("test");

        _ = calc.Mode;
        _ = calc.Mode;
        _ = calc.Mode;

        _ = calc.Received(3).Mode;
    }
}

public class PropertiesVerifyCountKnockOffTests
{
    [Fact]
    public void KnockOff_VerifyGetCount()
    {
        var stub = new CompareCalculatorStub();
        stub.Mode.Get("test");

        ICalculator calc = stub;
        _ = calc.Mode;
        _ = calc.Mode;
        _ = calc.Mode;

        stub.Mode.VerifyGet(Times.Exactly(3));
    }
}

// =============================================================================
// PROPERTIES TABLE - Capture value
// =============================================================================

public class PropertiesCaptureNSubTests
{
    [Fact]
    public void NSubstitute_CaptureSetValue()
    {
        var calc = Substitute.For<ICalculator>();
        string? captured = null;

        calc.When(x => x.Mode = Arg.Do<string>(v => captured = v)).Do(_ => { });

        calc.Mode = "Radians";

        Assert.Equal("Radians", captured);
    }
}

public class PropertiesCaptureKnockOffTests
{
    [Fact]
    public void KnockOff_CaptureSetValue()
    {
        var stub = new CompareCalculatorStub();

        ICalculator calc = stub;
        calc.Mode = "Radians";

        // Built-in LastSetValue - no setup required
        Assert.Equal("Radians", stub.Mode.LastSetValue);
    }
}

// =============================================================================
// EVENTS TABLE - Raise event
// =============================================================================

public class EventsRaiseNSubTests
{
    [Fact]
    public void NSubstitute_RaiseEvent()
    {
        var calc = Substitute.For<ICalculator>();
        var raised = false;

        calc.PoweringUp += (sender, args) => raised = true;

        calc.PoweringUp += Raise.Event();

        Assert.True(raised);
    }
}

public class EventsRaiseKnockOffTests
{
    [Fact]
    public void KnockOff_RaiseEvent()
    {
        var stub = new CompareCalculatorStub();
        var raised = false;

        ICalculator calc = stub;
        calc.PoweringUp += (sender, args) => raised = true;

        stub.PoweringUp.Raise(stub, EventArgs.Empty);

        Assert.True(raised);
    }
}

// =============================================================================
// EVENTS TABLE - Verify subscription (KnockOff unique feature)
// =============================================================================

public class EventsVerifySubscriptionKnockOffTests
{
    [Fact]
    public void KnockOff_VerifySubscription()
    {
        var stub = new CompareCalculatorStub();

        ICalculator calc = stub;
        calc.PoweringUp += (sender, args) => { };

        stub.PoweringUp.VerifyAdd(Times.Once);
    }

    [Fact]
    public void KnockOff_VerifyUnsubscription()
    {
        var stub = new CompareCalculatorStub();
        EventHandler handler = (sender, args) => { };

        ICalculator calc = stub;
        calc.PoweringUp += handler;
        calc.PoweringUp -= handler;

        stub.PoweringUp.VerifyRemove(Times.Once);
    }

    [Fact]
    public void KnockOff_HasSubscribers()
    {
        var stub = new CompareCalculatorStub();

        ICalculator calc = stub;
        Assert.False(stub.PoweringUp.HasSubscribers);

        calc.PoweringUp += (sender, args) => { };
        Assert.True(stub.PoweringUp.HasSubscribers);
    }
}

// =============================================================================
// DELEGATES TABLE - Setup
// =============================================================================

public class DelegatesSetupNSubTests
{
    [Fact]
    public void NSubstitute_DelegateSetup()
    {
        var factory = Substitute.For<StringFactory>();
        factory(Arg.Any<int>()).Returns("result");

        Assert.Equal("result", factory(42));
    }
}

public class DelegatesSetupKnockOffTests
{
    [Fact]
    public void KnockOff_DelegateSetup()
    {
        var stub = new CompareDelegateStubHolder.Stubs.StringFactory();
        stub.Interceptor.Return("result");

        StringFactory factory = stub;
        Assert.Equal("result", factory(42));
    }
}

// =============================================================================
// DELEGATES TABLE - With logic
// =============================================================================

public class DelegatesLogicNSubTests
{
    [Fact]
    public void NSubstitute_DelegateWithLogic()
    {
        var factory = Substitute.For<StringFactory>();
        factory(Arg.Is<int>(x => x > 0)).Returns(x => $"val: {x.Arg<int>()}");

        Assert.Equal("val: 42", factory(42));
    }
}

public class DelegatesLogicKnockOffTests
{
    [Fact]
    public void KnockOff_DelegateWithLogic()
    {
        var stub = new CompareDelegateStubHolder.Stubs.StringFactory();
        stub.Interceptor.Return((x) => $"val: {x}");

        StringFactory factory = stub;
        Assert.Equal("val: 42", factory(42));
    }
}

// =============================================================================
// DELEGATES TABLE - Verify
// =============================================================================

public class DelegatesVerifyNSubTests
{
    [Fact]
    public void NSubstitute_DelegateVerify()
    {
        var factory = Substitute.For<StringFactory>();

        factory(42);

        factory.Received()(42);
    }
}

public class DelegatesVerifyKnockOffTests
{
    [Fact]
    public void KnockOff_DelegateVerify()
    {
        var stub = new CompareDelegateStubHolder.Stubs.StringFactory();
        stub.Interceptor.Return((x) => "");

        StringFactory factory = stub;
        factory(42);

        stub.Interceptor.Verify();
    }
}

// =============================================================================
// DELEGATES TABLE - Capture (KnockOff unique feature)
// =============================================================================

public class DelegatesCaptureKnockOffTests
{
    [Fact]
    public void KnockOff_DelegateCapture()
    {
        var stub = new CompareDelegateStubHolder.Stubs.StringFactory();
        stub.Interceptor.Return((x) => "");

        StringFactory factory = stub;
        factory(42);

        // Built-in capture - no manual setup required
        Assert.Equal(42, stub.Interceptor.LastArg);
    }
}

// =============================================================================
// INDEXERS TABLE - Setup getter
// =============================================================================

public class IndexersGetterNSubTests
{
    [Fact]
    public void NSubstitute_IndexerGetter()
    {
        var cache = Substitute.For<ICache>();
        cache["key"].Returns("42");

        Assert.Equal("42", cache["key"]);
    }
}

public class IndexersGetterKnockOffTests
{
    [Fact]
    public void KnockOff_IndexerGetter()
    {
        var stub = new CompareCacheStub();
        stub.Indexer.Backing["key"] = "42";

        ICache cache = stub;
        Assert.Equal("42", cache["key"]);
    }
}

// =============================================================================
// INDEXERS TABLE - Dynamic getter
// =============================================================================

public class IndexersDynamicNSubTests
{
    [Fact]
    public void NSubstitute_IndexerDynamic()
    {
        var cache = Substitute.For<ICache>();
        cache[Arg.Any<string>()].Returns("default");

        Assert.Equal("default", cache["any"]);
        Assert.Equal("default", cache["key"]);
    }
}

public class IndexersDynamicKnockOffTests
{
    [Fact]
    public void KnockOff_IndexerDynamic()
    {
        var stub = new CompareCacheStub();
        stub.Indexer.Get((key) => "default");

        ICache cache = stub;
        Assert.Equal("default", cache["any"]);
        Assert.Equal("default", cache["key"]);
    }
}

// =============================================================================
// INDEXERS TABLE - Verify getter
// =============================================================================

public class IndexersVerifyGetNSubTests
{
    [Fact]
    public void NSubstitute_IndexerVerifyGet()
    {
        var cache = Substitute.For<ICache>();

        _ = cache["key"];

        _ = cache.Received()["key"];
    }
}

public class IndexersVerifyGetKnockOffTests
{
    [Fact]
    public void KnockOff_IndexerVerifyGet()
    {
        var stub = new CompareCacheStub();

        ICache cache = stub;
        _ = cache["key"];

        stub.Indexer.VerifyGet();
    }
}

// =============================================================================
// INDEXERS TABLE - Verify setter
// =============================================================================

public class IndexersVerifySetNSubTests
{
    [Fact]
    public void NSubstitute_IndexerVerifySet()
    {
        var cache = Substitute.For<ICache>();

        cache["key"] = "42";

        cache.Received()["key"] = "42";
    }
}

public class IndexersVerifySetKnockOffTests
{
    [Fact]
    public void KnockOff_IndexerVerifySet()
    {
        var stub = new CompareCacheStub();

        ICache cache = stub;
        cache["key"] = "42";

        stub.Indexer.VerifySet();
    }
}

// =============================================================================
// INDEXERS TABLE - Capture (KnockOff unique feature)
// =============================================================================

public class IndexersCaptureKnockOffTests
{
    [Fact]
    public void KnockOff_IndexerCapture()
    {
        var stub = new CompareCacheStub();

        ICache cache = stub;
        cache["key"] = "42";

        // Built-in capture - no manual setup required
        Assert.NotNull(stub.Indexer.LastSetEntry);
        Assert.Equal("key", stub.Indexer.LastSetEntry.Value.Key);
        Assert.Equal("42", stub.Indexer.LastSetEntry.Value.Value);
    }
}

// =============================================================================
// SOURCE DELEGATION - README unique feature section
// =============================================================================

public class SourceDelegationReadmeTests
{
    [Fact]
    public void Source_DelegateAndOverride()
    {
        var realRepo = new SimpleUserRepo();
        var stub = new CompareUserRepoStub();

        #region readme-source-delegation
        stub.Source(realRepo);  // ALL methods delegate to real implementation

        // Override just the method you're testing
        stub.GetUser.Return((id) => new User { Id = id, Name = "Test User" });

        IUserRepo repo = stub;
        repo.Save(new User { Id = 1 });  // Calls real SimpleUserRepo.Save()
        var user = repo.GetUser(1);       // Returns test data
        #endregion

        Assert.Equal("Test User", user?.Name);
    }
}

public class SimpleUserRepo : IUserRepo
{
    private readonly List<User> _users = [];

    public User? GetUser(int id) => _users.FirstOrDefault(u => u.Id == id);
    public Task<User?> GetUserAsync(int id) => Task.FromResult(GetUser(id));
    public void Save(User user) => _users.Add(user);
}

// =============================================================================
// PATTERN EXAMPLES - Three stub patterns
// =============================================================================

#region readme-pattern-standalone
[KnockOff]
public partial class ReadmeStandaloneStub : IUserRepo { }
#endregion

[KnockOff<IUserRepo>]
public partial class ReadmeInlineTests
{
    #region readme-pattern-inline-interface
    [Fact]
    public void InlineInterface_Pattern()
    {
        var stub = new Stubs.IUserRepo();
        stub.GetUser.Return((id) => new User { Id = id });

        IUserRepo repo = stub;
        Assert.NotNull(repo.GetUser(1));
    }
    #endregion
}

public abstract class MyService
{
    public abstract User? GetUser(int id);
}

[KnockOff<MyService>]
public partial class ReadmeInlineClassTests
{
    #region readme-pattern-inline-class
    [Fact]
    public void InlineClass_Pattern()
    {
        var stub = new Stubs.MyService();
        stub.GetUser.Return((id) => new User { Id = id });

        MyService service = stub.Object;
        Assert.NotNull(service.GetUser(1));
    }
    #endregion
}
