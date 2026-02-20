using KnockOff;

namespace KnockOff.Documentation.Samples.Methods;

// =============================================================================
// Interfaces for Method Samples
// =============================================================================

public interface ILogSvcMethods
{
    void LogMessage(string message);
    string GetUserName(int userId);
}

public interface ISaveRepoMethods
{
    void Save(object entity);
    User GetById(int id);
}

public interface INotifierMethods
{
    void Notify(string message);
}

public interface IUserRepoMethods
{
    User? GetUser(int userId);
}

public interface IAuthSvcMethods
{
    bool ValidateCredentials(string username, string password);
}

public interface IProcessorMethods
{
    void ProcessData(string data);
}

public interface ISearchRepo
{
    List<User> Find();
    User? Find(int id);
    User? Find(string name);
}

// =============================================================================
// Stubs for Method Samples
// =============================================================================

[KnockOff]
public partial class LogSvcMethodsStub : ILogSvcMethods { }

[KnockOff]
public partial class SaveRepoMethodsStub : ISaveRepoMethods { }

[KnockOff]
public partial class NotifierMethodsStub : INotifierMethods { }

[KnockOff]
public partial class UserRepoMethodsStub : IUserRepoMethods { }

[KnockOff]
public partial class AuthSvcMethodsStub : IAuthSvcMethods { }

[KnockOff]
public partial class ProcessorMethodsStub : IProcessorMethods { }

[KnockOff]
public partial class SearchRepoStub : ISearchRepo { }

// =============================================================================
// Method Configuration Samples
// =============================================================================

public class MethodConfigurationTests
{
    [Fact]
    public void VoidMethod_ConfiguredWithReturn()
    {
        var stub = new LogSvcMethodsStub();
        var logged = new List<string>();

        #region methods-oncall-void
        // Return for void methods uses Action<...params>
        stub.LogMessage.Call((message) => logged.Add(message));
        #endregion

        ILogSvcMethods logger = stub;
        logger.LogMessage("Hello, World!");

        Assert.Single(logged);
        Assert.Equal("Hello, World!", logged[0]);
    }

    [Fact]
    public void MethodWithReturn_ConfiguredWithReturn()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-return
        // Return with return value: Func<...params, TReturn>
        stub.GetUserName.Call((userId) => "TestUser");
        #endregion

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("TestUser", name);
    }

    [Fact]
    public void MethodWithReturn_ConfiguredWithValue()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-value
        // Returns - simpler syntax when you don't need callback logic
        stub.GetUserName.Return("StaticUser");
        #endregion

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("StaticUser", name);
    }

    [Fact]
    public void ValueVsCallback_ChooseBasedOnNeed()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-value-vs-callback
        // Use VALUE when returning a fixed result:
        stub.GetUserName.Return("Alice");

        // Use CALLBACK when you need:
        // - Dynamic values based on arguments
        // - Side effects
        // - Conditional logic
        stub.GetUserName.Call((userId) => userId > 100 ? "Admin" : "User");

        // Both return tracking objects for verification
        #endregion
    }

    [Fact]
    public void MethodWithMultipleParams_AllAvailableInReturn()
    {
        var stub = new AuthSvcMethodsStub();

        #region methods-oncall-multi-param
        // All method parameters are passed to the callback in order
        stub.ValidateCredentials.Call((string username, string password) =>
            username == "admin" && password == "secret");
        #endregion

        IAuthSvcMethods auth = stub;

        Assert.True(auth.ValidateCredentials("admin", "secret"));
        Assert.False(auth.ValidateCredentials("user", "wrong"));
    }
}

// =============================================================================
// Method Verification Samples
// =============================================================================

