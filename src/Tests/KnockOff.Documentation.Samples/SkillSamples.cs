using KnockOff;
using KnockOff.Documentation.Samples; // For User type

namespace KnockOff.Documentation.Samples.Skill;

// =============================================================================
// Stand-Alone Pattern Samples (matches SKILL.md)
// =============================================================================

public interface ISkillUserRepo
{
    User? GetById(int id);
    void Save(User user);
}

#region skill-standalone-pattern
[KnockOff]
public partial class SkillUserRepoStub : ISkillUserRepo { }
#endregion

public class StandalonePatternTests
{
    #region skill-standalone-usage
    [Fact]
    public void StandaloneStub_ConfigureAndVerify()
    {
        var stub = new SkillUserRepoStub();
        stub.GetById.Return((id) => new User { Id = id }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();
        ISkillUserRepo repo = stub;

        var user = repo.GetById(42);
        repo.Save(user!);

        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Inline Interface Pattern Samples (matches SKILL.md)
// =============================================================================

public interface ISkillEmailService
{
    bool Send(string to, string subject);
}

#region skill-inline-interface-pattern
[KnockOff<ISkillEmailService>]
public partial class SkillEmailTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.ISkillEmailService();
        stub.Send.Return((to, subj) => true).Verifiable();
        ISkillEmailService email = stub;
    }
}
#endregion

// =============================================================================
// Inline Class Pattern Samples (matches SKILL.md)
// =============================================================================

public class SkillDataServiceBase
{
    public virtual string? GetData(int id) => null;
}

#region skill-inline-class-pattern
[KnockOff<SkillDataServiceBase>]
public partial class SkillDataTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.SkillDataServiceBase();
        stub.GetData.Return((id) => "test").Verifiable();
        SkillDataServiceBase service = stub.Object;  // Use .Object!
    }
}
#endregion

// =============================================================================
// Inline Delegate Pattern Samples (matches SKILL.md)
// =============================================================================

public delegate bool SkillValidationRule(string value);

#region skill-inline-delegate-pattern
[KnockOff<SkillValidationRule>]  // delegate bool SkillValidationRule(string value);
public partial class SkillValidationTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.SkillValidationRule();
        stub.Interceptor.Return((val) => val != "invalid");
        SkillValidationRule rule = stub;  // Implicit conversion
    }
}
#endregion

// =============================================================================
// Method Return Examples
// =============================================================================

public interface ISkillConfigSvc
{
    string GetValue(string key);
    void SetValue(string key, string value);
}

[KnockOff]
public partial class SkillConfigSvcStub : ISkillConfigSvc { }

public class MethodReturnTests
{
    [Fact]
    public void Return_ValueAndCallbackExamples()
    {
        var stub = new SkillConfigSvcStub();

        // VALUE syntax - for fixed return values
        stub.GetValue.Return("default-value");

        // CALLBACK syntax - for dynamic values based on arguments
        stub.GetValue.Return((key) => key == "debug" ? "true" : "false");

        // Void methods use Action callback
        stub.SetValue.Call((key, value) => { /* track or validate */ });
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

        // Get with value - simplest syntax
        stub.Timeout.Get(30);

        // Get with callback - for computed values
        stub.ApiKey.Get(() => Environment.GetEnvironmentVariable("API_KEY") ?? "test-key");

        // Set - intercept property writes
        stub.ApiKey.Set((value) => { /* validate or track */ });
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

public class SkillVerificationTests
{
    [Fact]
    public void Verifiable_BatchVerification()
    {
        var stub = new SkillLoggerStub();

        // Mark methods as verifiable
        stub.Log.Call((msg) => { }).Verifiable();
        stub.LogError.Call((msg) => { }).Verifiable();

        ISkillLogger logger = stub;
        logger.Log("Starting");
        logger.LogError("Oops");

        // Single Verify() checks all marked members
        stub.Verify();
    }

    [Fact]
    public void Times_ConstraintExamples()
    {
        var stub = new SkillLoggerStub();

        // Verify specific call counts
        var tracking = stub.Log.Call((msg) => { });

        ISkillLogger logger = stub;
        logger.Log("First");
        logger.Log("Second");

        tracking.Verify(Times.Exactly(2));  // Exactly 2 calls
        tracking.Verify(Times.AtLeast(1));  // At least 1 call
        // Times.Once, Times.Never, Times.AtMost(n) also available
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

        var tracking = stub.Notify.Call((userId, message) => { });

        ISkillNotifier notifier = stub;
        notifier.Notify(42, "Hello");

        // Access arguments from tracking object
        var (userId, message) = tracking.LastArgs;
        Assert.Equal(42, userId);
        Assert.Equal("Hello", message);
    }
}

// =============================================================================
// Common Gotchas - Missing Partial
// =============================================================================

public interface ISkillFoo
{
    void DoSomething();
}

// CORRECT: Include 'partial' keyword
[KnockOff]
public partial class SkillFooStub : ISkillFoo { }

// =============================================================================
// Common Gotchas - Wrong Signature
// =============================================================================

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

        // CORRECT: Callback signature matches method parameters exactly
        stub.Process.Call((int id, string name) => { /* ... */ });
    }
}

// =============================================================================
// Common Gotchas - Missing .Object
// =============================================================================

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

        // CORRECT: Use .Object for inline class stubs
        SkillAbstractBase service = stub.Object;
        _ = service.GetName();
    }
}

// =============================================================================
// Common Gotchas - Async Auto-Wrap
// =============================================================================

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

        // CORRECT: KnockOff auto-wraps - just pass the value directly
        stub.GetUserAsync.Return(new User { Id = 1, Name = "Alice" });

        // No need for Task.FromResult with Return()
        ISkillAsyncSvc service = stub;
        var user = await service.GetUserAsync(1);
        Assert.Equal("Alice", user!.Name);
    }
}
