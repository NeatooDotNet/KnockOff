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
    #region methods-oncall-void
    [Fact]
    public void VoidMethod_ConfiguredWithOnCall()
    {
        var stub = new LogSvcMethodsStub();

        // OnCall for void methods uses Action<TStub, ...params>
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
    }
    #endregion

    #region methods-oncall-return
    [Fact]
    public void MethodWithReturn_ConfiguredWithOnCall()
    {
        var stub = new LogSvcMethodsStub();

        // OnCall with return value: first param is stub (ko), then method params
        var tracking = stub.GetUserName.OnCall((userId) => "TestUser");

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("TestUser", name);
        tracking.Verify();
    }
    #endregion

    #region methods-oncall-multi-param
    [Fact]
    public void MethodWithMultipleParams_AllAvailableInOnCall()
    {
        var stub = new AuthSvcMethodsStub();

        // All method parameters follow the stub instance (ko)
        var tracking = stub.ValidateCredentials.OnCall((username, password) =>
            username == "admin" && password == "secret");

        IAuthSvcMethods auth = stub;

        Assert.True(auth.ValidateCredentials("admin", "secret"));
        Assert.False(auth.ValidateCredentials("user", "wrong"));

        // Verify exactly 2 calls were made
        tracking.Verify(Times.Exactly(2));
    }
    #endregion
}

// =============================================================================
// Method Verification Samples
// =============================================================================

public class MethodVerificationTests
{
    #region methods-verify-wascalled
    [Fact]
    public void Verify_VerifiesMethodInvocation()
    {
        var stub = new SaveRepoMethodsStub();
        stub.Save.OnCall((entity) => { }).Verifiable();

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
    #endregion

    #region methods-verify-times
    [Fact]
    public void Verify_WithTimesConstraint()
    {
        var stub = new NotifierMethodsStub();
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
    }
    #endregion

    #region methods-verify-callcount
    [Fact]
    public void Verify_ExactCallCount()
    {
        var stub = new NotifierMethodsStub();
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
    }
    #endregion

    #region methods-verify-verifiable
    [Fact]
    public void Verifiable_BatchVerification()
    {
        var stub = new SaveRepoMethodsStub();

        // Mark expected calls
        stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
        stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });
        repository.GetById(1);

        // Verify all marked methods (throws if any not called correctly)
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Argument Capture Samples
// =============================================================================

public class ArgumentCaptureTests
{
    #region methods-capture-single
    [Fact]
    public void LastArg_CapturesSingleParameter()
    {
        var stub = new UserRepoMethodsStub();
        var tracking = stub.GetUser.OnCall((userId) => new User { Id = userId });

        IUserRepoMethods repository = stub;
        repository.GetUser(42);

        // LastArg captures the most recent call's argument (from tracking)
        int capturedId = tracking.LastArg;
        Assert.Equal(42, capturedId);
    }
    #endregion

    #region methods-capture-multiple
    [Fact]
    public void LastArgs_CapturesAllParameters()
    {
        var stub = new AuthSvcMethodsStub();
        var tracking = stub.ValidateCredentials.OnCall((username, password) => true);

        IAuthSvcMethods auth = stub;
        auth.ValidateCredentials("admin", "secret123");

        // LastArgs is a named tuple with all parameters (from tracking)
        var (username, password) = tracking.LastArgs;
        Assert.Equal("admin", username);
        Assert.Equal("secret123", password);
    }
    #endregion
}

// =============================================================================
// Reset Sample
// =============================================================================

public class MethodResetTests
{
    #region methods-reset
    [Fact]
    public void Reset_ClearsTrackingState()
    {
        var stub = new ProcessorMethodsStub();
        var tracking = stub.ProcessData.OnCall((data) => { });

        IProcessorMethods processor = stub;
        processor.ProcessData("initial");

        // Verify one call was made
        tracking.Verify(Times.Once);

        // Reset clears CallCount on the interceptor
        stub.ProcessData.Reset();

        // After reset, Verify(Times.Never) passes via tracking
        tracking.Verify(Times.Never);
    }
    #endregion
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
    #region methods-complete-example
    [Fact]
    public void UserService_UpdateUserEmail_CallsRepositoryCorrectly()
    {
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
    }
    #endregion
}

// =============================================================================
// Overloaded Methods
// =============================================================================

public class OverloadedMethodTests
{
    #region methods-overloads
    [Fact]
    public void Overloads_DistinguishedByCallbackSignature()
    {
        var stub = new SearchRepoStub();

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
    }
    #endregion
}
