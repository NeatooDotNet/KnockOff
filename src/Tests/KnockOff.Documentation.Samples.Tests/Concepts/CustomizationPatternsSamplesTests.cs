using KnockOff.Documentation.Samples.Concepts;

namespace KnockOff.Documentation.Samples.Tests.Concepts;

/// <summary>
/// Tests for docs/concepts/customization-patterns.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Concepts")]
public class CustomizationPatternsSamplesTests : SamplesTestBase
{
    // ========================================================================
    // docs:customization-patterns:user-method-basic
    // ========================================================================

    [Fact]
    public void UserMethod_GetUser_ReturnsDefaultUser()
    {
        var knockOff = new PatternUserServiceKnockOff();
        IPatternUserService service = knockOff;

        var user = service.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Default User", user.Name);
    }

    [Fact]
    public void UserMethod_CalculateScore_DoublesBaseScore()
    {
        var knockOff = new PatternUserServiceKnockOff();
        IPatternUserService service = knockOff;

        var score = service.CalculateScore("test", 50);

        Assert.Equal(100, score);
    }

    // ========================================================================
    // docs:customization-patterns:user-method-async
    // ========================================================================

    [Fact]
    public async Task UserMethodAsync_GetByIdAsync_ReturnsUser()
    {
        var knockOff = new PatternRepositoryKnockOff();
        IPatternRepository repo = knockOff;

        var user = await repo.GetByIdAsync(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    [Fact]
    public async Task UserMethodAsync_CountAsync_Returns42()
    {
        var knockOff = new PatternRepositoryKnockOff();
        IPatternRepository repo = knockOff;

        var count = await repo.CountAsync();

        Assert.Equal(42, count);
    }

    // ========================================================================
    // docs:customization-patterns:callback-method
    // ========================================================================

    [Fact]
    public void CallbackMethod_VoidMethod_ExecutesCallback()
    {
        var knockOff = new PatternCallbackServiceKnockOff();
        IPatternCallbackService service = knockOff;
        var callbackExecuted = false;

        // OnCall is a method for standalone stubs
        knockOff.DoSomething.OnCall((ko) =>
        {
            callbackExecuted = true;
        });

        service.DoSomething();

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void CallbackMethod_ReturnMethod_ReturnsCallbackValue()
    {
        var knockOff = new PatternCallbackServiceKnockOff();
        IPatternCallbackService service = knockOff;

        // OnCall is a method for standalone stubs
        knockOff.GetUser.OnCall((ko, id) => new PatternUser { Id = id, Name = "Mocked" });

        var user = service.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Mocked", user.Name);
    }

    [Fact]
    public void CallbackMethod_MultipleParams_ReceivesAllParams()
    {
        var knockOff = new PatternCallbackServiceKnockOff();
        IPatternCallbackService service = knockOff;

        // OnCall is a method for standalone stubs
        knockOff.Calculate.OnCall((ko, name, value, flag) =>
        {
            return flag ? value * 2 : value;
        });

        Assert.Equal(100, service.Calculate("test", 50, true));
        Assert.Equal(50, service.Calculate("test", 50, false));
    }

    // ========================================================================
    // docs:customization-patterns:callback-property
    // ========================================================================

    [Fact]
    public void CallbackProperty_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new PatternPropertyServiceKnockOff();
        IPatternPropertyService service = knockOff;

        knockOff.Name.OnGet = (ko) => "Dynamic Value";

        Assert.Equal("Dynamic Value", service.Name);
    }

    [Fact]
    public void CallbackProperty_OnSet_ExecutesCallback()
    {
        var knockOff = new PatternPropertyServiceKnockOff();
        IPatternPropertyService service = knockOff;
        string? capturedValue = null;

        knockOff.Name.OnSet = (ko, value) =>
        {
            capturedValue = value;
        };

        service.Name = "Test Value";

        Assert.Equal("Test Value", capturedValue);
    }

    // ========================================================================
    // docs:customization-patterns:callback-ko-access
    // ========================================================================

    [Fact]
    public void CallbackKoAccess_CanAccessOtherInterceptors()
    {
        var knockOff = new PatternKoAccessServiceKnockOff();
        IPatternKoAccessService service = knockOff;

        // Configure Initialize with empty callback to track calls
        var initTracking = knockOff.Initialize.OnCall((ko) => { });

        // OnCall is a method for standalone stubs
        knockOff.GetUser.OnCall((ko, id) =>
        {
            // Access tracking from captured variable
            if (initTracking.WasCalled)
                return new PatternUser { Id = id, Name = "Initialized" };

            return new PatternUser { Id = id, Name = "Not Initialized" };
        });

        // Before Initialize is called
        var user1 = service.GetUser(1);
        Assert.Equal("Not Initialized", user1.Name);

        // After Initialize is called
        service.Initialize();
        var user2 = service.GetUser(2);
        Assert.Equal("Initialized", user2.Name);
    }

    [Fact]
    public void CallbackKoAccess_CanAccessBackingFields()
    {
        var knockOff = new PatternKoAccessServiceKnockOff();
        IPatternKoAccessService service = knockOff;

        // Set up backing field value
        knockOff.Name.Value = "Test Name";

        // OnCall is a method for standalone stubs
        knockOff.GetUser.OnCall((ko, id) =>
        {
            // Access backing fields via interceptor
            return new PatternUser { Id = id, Name = ko.Name.Value };
        });

        var user = service.GetUser(1);
        Assert.Equal("Test Name", user.Name);
    }

    // ========================================================================
    // docs:customization-patterns:callback-indexer
    // ========================================================================

    [Fact]
    public void CallbackIndexer_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new PatternIndexerServiceKnockOff();
        IPatternIndexerService service = knockOff;

        knockOff.Indexer.OnGet = (ko, key) => key switch
        {
            "Name" => new PatternPropertyInfo { Value = "Test" },
            "Age" => new PatternPropertyInfo { Value = "25" },
            _ => null
        };

        Assert.Equal("Test", service["Name"]?.Value);
        Assert.Equal("25", service["Age"]?.Value);
        Assert.Null(service["Unknown"]);
    }

    [Fact]
    public void CallbackIndexer_OnSet_ExecutesCallback()
    {
        var knockOff = new PatternIndexerServiceKnockOff();
        IPatternIndexerService service = knockOff;
        (string key, PatternPropertyInfo? value)? captured = null;

        knockOff.Indexer.OnSet = (ko, key, value) =>
        {
            captured = (key, value);
        };

        service["Name"] = new PatternPropertyInfo { Value = "Test" };

        Assert.NotNull(captured);
        Assert.Equal("Name", captured.Value.key);
        Assert.Equal("Test", captured.Value.value?.Value);
    }

    // ========================================================================
    // docs:customization-patterns:priority-example
    // ========================================================================

    [Fact]
    public void Priority_NoCallback_UsesUserMethod()
    {
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        // No callback set → uses user method
        var result = service.Calculate(5);

        Assert.Equal(10, result); // 5 * 2
    }

    [Fact]
    public void Priority_UserMethod_TrackedViaInterceptor()
    {
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        // User method interceptors provide tracking, not callbacks
        var result = service.Calculate(5);

        Assert.Equal(10, result); // user method: 5 * 2
        Assert.True(knockOff.Calculate2.WasCalled);
        Assert.Equal(5, knockOff.Calculate2.LastArg);
    }

    [Fact]
    public void Priority_AfterReset_TrackingCleared()
    {
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        service.Calculate(5);
        Assert.Equal(1, knockOff.Calculate2.CallCount);

        // Reset clears tracking
        knockOff.Calculate2.Reset();

        Assert.Equal(0, knockOff.Calculate2.CallCount);
        Assert.False(knockOff.Calculate2.WasCalled);
    }

    // ========================================================================
    // docs:customization-patterns:reset-behavior
    // ========================================================================

    [Fact]
    public void Reset_ClearsTracking()
    {
        var knockOff = new PatternUserServiceKnockOff();
        IPatternUserService service = knockOff;

        // User method interceptors provide tracking only
        service.GetUser(1);
        service.GetUser(2);

        Assert.Equal(2, knockOff.GetUser2.CallCount);

        // Reset clears tracking
        knockOff.GetUser2.Reset();

        Assert.Equal(0, knockOff.GetUser2.CallCount);  // Tracking cleared

        // User method continues to provide behavior
        var user = service.GetUser(3);
        Assert.Equal("Default User", user.Name);
        Assert.Equal(1, knockOff.GetUser2.CallCount);  // New call tracked
    }

    // ========================================================================
    // docs:customization-patterns:combining-patterns
    // ========================================================================

    [Fact]
    public void CombiningPatterns_DefaultIsNull()
    {
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;

        // Uses default (null)
        Assert.Null(repo.GetById(999));
    }

    [Fact]
    public void CombiningPatterns_UserMethodTracking()
    {
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;

        // User method interceptors provide tracking
        repo.GetById(1);
        repo.GetById(2);

        Assert.Equal(2, knockOff.GetById2.CallCount);
        Assert.Equal(2, knockOff.GetById2.LastArg);  // Last ID called
    }

    [Fact]
    public void CombiningPatterns_ResetClearsTracking()
    {
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;

        repo.GetById(1);
        Assert.Equal(1, knockOff.GetById2.CallCount);

        // Reset clears tracking
        knockOff.GetById2.Reset();

        Assert.Equal(0, knockOff.GetById2.CallCount);
        Assert.False(knockOff.GetById2.WasCalled);

        // User method still works
        Assert.Null(repo.GetById(999));  // null from user method
        Assert.True(knockOff.GetById2.WasCalled);  // New call tracked
    }
}
