using KnockOff.Documentation.Samples.Reference;

namespace KnockOff.Documentation.Samples.Tests.Reference;

/// <summary>
/// Tests for docs/reference/generated-code.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Reference")]
public class GeneratedCodeSamplesTests
{
    [Fact]
    public void InputExample_UserMethod_Called()
    {
        var knockOff = new GenUserServiceKnockOff();
        IGenUserService service = knockOff;

        var user = service.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        // Interceptor has suffix due to user method collision
        Assert.True(knockOff.GetUser2.WasCalled);
    }

    [Fact]
    public void InputExample_Property_Works()
    {
        var knockOff = new GenUserServiceKnockOff();
        IGenUserService service = knockOff;

        service.Name = "Test";
        var name = service.Name;

        Assert.Equal("Test", name);
        Assert.Equal(1, knockOff.Name.SetCount);
        Assert.Equal(1, knockOff.Name.GetCount);
    }

    [Fact]
    public void MultipleParameters_LastCallArgs_TracksAll()
    {
        var knockOff = new GenLoggerKnockOff();
        IGenLogger logger = knockOff;

        var tracking = knockOff.Log.OnCall((ko, level, message, code) => { });
        logger.Log("ERROR", "Something failed", 500);

        var args = tracking.LastArgs;
        Assert.Equal("ERROR", args.level);
        Assert.Equal("Something failed", args.message);
        Assert.Equal(500, args.code);
    }

    [Fact]
    public void MultipleParameters_OnCall_ReceivesIndividualParams()
    {
        var knockOff = new GenLoggerKnockOff();
        IGenLogger logger = knockOff;

        string? capturedLevel = null;
        string? capturedMessage = null;
        int? capturedCode = null;

        knockOff.Log.OnCall((ko, level, message, code) =>
        {
            capturedLevel = level;
            capturedMessage = message;
            capturedCode = code;
        });

        logger.Log("WARN", "Low memory", 300);

        Assert.Equal("WARN", capturedLevel);
        Assert.Equal("Low memory", capturedMessage);
        Assert.Equal(300, capturedCode);
    }

    [Fact]
    public void SeparateStubs_BothWork()
    {
        var logger = new GenAuditLoggerKnockOff();
        var auditor = new GenAuditorKnockOff();

        IGenAuditLogger loggerService = logger;
        IGenAuditor auditorService = auditor;

        var logTracking = logger.Log.OnCall((ko, message) => { });
        var auditTracking = auditor.Audit.OnCall((ko, action) => { });

        loggerService.Log("test message");
        auditorService.Audit("test action");

        Assert.True(logTracking.WasCalled);
        Assert.True(auditTracking.WasCalled);
    }
}
