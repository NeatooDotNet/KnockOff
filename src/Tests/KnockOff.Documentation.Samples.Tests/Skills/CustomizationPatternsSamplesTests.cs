using KnockOff.Documentation.Samples.Skills;

namespace KnockOff.Documentation.Samples.Tests.Skills;

/// <summary>
/// Tests for ~/.claude/skills/knockoff/customization-patterns.md samples.
/// Verifies customization pattern code snippets compile and work.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Skills")]
public class CustomizationPatternsSamplesTests : SamplesTestBase
{
    // ========================================================================
    // skill:customization-patterns:user-method-basic
    // ========================================================================

    [Fact]
    public void UserMethodBasic_Add_ReturnsSum()
    {
        var knockOff = new CpCalculatorKnockOff();
        ICpCalculator calc = knockOff;

        Assert.Equal(5, calc.Add(2, 3));
    }

    [Fact]
    public void UserMethodBasic_Divide_ReturnsQuotient()
    {
        var knockOff = new CpCalculatorKnockOff();
        ICpCalculator calc = knockOff;

        Assert.Equal(2.5, calc.Divide(5, 2));
    }

    [Fact]
    public void UserMethodBasic_Divide_HandlesZeroDivisor()
    {
        var knockOff = new CpCalculatorKnockOff();
        ICpCalculator calc = knockOff;

        Assert.Equal(0, calc.Divide(5, 0));
    }

    // ========================================================================
    // skill:customization-patterns:user-method-async
    // ========================================================================