public class MethodVerificationTests
{
    [Fact]
    public void Verify_VerifiesMethodInvocation()
    {
        var stub = new SaveRepoMethodsStub();

        #region methods-verify-wascalled
        // Mark with Verifiable(), then stub.Verify() checks all marked members
        stub.Save.Call((entity) => { }).Verifiable();
        #endregion

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });

        stub.Verify();
    }

    [Fact]
    public void Verify_ExactCallCount()
    {
        var stub = new NotifierMethodsStub();
        var tracking = stub.Notify.Call((message) => { });

        INotifierMethods notifier = stub;
        notifier.Notify("item1");
        notifier.Notify("item2");

        #region methods-verify-callcount
        // Verify exact call count (throws if different)
        tracking.Verify(Called.Exactly(2));
        #endregion
    }

    [Fact]
    public void Verifiable_BatchVerification()
    {
        var stub = new SaveRepoMethodsStub();

        #region methods-verify-verifiable
        // Mark expected calls with Verifiable(), then stub.Verify() checks all
        stub.Save.Call((entity) => { }).Verifiable(Called.Once);
        stub.GetById.Call((id) => new User { Id = id }).Verifiable();
        #endregion

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });
        repository.GetById(1);

        stub.Verify();
    }
}

// =============================================================================
// Argument Capture Samples
// =============================================================================

public class ArgumentCaptureTests
{
    [Fact]
    public void LastArg_CapturesSingleParameter()
    {
        var stub = new UserRepoMethodsStub();
        var tracking = stub.GetUser.Call((userId) => new User { Id = userId });

        IUserRepoMethods repository = stub;
        repository.GetUser(42);

        #region methods-capture-single
        // LastArg captures the most recent call's argument
        int capturedId = tracking.LastArg;
        #endregion

        Assert.Equal(42, capturedId);
    }

    [Fact]
    public void LastArgs_CapturesAllParameters()
    {
        var stub = new AuthSvcMethodsStub();
        var tracking = stub.ValidateCredentials.Call((string username, string password) => true);

        IAuthSvcMethods auth = stub;
        auth.ValidateCredentials("admin", "secret123");

        #region methods-capture-multiple
        // LastArgs is a named tuple with all parameters
        var (username, password) = tracking.LastArgs;
        #endregion

        Assert.Equal("admin", username);
        Assert.Equal("secret123", password);
    }
}

// =============================================================================
// Reset Sample
// =============================================================================

