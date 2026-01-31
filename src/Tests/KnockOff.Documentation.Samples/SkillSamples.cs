using KnockOff;

namespace KnockOff.Documentation.Samples.Skill;

// =============================================================================
// Stand-Alone Pattern Samples
// =============================================================================

#region skill-standalone-pattern-define
public interface ISkillUserRepo
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class SkillUserRepoStub : ISkillUserRepo { }
#endregion

public class StandalonePatternTests
{
    #region skill-standalone-pattern-usage
    [Fact]
    public void StandaloneStub_ConfigureAndVerify()
    {
        // Create the stub
        var stub = new SkillUserRepoStub();

        // Configure behavior
        stub.GetById.OnCall((id) => new User { Id = id, Name = "Alice" }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable();

        // Use as interface
        ISkillUserRepo repo = stub;
        var user = repo.GetById(42);
        repo.Save(user!);

        // Verify calls
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Inline Interface Pattern Samples
// =============================================================================

public interface ISkillEmailSvc
{
    bool Send(string to, string subject);
}

#region skill-inline-interface-pattern-define
[KnockOff<ISkillEmailSvc>]
public partial class SkillEmailSvcTests
{
    // Generator creates Stubs.ISkillEmailSvc inside this class
}
#endregion

public partial class SkillEmailSvcTests
{
    #region skill-inline-interface-pattern-usage
    [Fact]
    public void InlineInterfaceStub_ConfigureAndVerify()
    {
        // Create stub from nested Stubs class
        var stub = new Stubs.ISkillEmailSvc();

        // Configure behavior
        stub.Send.OnCall((to, subject) => true).Verifiable();

        // Use as interface
        ISkillEmailSvc email = stub;
        var result = email.Send("test@example.com", "Hello");

        // Verify
        Assert.True(result);
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Inline Class Pattern Samples
// =============================================================================

#region skill-inline-class-pattern-define
public class SkillDataSvc
{
    public virtual string? GetData(int id) => null;
    public virtual bool IsConnected { get; set; }
}

[KnockOff<SkillDataSvc>]
public partial class SkillDataSvcTests
{
    // Generator creates Stubs.SkillDataSvc inside this class
}
#endregion

public partial class SkillDataSvcTests
{
    #region skill-inline-class-pattern-usage
    [Fact]
    public void InlineClassStub_UseObjectProperty()
    {
        // Create stub from nested Stubs class
        var stub = new Stubs.SkillDataSvc();

        // Configure behavior
        stub.GetData.OnCall((id) => $"Data-{id}").Verifiable();

        // Use .Object to get the actual class instance
        SkillDataSvc service = stub.Object;
        var data = service.GetData(42);

        // Verify
        Assert.Equal("Data-42", data);
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Method OnCall Examples
// =============================================================================

public interface ISkillConfigSvc
{
    string GetValue(string key);
    void SetValue(string key, string value);
}

[KnockOff]
public partial class SkillConfigSvcStub : ISkillConfigSvc { }

public class MethodOnCallTests
{
    [Fact]
    public void OnCall_ValueAndCallbackExamples()
    {
        var stub = new SkillConfigSvcStub();

        #region skill-method-oncall-examples
        // VALUE syntax - for fixed return values
        stub.GetValue.Returns("default-value");

        // CALLBACK syntax - for dynamic values based on arguments
        stub.GetValue.OnCall((key) => key == "debug" ? "true" : "false");

        // Void methods use Action callback
        stub.SetValue.OnCall((key, value) => { /* track or validate */ });
        #endregion
    }
}

// =============================================================================
// Property Configuration Examples
// =============================================================================

public interface ISkillAppConfig
{
    string ApiKey { get; set; }
    int Timeout { get; }
}

[KnockOff]
public partial class SkillAppConfigStub : ISkillAppConfig { }

public class PropertyConfigTests
{
    [Fact]
    public void Property_ConfigurationExamples()
    {
        var stub = new SkillAppConfigStub();

        #region skill-property-configuration-examples
        // OnGet with value - simplest syntax
        stub.Timeout.OnGet(30);

        // OnGet with callback - for computed values
        stub.ApiKey.OnGet(() => Environment.GetEnvironmentVariable("API_KEY") ?? "test-key");

        // OnSet - intercept property writes
        stub.ApiKey.OnSet((value) => { /* validate or track */ });
        #endregion
    }
}

// =============================================================================
// Verification Examples
// =============================================================================

public interface ISkillLogger
{
    void Log(string message);
    void LogError(string message);
}

[KnockOff]
public partial class SkillLoggerStub : ISkillLogger { }

public class VerificationTests
{
    [Fact]
    public void Verifiable_BatchVerification()
    {
        var stub = new SkillLoggerStub();

        #region skill-verifiable-batch
        // Mark methods as verifiable
        stub.Log.OnCall((msg) => { }).Verifiable();
        stub.LogError.OnCall((msg) => { }).Verifiable();

        ISkillLogger logger = stub;
        logger.Log("Starting");
        logger.LogError("Oops");

        // Single Verify() checks all marked members
        stub.Verify();
        #endregion
    }

    [Fact]
    public void Times_ConstraintExamples()
    {
        var stub = new SkillLoggerStub();

        #region skill-times-constraints
        // Verify specific call counts
        var tracking = stub.Log.OnCall((msg) => { });

        ISkillLogger logger = stub;
        logger.Log("First");
        logger.Log("Second");

        tracking.Verify(Times.Exactly(2));  // Exactly 2 calls
        tracking.Verify(Times.AtLeast(1));  // At least 1 call
        // Times.Once, Times.Never, Times.AtMost(n) also available
        #endregion
    }
}

// =============================================================================
// Argument Access Examples
// =============================================================================

public interface ISkillNotifier
{
    void Notify(int userId, string message);
}

[KnockOff]
public partial class SkillNotifierStub : ISkillNotifier { }

public class ArgumentAccessTests
{
    [Fact]
    public void Arguments_AccessFromTracking()
    {
        var stub = new SkillNotifierStub();

        #region skill-accessing-arguments
        var tracking = stub.Notify.OnCall((userId, message) => { });

        ISkillNotifier notifier = stub;
        notifier.Notify(42, "Hello");

        // Access arguments from tracking object
        var (userId, message) = tracking.LastArgs;
        Assert.Equal(42, userId);
        Assert.Equal("Hello", message);
        #endregion
    }
}

// =============================================================================
// Common Gotchas
// =============================================================================

// NOTE: These samples demonstrate CORRECT patterns.
// The gotcha descriptions in SKILL.md explain the problem;
// the code shows the solution.

public interface ISkillFoo
{
    void DoSomething();
}

#region skill-gotcha-missing-partial
// CORRECT: Include 'partial' keyword
[KnockOff]
public partial class SkillFooStub : ISkillFoo { }
#endregion

public interface ISkillBar
{
    void Process(int id, string name);
}

[KnockOff]
public partial class SkillBarStub : ISkillBar { }

public class GotchaTests
{
    [Fact]
    public void Gotcha_CorrectSignature()
    {
        var stub = new SkillBarStub();

        #region skill-gotcha-wrong-signature
        // CORRECT: Callback signature matches method parameters exactly
        stub.Process.OnCall((int id, string name) => { /* ... */ });
        #endregion
    }
}

public class SkillAbstractBase
{
    public virtual string GetName() => "Base";
}

[KnockOff<SkillAbstractBase>]
public partial class GotchaMissingObjectTests
{
    [Fact]
    public void Gotcha_UseObjectForClassStubs()
    {
        var stub = new Stubs.SkillAbstractBase();

        #region skill-gotcha-missing-object
        // CORRECT: Use .Object for inline class stubs
        SkillAbstractBase service = stub.Object;
        _ = service.GetName();
        #endregion
    }
}

public interface ISkillAsyncSvc
{
    Task<User?> GetUserAsync(int id);
}

[KnockOff]
public partial class SkillAsyncSvcStub : ISkillAsyncSvc { }

public class AsyncGotchaTests
{
    [Fact]
    public async Task Gotcha_AsyncAutoWrap()
    {
        var stub = new SkillAsyncSvcStub();

        #region skill-gotcha-async-autowrap
        // CORRECT: KnockOff auto-wraps - just pass the value directly
        stub.GetUserAsync.Returns(new User { Id = 1, Name = "Alice" });

        // No need for Task.FromResult with Returns()
        ISkillAsyncSvc service = stub;
        var user = await service.GetUserAsync(1);
        Assert.Equal("Alice", user!.Name);
        #endregion
    }
}