    [Fact]
    public async Task UserMethodAsync_GetByIdAsync_ReturnsUser()
    {
        var knockOff = new CpRepoKnockOff();
        ICpRepository repo = knockOff;

        var user = await repo.GetByIdAsync(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    [Fact]
    public async Task UserMethodAsync_CountAsync_ReturnsValue()
    {
        var knockOff = new CpRepoKnockOff();
        ICpRepository repo = knockOff;

        var count = await repo.CountAsync();

        Assert.Equal(100, count);
    }

    // ========================================================================
    // skill:customization-patterns:callback-method
    // ========================================================================

    [Fact]
    public void CallbackMethod_VoidNoParams_ExecutesCallback()
    {
        var knockOff = new CpCallbackServiceKnockOff();
        ICpCallbackService service = knockOff;
        var called = false;

        knockOff.Initialize.OnCall((ko) => { called = true; });
        service.Initialize();

        Assert.True(called);
    }

    [Fact]
    public void CallbackMethod_SingleParam_ReturnsValue()
    {
        var knockOff = new CpCallbackServiceKnockOff();
        ICpCallbackService service = knockOff;

        knockOff.GetById.OnCall((ko, id) =>
            new CpUser { Id = id, Name = "Mocked" });

        var user = service.GetById(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Mocked", user.Name);
    }

    [Fact]
    public void CallbackMethod_MultipleParams_ReceivesAll()
    {
        var knockOff = new CpCallbackServiceKnockOff();
        ICpCallbackService service = knockOff;

        knockOff.Search.OnCall((ko, query, limit, offset) =>
            new List<CpUser> { new() { Name = $"{query}:{limit}:{offset}" } });

        var result = service.Search("test", 10, 5);

        Assert.Single(result);
        Assert.Equal("test:10:5", result[0].Name);
    }

    // ========================================================================
    // skill:customization-patterns:callback-property
    // ========================================================================

    [Fact]
    public void CallbackProperty_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new CpPropertyServiceKnockOff();
        ICpPropertyService service = knockOff;

        knockOff.CurrentUser.OnGet = (ko) =>
            new CpUser { Name = "TestUser" };

        Assert.Equal("TestUser", service.CurrentUser?.Name);
    }

    [Fact]
    public void CallbackProperty_OnSet_CapturesValue()
    {
        var knockOff = new CpPropertyServiceKnockOff();
        ICpPropertyService service = knockOff;
        CpUser? capturedUser = null;

        knockOff.CurrentUser.OnSet = (ko, value) =>
        {
            capturedUser = value;
        };

        var user = new CpUser { Name = "CapturedUser" };
        service.CurrentUser = user;

        Assert.NotNull(capturedUser);
        Assert.Equal("CapturedUser", capturedUser.Name);
    }

    // ========================================================================
    // skill:customization-patterns:callback-indexer
    // ========================================================================

    [Fact]
    public void CallbackIndexer_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new CpPropertyStoreKnockOff();
        ICpPropertyStore store = knockOff;

        knockOff.Indexer.OnGet = (ko, key) => key switch
        {
            "admin" => "AdminConfig",
            "guest" => "GuestConfig",
            _ => null
        };

        Assert.Equal("AdminConfig", store["admin"]);
        Assert.Equal("GuestConfig", store["guest"]);
        Assert.Null(store["unknown"]);
    }

    [Fact]
    public void CallbackIndexer_OnSet_InterceptorsValue()
    {
        var knockOff = new CpPropertyStoreKnockOff();
        ICpPropertyStore store = knockOff;
        (string key, object? value)? captured = null;

        knockOff.Indexer.OnSet = (ko, key, value) =>
        {
            captured = (key, value);
        };

        store["testKey"] = "testValue";

        Assert.Equal("testKey", captured?.key);
        Assert.Equal("testValue", captured?.value);
    }

    // ========================================================================
    // skill:customization-patterns:callback-overloads
    // ========================================================================

    [Fact]
    public void CallbackOverloads_SeparateHandlers()
    {
        var knockOff = new CpOverloadServiceKnockOff();
        ICpOverloadService service = knockOff;
        var oneParam = false;
        var twoParam = false;

        // Overloaded methods use single interceptor with overloaded OnCall methods
        // The compiler resolves the overload based on delegate parameter types
        knockOff.Process.OnCall((CpOverloadServiceKnockOff ko, string data) => { oneParam = true; });
        knockOff.Process.OnCall((CpOverloadServiceKnockOff ko, string data, int priority) => { twoParam = true; });

        service.Process("a");
        service.Process("b", 1);

        Assert.True(oneParam);
        Assert.True(twoParam);
    }

    [Fact]
    public void CallbackOverloads_ReturnMethods()
    {
        var knockOff = new CpOverloadServiceKnockOff();
        ICpOverloadService service = knockOff;

        // Overloaded methods use single interceptor with overloaded OnCall methods
        knockOff.Calculate.OnCall((CpOverloadServiceKnockOff ko, int value) => value * 2);
        knockOff.Calculate.OnCall((CpOverloadServiceKnockOff ko, int a, int b) => a + b);

        Assert.Equal(10, service.Calculate(5));
        Assert.Equal(8, service.Calculate(3, 5));
    }

    // ========================================================================
    // skill:customization-patterns:callback-out-params
    // ========================================================================

    [Fact]
    public void CallbackOutParams_TryParse()
    {
        var knockOff = new CpParserKnockOff();
        ICpParser parser = knockOff;

        // OnCall is a method that takes a delegate parameter
        knockOff.TryParse.OnCall(
            (CpParserKnockOff ko, string input, out int result) =>
            {
                if (int.TryParse(input, out result))
                    return true;
                result = 0;
                return false;
            });

        Assert.True(parser.TryParse("42", out var value));
        Assert.Equal(42, value);

        Assert.False(parser.TryParse("invalid", out var invalid));
        Assert.Equal(0, invalid);
    }

    [Fact]
    public void CallbackOutParams_MultipleOuts()
    {
        var knockOff = new CpParserKnockOff();
        ICpParser parser = knockOff;

        // OnCall is a method that takes a delegate parameter
        knockOff.GetStats.OnCall(
            (CpParserKnockOff ko, out int count, out double average) =>
            {
                count = 42;
                average = 3.14;
            });

        parser.GetStats(out var c, out var a);

        Assert.Equal(42, c);
        Assert.Equal(3.14, a);
    }

    // ========================================================================
    // skill:customization-patterns:callback-ref-params
    // ========================================================================

    [Fact]
    public void CallbackRefParams_Increment()
    {
        var knockOff = new CpProcessorKnockOff();
        ICpProcessor processor = knockOff;

        // OnCall is a method that takes a delegate parameter
        knockOff.Increment.OnCall(
            (CpProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            });

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x);
    }

    [Fact]
    public void CallbackRefParams_TryUpdate()
    {
        var knockOff = new CpProcessorKnockOff();
        ICpProcessor processor = knockOff;

        // OnCall is a method that takes a delegate parameter
        knockOff.TryUpdate.OnCall(
            (CpProcessorKnockOff ko, string key, ref string value) =>
            {
                if (key == "valid")
                {
                    value = value.ToUpper();
                    return true;
                }
                return false;
            });

        string val1 = "test";
        Assert.True(processor.TryUpdate("valid", ref val1));
        Assert.Equal("TEST", val1);

        string val2 = "test";
        Assert.False(processor.TryUpdate("invalid", ref val2));
        Assert.Equal("test", val2);
    }

