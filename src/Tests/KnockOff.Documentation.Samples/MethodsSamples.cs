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
    public void VoidMethod_ConfiguredWithOnCall()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-void
        // OnCall for void methods uses Action<...params>
        var logged = new List<string>();
        var tracking = stub.LogMessage.OnCall((message) =>
        {
            logged.Add(message);
        });

        ILogSvcMethods logger = stub;
        logger.LogMessage("Hello, World!");

        Assert.Single(logged);
        Assert.Equal("Hello, World!", logged[0]);
        tracking.Verify();
        #endregion
    }

    [Fact]
    public void MethodWithReturn_ConfiguredWithOnCall()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-return
        // OnCall with return value: Func<...params, TReturn>
        var tracking = stub.GetUserName.OnCall((userId) => "TestUser");

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("TestUser", name);
        tracking.Verify();
        #endregion
    }

    [Fact]
    public void MethodWithReturn_ConfiguredWithValue()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-value
        // Returns - simpler syntax when you don't need callback logic
        // Just pass the return value directly
        var tracking = stub.GetUserName.Returns("StaticUser");

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("StaticUser", name);
        tracking.Verify();
        #endregion
    }

    [Fact]
    public void ValueVsCallback_ChooseBasedOnNeed()
    {
        var stub = new LogSvcMethodsStub();

        #region methods-oncall-value-vs-callback
        // Use VALUE when returning a fixed result:
        stub.GetUserName.Returns("Alice");

        // Use CALLBACK when you need:
        // - Dynamic values based on arguments
        // - Side effects
        // - Conditional logic
        stub.GetUserName.OnCall((userId) => userId > 100 ? "Admin" : "User");

        // Both return tracking objects for verification
        #endregion
    }

    [Fact]
    public void MethodWithMultipleParams_AllAvailableInOnCall()
    {
        var stub = new AuthSvcMethodsStub();

        #region methods-oncall-multi-param
        // All method parameters are passed to the callback in order
        var tracking = stub.ValidateCredentials.OnCall((username, password) =>
            username == "admin" && password == "secret");

        IAuthSvcMethods auth = stub;

        Assert.True(auth.ValidateCredentials("admin", "secret"));
        Assert.False(auth.ValidateCredentials("user", "wrong"));

        // Verify exactly 2 calls were made
        tracking.Verify(Times.Exactly(2));
        #endregion
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
        stub.Save.OnCall((entity) => { }).Verifiable();

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
        #endregion
    }

    [Fact]
    public void Verify_WithTimesConstraint()
    {
        var stub = new NotifierMethodsStub();

        #region methods-verify-times
        var tracking = stub.Notify.OnCall((message) => { });

        INotifierMethods notifier = stub;

        // Simulate processing a 2-item collection
        var items = new[] { "item1", "item2" };
        foreach (var item in items)
        {
            notifier.Notify($"Processing {item}");
        }

        // Verify exact call count using Times
        tracking.Verify(Times.Exactly(2));
        #endregion
    }

    [Fact]
    public void Verify_ExactCallCount()
    {
        var stub = new NotifierMethodsStub();

        #region methods-verify-callcount
        var tracking = stub.Notify.OnCall((message) => { });

        INotifierMethods notifier = stub;

        // Simulate processing a 2-item collection
        var items = new[] { "item1", "item2" };
        foreach (var item in items)
        {
            notifier.Notify($"Processing {item}");
        }

        // Verify exactly 2 calls (throws if different)
        tracking.Verify(Times.Exactly(2));
        #endregion
    }

    [Fact]
    public void Verifiable_BatchVerification()
    {
        var stub = new SaveRepoMethodsStub();

        #region methods-verify-verifiable
        // Mark expected calls
        stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
        stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });
        repository.GetById(1);

        // Verify all marked methods (throws if any not called correctly)
        stub.Verify();
        #endregion
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

        #region methods-capture-single
        var tracking = stub.GetUser.OnCall((userId) => new User { Id = userId });

        IUserRepoMethods repository = stub;
        repository.GetUser(42);

        // LastArg captures the most recent call's argument (from tracking)
        int capturedId = tracking.LastArg;
        Assert.Equal(42, capturedId);
        #endregion
    }

    [Fact]
    public void LastArgs_CapturesAllParameters()
    {
        var stub = new AuthSvcMethodsStub();

        #region methods-capture-multiple
        var tracking = stub.ValidateCredentials.OnCall((username, password) => true);

        IAuthSvcMethods auth = stub;
        auth.ValidateCredentials("admin", "secret123");

        // LastArgs is a named tuple with all parameters (from tracking)
        var (username, password) = tracking.LastArgs;
        Assert.Equal("admin", username);
        Assert.Equal("secret123", password);
        #endregion
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

        #region methods-reset
        var tracking = stub.ProcessData.OnCall((data) => { });

        IProcessorMethods processor = stub;
        processor.ProcessData("initial");

        // Verify one call was made
        tracking.Verify(Times.Once);

        // Reset clears CallCount on the interceptor
        stub.ProcessData.Reset();

        // After reset, Verify(Times.Never) passes via tracking
        tracking.Verify(Times.Never);
        #endregion
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
        #region methods-complete-example
        // Arrange
        var stub = new CompleteUserRepoStub();

        var testUser = new User { Id = 1, Name = "Alice", Email = "old@test.com" };
        var getTracking = stub.GetUser.OnCall((id) => id == 1 ? testUser : null).Verifiable();
        var saveTracking = stub.SaveUser.OnCall((user) => { }).Verifiable();

        var service = new UserService(stub);

        // Act
        var result = service.UpdateUserEmail(1, "new@test.com");

        // Assert
        Assert.True(result);

        // Verify both methods were called
        stub.Verify();

        // Verify GetUser was called with correct ID
        Assert.Equal(1, getTracking.LastArg);

        // Verify saved user has new email via the tracking args
        var savedUser = saveTracking.LastArg;
        Assert.Equal("new@test.com", savedUser.Email);
        #endregion
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
        // Overloads are distinguished by the callback parameter types
        // The fully-typed lambda tells KnockOff which overload to configure
        var findAllTracking = stub.Find.OnCall(() =>
            new List<User>()).Verifiable();
        var findByIdTracking = stub.Find.OnCall((int id) =>
            new User { Id = id, Name = "ById" }).Verifiable();
        var findByNameTracking = stub.Find.OnCall((string name) =>
            new User { Id = 1, Name = name }).Verifiable();

        ISearchRepo repo = stub;

        // Call each overload
        repo.Find();
        repo.Find(42);
        repo.Find("Alice");

        // Verify all overloads were called
        stub.Verify();

        // Access last arguments via tracking objects
        Assert.Equal(42, findByIdTracking.LastArg);
        Assert.Equal("Alice", findByNameTracking.LastArg);
        #endregion
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

// =============================================================================
// Sequence Samples
// =============================================================================

public class SequenceTests
{
    [Fact]
    public void Sequence_BasicStatusProgression()
    {
        var stub = new StatusSvcStub();

        #region methods-sequence-basic
        // Configure different returns for successive calls
        stub.GetStatus
            .OnCall(() => "Pending")
            .ThenCall(() => "Processing")
            .ThenCall(() => "Complete");

        IStatusSvc service = stub;

        // Each call returns the next value in sequence
        Assert.Equal("Pending", service.GetStatus());
        Assert.Equal("Processing", service.GetStatus());
        Assert.Equal("Complete", service.GetStatus());
        #endregion
    }

    [Fact]
    public void Sequence_VoidMethods()
    {
        var stub = new NotifierSvcStub();

        #region methods-sequence-void
        // Void method sequences use Action callbacks
        var calls = new List<string>();
        stub.Notify
            .OnCall((msg) => calls.Add("first"))
            .ThenCall((msg) => calls.Add("second"))
            .ThenCall((msg) => calls.Add("third"));

        INotifierSvc notifier = stub;

        notifier.Notify("a");
        notifier.Notify("b");
        notifier.Notify("c");

        Assert.Equal(new[] { "first", "second", "third" }, calls);
        #endregion
    }

    [Fact]
    public void Sequence_ReturnMethods()
    {
        var stub = new CalculatorSvcStub();

        #region methods-sequence-return
        // Return method sequences use Func callbacks
        stub.Calculate
            .OnCall((x, y) => x + y)
            .ThenCall((x, y) => x * y)
            .ThenCall((x, y) => x - y);

        ICalculatorSvc calc = stub;

        // 5 + 3 = 8
        Assert.Equal(8, calc.Calculate(5, 3));

        // 5 * 3 = 15
        Assert.Equal(15, calc.Calculate(5, 3));

        // 5 - 3 = 2
        Assert.Equal(2, calc.Calculate(5, 3));
        #endregion
    }

    [Fact]
    public void Sequence_Exhaustion_ReturnsDefault()
    {
        var stub = new ValueSvcStub();

        #region methods-sequence-exhaustion
        // Sequence callbacks run once each in order
        stub.GetValue
            .OnCall(() => 1)
            .ThenCall(() => 2)
            .ThenCall(() => 3);

        IValueSvc service = stub;

        Assert.Equal(1, service.GetValue());
        Assert.Equal(2, service.GetValue());
        Assert.Equal(3, service.GetValue());

        // After exhaustion: default(int) = 0 in non-strict mode
        Assert.Equal(0, service.GetValue());
        #endregion
    }

    [Fact]
    public void Sequence_MixedValuesAndCallbacks()
    {
        var stub = new StatusSvcStub();

        #region methods-sequence-mixed
        // Mix fixed values with dynamic callbacks using OnCall
        stub.GetStatus
            .OnCall(() => "Initial")
            .ThenCall(() => DateTime.Now.ToString("HH:mm:ss"))
            .ThenCall(() => "Final");

        IStatusSvc service = stub;

        // First call: fixed value
        Assert.Equal("Initial", service.GetStatus());

        // Second call: dynamic value (time)
        var timeResult = service.GetStatus();
        Assert.Matches(@"\d{2}:\d{2}:\d{2}", timeResult);

        // Third call: fixed value
        Assert.Equal("Final", service.GetStatus());
        #endregion
    }

    [Fact]
    public void Sequence_Verification()
    {
        var stub = new ProcessSvcStub();

        #region methods-sequence-verification
        // Sequence can be verified like any callback
        var sequence = stub.Process
            .OnCall(() => { })
            .ThenCall(() => { })
            .ThenCall(() => { });

        IProcessSvc processor = stub;
        processor.Process();
        processor.Process();
        processor.Process();

        // Verify sequence was exhausted
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
            .OnCall(() => { })
            .ThenCall(() => { })
            .Verifiable();

        IProcessSvc processor = stub;
        processor.Process();
        processor.Process();

        // stub.Verify() checks all Verifiable() sequences completed
        stub.Verify();
        #endregion
    }
}