public class MethodResetTests
{
    [Fact]
    public void Reset_ClearsTrackingState()
    {
        var stub = new ProcessorMethodsStub();
        var tracking = stub.ProcessData.Call((data) => { });

        IProcessorMethods processor = stub;
        processor.ProcessData("initial");

        tracking.Verify(Called.Once);

        #region methods-reset
        // Reset clears call count and captured arguments, but preserves callbacks
        stub.ProcessData.Reset();
        #endregion

        tracking.Verify(Called.Never);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public interface ICompleteUserRepo
{
    User? GetUser(int id);
    void SaveUser(User user);
}

[KnockOff]
public partial class CompleteUserRepoStub : ICompleteUserRepo { }

// System under test
public class UserService
{
    private readonly ICompleteUserRepo _repository;

    public UserService(ICompleteUserRepo repository)
    {
        _repository = repository;
    }

    public bool UpdateUserEmail(int userId, string newEmail)
    {
        var user = _repository.GetUser(userId);
        if (user == null) return false;

        user.Email = newEmail;
        _repository.SaveUser(user);
        return true;
    }
}

public class CompleteMethodExampleTests
{
    [Fact]
    public void UserService_UpdateUserEmail_CallsRepositoryCorrectly()
    {
        var stub = new CompleteUserRepoStub();
        var testUser = new User { Id = 1, Name = "Alice", Email = "old@test.com" };

        #region methods-complete-example
        // Configure stub with tracking
        var getTracking = stub.GetUser.Call((id) => id == 1 ? testUser : null).Verifiable();
        var saveTracking = stub.SaveUser.Call((user) => { }).Verifiable();
        #endregion

        var service = new UserService(stub);
        var result = service.UpdateUserEmail(1, "new@test.com");

        Assert.True(result);
        stub.Verify();
        Assert.Equal(1, getTracking.LastArg);
        Assert.Equal("new@test.com", saveTracking.LastArg!.Email);
    }
}

// =============================================================================
// Overloaded Methods
// =============================================================================

public class OverloadedMethodTests
{
    [Fact]
    public void Overloads_DistinguishedByCallbackSignature()
    {
        var stub = new SearchRepoStub();

        #region methods-overloads
        // Fully-typed lambda tells KnockOff which overload to configure
        stub.Find.Call(() => new List<User>());
        stub.Find.Call((int id) => new User { Id = id, Name = "ById" });
        stub.Find.Call((string name) => new User { Id = 1, Name = name });
        #endregion

        ISearchRepo repo = stub;

        repo.Find();
        repo.Find(42);
        repo.Find("Alice");
    }
}

// =============================================================================
// Interfaces for Sequence Samples
// =============================================================================

public interface IStatusSvc
{
    string GetStatus();
}

public interface INotifierSvc
{
    void Notify(string message);
}

public interface ICalculatorSvc
{
    int Calculate(int x, int y);
}

public interface IValueSvc
{
    int GetValue();
}

public interface IProcessSvc
{
    void Process();
}

public interface IDataSvc
{
    Task<string> GetDataAsync(int id);
}

// =============================================================================
// Stubs for Sequence Samples
// =============================================================================

[KnockOff]
public partial class StatusSvcStub : IStatusSvc { }

[KnockOff]
public partial class NotifierSvcStub : INotifierSvc { }

[KnockOff]
public partial class CalculatorSvcStub : ICalculatorSvc { }

[KnockOff]
public partial class ValueSvcStub : IValueSvc { }

[KnockOff]
public partial class ProcessSvcStub : IProcessSvc { }

[KnockOff]
public partial class DataSvcStub : IDataSvc { }

// =============================================================================
// Sequence Samples
// =============================================================================

public class SequenceTests
{
    [Fact]
    public void Sequence_ParamsIntro()
    {
        var stub = new ValueSvcStub();
        var addStub = new CalculatorSvcStub();

        #region methods-params-sequence-intro
        // Use PARAMS SEQUENCE for multiple constant values:
        // Returns 1, then 2, then 3, then repeats 3
        stub.GetValue.Return(1, 2, 3);

        // Mix callbacks with params for complex sequences:
        // First call uses callback, then returns 100, 200, 300
        addStub.Calculate.Call((int x, int y) => x + y).ThenReturn(100, 200, 300);
        #endregion

        IValueSvc service = stub;
        ICalculatorSvc calc = addStub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Equal(3, service.GetValue());
        Assert.Equal(3, service.GetValue());

        Assert.Equal(8, calc.Calculate(5, 3));   // computed
        Assert.Equal(100, calc.Calculate(0, 0)); // constant
    }

    [Fact]
    public void Sequence_ParamsSyntax()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-params
        // Returns(first, params rest) for value sequences
        stub.GetValue.Return(1, 2, 3);
        #endregion

        IValueSvc service = stub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Equal(3, service.GetValue());
        Assert.Equal(3, service.GetValue()); // repeats last value
    }

    [Fact]
    public async Task Sequence_ParamsAsync()
    {
        var stub = new DataSvcStub();

        #region methods-sequence-params-async
        // Async methods auto-wrap values - no Task.FromResult needed
        stub.GetDataAsync.Return("first", "second", "third");
        #endregion

        IDataSvc service = stub;

        Assert.Equal("first", await service.GetDataAsync(1));
        Assert.Equal("second", await service.GetDataAsync(2));
        Assert.Equal("third", await service.GetDataAsync(3));
        Assert.Equal("third", await service.GetDataAsync(4)); // repeats last value
    }

    [Fact]
    public void Sequence_CallbackThenParams()
    {
        var stub = new CalculatorSvcStub();

        #region methods-sequence-callback-then-params
        // Return for first callback, then ThenReturn for constant values
        stub.Calculate
            .Call((int x, int y) => x + y)
            .ThenReturn(100, 200, 300);
        #endregion

        ICalculatorSvc calc = stub;

        Assert.Equal(8, calc.Calculate(5, 3));   // 5 + 3 = 8 (computed)
        Assert.Equal(100, calc.Calculate(0, 0)); // constant
        Assert.Equal(200, calc.Calculate(0, 0)); // constant
        Assert.Equal(300, calc.Calculate(0, 0)); // constant
        Assert.Equal(300, calc.Calculate(0, 0)); // repeats last
    }

    [Fact]
    public void Sequence_BasicStatusProgression()
    {
        var stub = new StatusSvcStub();

        #region methods-sequence-basic
        // Chain ThenCall() for callback sequences
        stub.GetStatus
            .Call(() => "Pending")
            .ThenReturn(() => "Processing")
            .ThenReturn(() => "Complete");
        #endregion

        IStatusSvc service = stub;

        Assert.Equal("Pending", service.GetStatus());
        Assert.Equal("Processing", service.GetStatus());
        Assert.Equal("Complete", service.GetStatus());
    }

    [Fact]
    public void Sequence_VoidMethods()
    {
        var stub = new NotifierSvcStub();
        var calls = new List<string>();

        #region methods-sequence-void
        // Void method sequences use Action callbacks
        stub.Notify
            .Call((msg) => calls.Add("first"))
            .ThenCall((msg) => calls.Add("second"))
            .ThenCall((msg) => calls.Add("third"));
        #endregion

        INotifierSvc notifier = stub;

        notifier.Notify("a");
        notifier.Notify("b");
        notifier.Notify("c");

        Assert.Equal(new[] { "first", "second", "third" }, calls);
    }

    [Fact]
    public void Sequence_ReturnMethods()
    {
        var stub = new CalculatorSvcStub();

        #region methods-sequence-return
        // Return method sequences use Func callbacks
        stub.Calculate
            .Call((int x, int y) => x + y)
            .ThenReturn((int x, int y) => x * y)
            .ThenReturn((int x, int y) => x - y);
        #endregion

        ICalculatorSvc calc = stub;

        Assert.Equal(8, calc.Calculate(5, 3));   // 5 + 3 = 8
        Assert.Equal(15, calc.Calculate(5, 3));  // 5 * 3 = 15
        Assert.Equal(2, calc.Calculate(5, 3));   // 5 - 3 = 2
    }

    [Fact]
    public void Sequence_Exhaustion_RepeatsLastValue()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-exhaustion
        // After exhaustion: repeats last value (NSubstitute behavior)
        stub.GetValue.Call(() => 1).ThenReturn(() => 2).ThenReturn(() => 3);
        #endregion

        IValueSvc service = stub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Equal(3, service.GetValue());
        Assert.Equal(3, service.GetValue()); // repeats last value
        Assert.Equal(3, service.GetValue());
    }