    [Fact]
    public void CallbackRefParams_TrackingCapturesOriginal()
    {
        var knockOff = new CpProcessorKnockOff();
        ICpProcessor processor = knockOff;

        // OnCall returns IMethodTracking<T> for tracking
        var tracking = knockOff.Increment.OnCall(
            (CpProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            });

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x); // Modified
        Assert.Equal(5, tracking.LastArg); // Original value captured before modification
    }

    // ========================================================================
    // skill:customization-patterns:priority-in-action
    // ========================================================================

    [Fact]
    public void Priority_UserMethod_ReturnsExpectedValue()
    {
        var knockOff = new CpPriorityServiceKnockOff();
        ICpPriorityService service = knockOff;

        // User method implementation doubles the value
        Assert.Equal(10, service.Calculate(5)); // 5 * 2
    }

    [Fact]
    public void Priority_UserMethodInterceptor_TracksCallsWithoutOverriding()
    {
        var knockOff = new CpPriorityServiceKnockOff();
        ICpPriorityService service = knockOff;

        // User method interceptors (suffix "2") only track calls - they cannot override behavior
        service.Calculate(5);

        Assert.True(knockOff.Calculate2.WasCalled);
        Assert.Equal(1, knockOff.Calculate2.CallCount);
        Assert.Equal(5, knockOff.Calculate2.LastArg);
    }

    [Fact]
    public void Priority_Reset_ClearsTrackingState()
    {
        var knockOff = new CpPriorityServiceKnockOff();
        ICpPriorityService service = knockOff;

        service.Calculate(5);
        Assert.Equal(1, knockOff.Calculate2.CallCount);

        knockOff.Calculate2.Reset();

        // Reset clears tracking but behavior stays the same (user method)
        Assert.Equal(0, knockOff.Calculate2.CallCount);
        Assert.Equal(10, service.Calculate(5)); // User method still works
    }

    // ========================================================================
    // skill:customization-patterns:reset-behavior
    // ========================================================================

    [Fact]
    public void Reset_ClearsTracking()
    {
        var knockOff = new CpResetRepositoryKnockOff();
        ICpResetRepository service = knockOff;
        var specialUser = new CpUser { Name = "Special" };

        // OnCall is a method that returns tracking
        var tracking = knockOff.GetUser.OnCall((ko, id) => specialUser);
        service.GetUser(1);
        service.GetUser(2);

        Assert.Equal(2, tracking.CallCount);

        knockOff.GetUser.Reset();

        // After reset, need to set up a new callback to get tracking
        var newTracking = knockOff.GetUser.OnCall((ko, id) => specialUser);
        Assert.Equal(0, newTracking.CallCount);
    }

    [Fact]
    public void Reset_ClearsTrackingButKeepsCallback()
    {
        var knockOff = new CpResetRepositoryKnockOff();
        ICpResetRepository service = knockOff;

        // OnCall is a method that returns tracking
        var tracking = knockOff.GetUser.OnCall((ko, id) => new CpUser { Name = "FromCallback" });
        service.GetUser(1);

        Assert.Equal(1, tracking.CallCount);
        Assert.Equal("FromCallback", service.GetUser(1)?.Name);

        knockOff.GetUser.Reset();

        // Tracking is cleared, but callback remains active
        // Need to set up new callback to get fresh tracking object
        var newTracking = knockOff.GetUser.OnCall((ko, id) => new CpUser { Name = "NewCallback" });
        Assert.Equal(0, newTracking.CallCount);

        // New callback is now active
        Assert.Equal("NewCallback", service.GetUser(1)?.Name);
        Assert.Equal(1, newTracking.CallCount);
    }

    // ========================================================================
    // skill:customization-patterns:combining-patterns
    // ========================================================================

    [Fact]
    public void CombiningPatterns_UserMethod_ReturnsExpectedValue()
    {
        var knockOff = new CpCombinedRepoKnockOff();
        ICpCombinedRepository repo = knockOff;

        // User method returns null for non-existent IDs
        Assert.Null(repo.GetById(999));
    }

    [Fact]
    public void CombiningPatterns_TracksAllCalls()
    {
        var knockOff = new CpCombinedRepoKnockOff();
        ICpCombinedRepository repo = knockOff;

        // User method interceptors (suffix "2") track calls to user methods
        repo.GetById(1);
        repo.GetById(999);

        Assert.True(knockOff.GetById2.WasCalled);
        Assert.Equal(2, knockOff.GetById2.CallCount);
        Assert.Equal(999, knockOff.GetById2.LastArg);
    }

    [Fact]
    public void CombiningPatterns_Reset_ClearsTracking()
    {
        var knockOff = new CpCombinedRepoKnockOff();
        ICpCombinedRepository repo = knockOff;

        repo.GetById(1);
        Assert.Equal(1, knockOff.GetById2.CallCount);

        knockOff.GetById2.Reset();

        // Reset clears tracking state
        Assert.Equal(0, knockOff.GetById2.CallCount);
        Assert.False(knockOff.GetById2.WasCalled);
    }
}
