using KnockOff.Documentation.Samples.Skills;

namespace KnockOff.Documentation.Samples.Tests.Skills;

/// <summary>
/// Tests for ~/.claude/skills/knockoff/moq-migration.md samples.
/// Verifies Moq migration code snippets compile and work.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Skills")]
public class MoqMigrationSamplesTests : SamplesTestBase
{
    // ========================================================================
    // skill:moq-migration:step1-create
    // ========================================================================

    [Fact]
    public void Step1_KnockOffCanBeCreated()
    {
        var knockOff = new MmUserServiceKnockOff();
        Assert.NotNull(knockOff);
    }

    // ========================================================================
    // skill:moq-migration:step2-object
    // ========================================================================

    [Fact]
    public void Step2_ImplicitCast()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;
        Assert.NotNull(service);
    }

    [Fact]
    public void Step2_ExplicitCast()
    {
        var knockOff = new MmUserServiceKnockOff();
        Assert.NotNull(knockOff.AsMmUserService());
    }

    // ========================================================================
    // skill:moq-migration:step3-setup
    // ========================================================================

    [Fact]
    public void Step3_SetupOnCall()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        knockOff.GetUser.OnCall = (ko, id) =>
            new MmUser { Id = id };

        var user = service.GetUser(42);

        Assert.Equal(42, user?.Id);
    }

    // ========================================================================
    // skill:moq-migration:step4-async
    // ========================================================================

    [Fact]
    public async Task Step4_AsyncSetup()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        knockOff.GetUserAsync.OnCall = (ko, id) =>
            Task.FromResult<MmUser?>(new MmUser { Id = id });

        var user = await service.GetUserAsync(42);

        Assert.Equal(42, user?.Id);
    }

    // ========================================================================
    // skill:moq-migration:step5-verify
    // ========================================================================

    [Fact]
    public void Step5_VerifyOnce()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        service.Save(new MmUser());

        Assert.Equal(1, knockOff.Save.CallCount);
    }

    [Fact]
    public void Step5_VerifyNever()
    {
        var knockOff = new MmUserServiceKnockOff();

        Assert.Equal(0, knockOff.Delete.CallCount);
    }

    [Fact]
    public void Step5_VerifyWasCalled()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        service.GetAll();

        Assert.True(knockOff.GetAll.WasCalled);
    }

    [Fact]
    public void Step5_VerifyExactly()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        service.Update(new MmUser());
        service.Update(new MmUser());
        service.Update(new MmUser());

        Assert.Equal(3, knockOff.Update.CallCount);
    }

    // ========================================================================
    // skill:moq-migration:step6-callback
    // ========================================================================

    [Fact]
    public void Step6_AutomaticTracking()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        var user = new MmUser { Name = "Test" };
        service.Save(user);

        Assert.Same(user, knockOff.Save.LastCallArg);
    }

    [Fact]
    public void Step6_CallbackForCustomCapture()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;
        var customList = new List<MmUser>();

        knockOff.Save.OnCall = (ko, user) =>
        {
            customList.Add(user);
        };

        service.Save(new MmUser { Name = "User1" });
        service.Save(new MmUser { Name = "User2" });

        Assert.Equal(2, customList.Count);
        Assert.Equal("User1", customList[0].Name);
        Assert.Equal("User2", customList[1].Name);
    }

    // ========================================================================
    // skill:moq-migration:static-returns
    // ========================================================================

    [Fact]
    public void StaticReturns_UserMethod()
    {
        var knockOff = new MmConfigServiceKnockOff();
        IMmConfigService service = knockOff;

        var config = service.GetConfig();

        Assert.Equal(30, config.Timeout);
    }

    // ========================================================================
    // skill:moq-migration:conditional-returns
    // ========================================================================

    [Fact]
    public void ConditionalReturns_SwitchExpression()
    {
        var knockOff = new MmUserServiceKnockOff();
        IMmUserService service = knockOff;

        knockOff.GetUser.OnCall = (ko, id) => id switch
        {
            1 => new MmUser { Name = "Admin" },
            2 => new MmUser { Name = "Guest" },
            _ => null
        };

        Assert.Equal("Admin", service.GetUser(1)?.Name);
        Assert.Equal("Guest", service.GetUser(2)?.Name);
        Assert.Null(service.GetUser(999));
    }

    // ========================================================================
    // skill:moq-migration:throwing-exceptions
    // ========================================================================

    [Fact]
    public void ThrowingExceptions_FromCallback()
    {
        var knockOff = new MmConnectionKnockOff();
        IMmConnectionService service = knockOff;

        knockOff.Connect.OnCall = (ko) =>
            throw new TimeoutException();

        Assert.Throws<TimeoutException>(() => service.Connect());
    }

    // ========================================================================
    // skill:moq-migration:sequential-returns
    // ========================================================================

    [Fact]
    public void SequentialReturns_Queue()
    {
        var knockOff = new MmSequenceKnockOff();
        IMmSequenceService service = knockOff;

        var results = new Queue<int>([1, 2, 3]);
        knockOff.GetNext.OnCall = (ko) => results.Dequeue();

        Assert.Equal(1, service.GetNext());
        Assert.Equal(2, service.GetNext());
        Assert.Equal(3, service.GetNext());
    }

    // ========================================================================
    // skill:moq-migration:property-setup
    // ========================================================================

    [Fact]
    public void PropertySetup_OnGet()
    {
        var knockOff = new MmPropServiceKnockOff();
        IMmPropService service = knockOff;

        knockOff.Name.OnGet = (ko) => "Test";

        Assert.Equal("Test", service.Name);
    }

    [Fact]
    public void PropertySetup_SetterTracking()
    {
        var knockOff = new MmPropServiceKnockOff();
        IMmPropService service = knockOff;

        service.Name = "Value";

        Assert.Equal("Value", knockOff.Name.LastSetValue);
    }

    // ========================================================================
    // skill:moq-migration:multiple-interfaces - Multi-interface tests removed (KO0010)
    // ========================================================================

    // ========================================================================
    // skill:moq-migration:argument-matching
    // ========================================================================

    [Fact]
    public void ArgumentMatching_ConditionalLogic()
    {
        var knockOff = new MmLoggerKnockOff();
        IMmLogger logger = knockOff;
        var errors = new List<string>();

        knockOff.Log.OnCall = (ko, message) =>
        {
            if (message.Contains("error"))
                errors.Add(message);
        };

        logger.Log("info: starting");
        logger.Log("error: something failed");
        logger.Log("info: continuing");
        logger.Log("error: another failure");

        Assert.Equal(2, errors.Count);
        Assert.Contains("something failed", errors[0]);
        Assert.Contains("another failure", errors[1]);
    }

    // ========================================================================
    // skill:moq-migration:method-overloads
    // ========================================================================

    [Fact]
    public void MethodOverloads_SeparateHandlers()
    {
        var knockOff = new MmProcessorKnockOff();
        IMmProcessorService service = knockOff;
        var oneParam = false;
        var twoParam = false;

        knockOff.Process1.OnCall = (ko, data) => oneParam = true;
        knockOff.Process2.OnCall = (ko, data, priority) => twoParam = true;

        service.Process("a");
        service.Process("b", 1);

        Assert.True(oneParam);
        Assert.True(twoParam);
    }

    [Fact]
    public void MethodOverloads_ReturnMethods()
    {
        var knockOff = new MmProcessorKnockOff();
        IMmProcessorService service = knockOff;

        knockOff.Calculate1.OnCall = (ko, value) => value * 2;
        knockOff.Calculate2.OnCall = (ko, a, b) => a + b;

        Assert.Equal(10, service.Calculate(5));
        Assert.Equal(8, service.Calculate(3, 5));
    }

    // ========================================================================
    // skill:moq-migration:out-params
    // ========================================================================

    [Fact]
    public void OutParams_ExplicitDelegateType()
    {
        var knockOff = new MmParserKnockOff();
        IMmParser parser = knockOff;

        knockOff.TryParse.OnCall =
            (MmParserKnockOff.TryParseInterceptor.TryParseDelegate)((MmParserKnockOff ko, string input, out int result) =>
            {
                return int.TryParse(input, out result);
            });

        Assert.True(parser.TryParse("42", out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void OutParams_TrackingOnlyInput()
    {
        var knockOff = new MmParserKnockOff();
        IMmParser parser = knockOff;

        knockOff.TryParse.OnCall =
            (MmParserKnockOff.TryParseInterceptor.TryParseDelegate)((MmParserKnockOff ko, string input, out int result) =>
            {
                return int.TryParse(input, out result);
            });

        parser.TryParse("42", out _);

        Assert.Equal("42", knockOff.TryParse.LastCallArg);
    }

    // ========================================================================
    // skill:moq-migration:ref-params
    // ========================================================================

    [Fact]
    public void RefParams_ExplicitDelegateType()
    {
        var knockOff = new MmRefProcessorKnockOff();
        IMmRefProcessor processor = knockOff;

        knockOff.Increment.OnCall =
            (MmRefProcessorKnockOff.IncrementInterceptor.IncrementDelegate)((MmRefProcessorKnockOff ko, ref int value) =>
            {
                value++;
            });

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(6, x);
    }

    [Fact]
    public void RefParams_TrackingCapturesOriginal()
    {
        var knockOff = new MmRefProcessorKnockOff();
        IMmRefProcessor processor = knockOff;

        knockOff.Increment.OnCall =
            (MmRefProcessorKnockOff.IncrementInterceptor.IncrementDelegate)((MmRefProcessorKnockOff ko, ref int value) =>
            {
                value++;
            });

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(6, x); // Modified
        Assert.Equal(5, knockOff.Increment.LastCallArg); // Original
    }
}
