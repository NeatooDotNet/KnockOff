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
        var tracking = stub.LogMessage.OnCall((ko, message) =>
        {
            logged.Add(message);
        });

        ILogSvcMethods logger = stub;
        logger.LogMessage("Hello, World!");

        Assert.Single(logged);
        Assert.Equal("Hello, World!", logged[0]);
        Assert.True(tracking.WasCalled);
    }
    #endregion

    #region methods-oncall-return
    [Fact]
    public void MethodWithReturn_ConfiguredWithOnCall()
    {
        var stub = new LogSvcMethodsStub();

        // OnCall with return value: first param is stub (ko), then method params
        var tracking = stub.GetUserName.OnCall((ko, userId) => "TestUser");

        ILogSvcMethods logger = stub;
        var name = logger.GetUserName(42);

        Assert.Equal("TestUser", name);
        Assert.True(tracking.WasCalled);
    }
    #endregion

    #region methods-oncall-multi-param
    [Fact]
    public void MethodWithMultipleParams_AllAvailableInOnCall()
    {
        var stub = new AuthSvcMethodsStub();

        // All method parameters follow the stub instance (ko)
        var tracking = stub.ValidateCredentials.OnCall((ko, username, password) =>
            username == "admin" && password == "secret");

        IAuthSvcMethods auth = stub;

        Assert.True(auth.ValidateCredentials("admin", "secret"));
        Assert.False(auth.ValidateCredentials("user", "wrong"));
        Assert.Equal(2, tracking.CallCount);
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
    public void WasCalled_VerifiesMethodInvocation()
    {
        var stub = new SaveRepoMethodsStub();
        var tracking = stub.Save.OnCall((ko, entity) => { });

        ISaveRepoMethods repository = stub;
        repository.Save(new User { Id = 1 });

        // WasCalled is true if method was invoked at least once
        Assert.True(tracking.WasCalled);
    }
    #endregion

    #region methods-verify-callcount
    [Fact]
    public void CallCount_VerifiesExactInvocations()
    {
        var stub = new NotifierMethodsStub();
        var tracking = stub.Notify.OnCall((ko, message) => { });

        INotifierMethods notifier = stub;

        // Simulate processing a 2-item collection
        var items = new[] { "item1", "item2" };
        foreach (var item in items)
        {
            notifier.Notify($"Processing {item}");
        }

        // Verify exact call count via tracking object
        Assert.Equal(2, tracking.CallCount);
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
        var tracking = stub.GetUser.OnCall((ko, userId) => new User { Id = userId });

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
        var tracking = stub.ValidateCredentials.OnCall((ko, username, password) => true);

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
        var tracking = stub.ProcessData.OnCall((ko, data) => { });

        IProcessorMethods processor = stub;
        processor.ProcessData("initial");

        Assert.Equal(1, tracking.CallCount);

        // Reset clears CallCount, WasCalled on the interceptor
        stub.ProcessData.Reset();

        // Tracking is also reset
        Assert.Equal(0, tracking.CallCount);
        Assert.False(tracking.WasCalled);
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
        var getTracking = stub.GetUser.OnCall((ko, id) => id == 1 ? testUser : null);
        var saveTracking = stub.SaveUser.OnCall((ko, user) => { });

        var service = new UserService(stub);

        // Act
        var result = service.UpdateUserEmail(1, "new@test.com");

        // Assert
        Assert.True(result);

        // Verify GetUser was called with correct ID
        Assert.True(getTracking.WasCalled);
        Assert.Equal(1, getTracking.LastArg);

        // Verify SaveUser was called
        Assert.True(saveTracking.WasCalled);

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
        var findAllTracking = stub.Find.OnCall((SearchRepoStub ko) =>
            new List<User>());
        var findByIdTracking = stub.Find.OnCall((SearchRepoStub ko, int id) =>
            new User { Id = id, Name = "ById" });
        var findByNameTracking = stub.Find.OnCall((SearchRepoStub ko, string name) =>
            new User { Id = 1, Name = name });

        ISearchRepo repo = stub;

        // Call each overload
        repo.Find();
        repo.Find(42);
        repo.Find("Alice");

        // Each tracking object is specific to its overload
        Assert.Equal(1, findAllTracking.CallCount);
        Assert.Equal(1, findByIdTracking.CallCount);
        Assert.Equal(42, findByIdTracking.LastArg);
        Assert.Equal(1, findByNameTracking.CallCount);
        Assert.Equal("Alice", findByNameTracking.LastArg);
    }
    #endregion
}