    [Fact]
    public void Sequence_Exhaustion_WithThenDefault_ReturnsDefault()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-then-default
        // ThenDefault() returns default(T) after exhaustion instead of repeating
        stub.GetValue.Call(() => 1).ThenReturn(() => 2).ThenDefault();
        #endregion

        IValueSvc service = stub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Equal(0, service.GetValue()); // default(int) = 0
        Assert.Equal(0, service.GetValue());
    }

    [Fact]
    public void Sequence_StrictModeThrows()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-strict
        // Strict mode throws on sequence exhaustion
        stub.Strict = true;
        stub.GetValue.Call(() => 1).ThenReturn(() => 2);
        #endregion

        IValueSvc service = stub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Throws<StubException>(() => service.GetValue());
    }

    [Fact]
    public void Sequence_MixedValuesAndCallbacks()
    {
        var stub = new StatusSvcStub();

        #region methods-sequence-mixed
        // Mix fixed values with dynamic callbacks
        stub.GetStatus
            .Call(() => "Initial")
            .ThenReturn(() => DateTime.Now.ToString("HH:mm:ss"))
            .ThenReturn(() => "Final");
        #endregion

        IStatusSvc service = stub;

        Assert.Equal("Initial", service.GetStatus());
        var timeResult = service.GetStatus();
        Assert.Matches(@"\d{2}:\d{2}:\d{2}", timeResult);
        Assert.Equal("Final", service.GetStatus());
    }

    [Fact]
    public void Sequence_Verification()
    {
        var stub = new ProcessSvcStub();
        var sequence = stub.Process
            .Call(() => { })
            .ThenCall(() => { })
            .ThenCall(() => { });

        IProcessSvc processor = stub;
        processor.Process();
        processor.Process();
        processor.Process();

        #region methods-sequence-verification
        // Verify sequence was called the expected number of times
        sequence.Verify();
        #endregion
    }

    [Fact]
    public void Sequence_WithBatchVerification()
    {
        var stub = new ProcessSvcStub();

        #region methods-sequence-with-times
        // Mark sequence for batch verification via stub.Verify()
        stub.Process
            .Call(() => { })
            .ThenCall(() => { })
            .Verifiable();
        #endregion

        IProcessSvc processor = stub;
        processor.Process();
        processor.Process();

        stub.Verify();
    }

    [Fact]
    public void Sequence_NSubstituteStyleFull()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-nsub-style
        // Returns 1 on first call, 2 on second, 3 on third and all subsequent calls
        stub.GetValue.Return(1, 2, 3);

        IValueSvc calc = stub;
        var r1 = calc.GetValue();  // 1
        var r2 = calc.GetValue();  // 2
        var r3 = calc.GetValue();  // 3
        var r4 = calc.GetValue();  // 3 (repeats last value)
        #endregion

        Assert.Equal(1, r1);
        Assert.Equal(2, r2);
        Assert.Equal(3, r3);
        Assert.Equal(3, r4);
    }

    [Fact]
    public void Sequence_SingleVsParams()
    {
        var singleStub = new ValueSvcStub();
        var paramsStub = new ValueSvcStub();

        #region methods-single-vs-params
        // Single value - repeats indefinitely (no sequence)
        singleStub.GetValue.Return(42);

        // Params sequence - progresses through values, repeats last
        paramsStub.GetValue.Return(1, 2, 3);
        #endregion

        IValueSvc single = singleStub;
        IValueSvc multi = paramsStub;

        Assert.Equal(42, single.GetValue());
        Assert.Equal(42, single.GetValue());

        Assert.Equal(1, multi.GetValue());
        Assert.Equal(2, multi.GetValue());
        Assert.Equal(3, multi.GetValue());
        Assert.Equal(3, multi.GetValue());
    }

    [Fact]
    public void Sequence_CallbackThenParamsFull()
    {
        var stub = new CalculatorSvcStub();

        #region methods-sequence-callback-then-params-full
        // First call: compute dynamically
        // Then: return 100, 200, 300 in sequence
        stub.Calculate.Call((int x, int y) => x + y).ThenReturn(100, 200, 300);

        ICalculatorSvc calc = stub;
        var r1 = calc.Calculate(1, 2);  // 3 (computed: 1 + 2)
        var r2 = calc.Calculate(0, 0);  // 100
        var r3 = calc.Calculate(0, 0);  // 200
        var r4 = calc.Calculate(0, 0);  // 300
        var r5 = calc.Calculate(0, 0);  // 300 (repeats last)
        #endregion

        Assert.Equal(3, r1);
        Assert.Equal(100, r2);
        Assert.Equal(200, r3);
        Assert.Equal(300, r4);
        Assert.Equal(300, r5);
    }

    [Fact]
    public async Task Sequence_ParamsAsyncFull()
    {
        var stub = new DataSvcStub();

        #region methods-sequence-params-async-full
        // Async values auto-wrapped - no Task.FromResult needed
        stub.GetDataAsync.Return("first", "second", "third");

        IDataSvc service = stub;
        var r1 = await service.GetDataAsync(1);  // "first"
        var r2 = await service.GetDataAsync(2);  // "second"
        var r3 = await service.GetDataAsync(3);  // "third"
        var r4 = await service.GetDataAsync(4);  // "third" (repeats)
        #endregion

        Assert.Equal("first", r1);
        Assert.Equal("second", r2);
        Assert.Equal("third", r3);
        Assert.Equal("third", r4);
    }

    [Fact]
    public void Sequence_ExhaustionParams()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-exhaustion-params
        stub.GetValue.Return(1, 2);

        IValueSvc calc = stub;
        var r1 = calc.GetValue();  // 1
        var r2 = calc.GetValue();  // 2
        var r3 = calc.GetValue();  // 2 (repeats last)
        var r4 = calc.GetValue();  // 2 (still repeats)
        #endregion

        Assert.Equal(1, r1);
        Assert.Equal(2, r2);
        Assert.Equal(2, r3);
        Assert.Equal(2, r4);
    }

    [Fact]
    public void Sequence_ParamsVerify()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-params-verify
        var sequence = stub.GetValue.Return(1, 2, 3);

        IValueSvc calc = stub;
        calc.GetValue();  // 1
        calc.GetValue();  // 2
        calc.GetValue();  // 3

        sequence.Verify();  // Passes - all 3 values used
        #endregion
    }
}
