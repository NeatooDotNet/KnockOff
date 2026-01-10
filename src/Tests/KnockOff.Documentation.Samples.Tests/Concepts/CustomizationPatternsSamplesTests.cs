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

        knockOff.DoSomething.OnCall = (ko) =>
        {
            callbackExecuted = true;
        };

        service.DoSomething();

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void CallbackMethod_ReturnMethod_ReturnsCallbackValue()
    {
        var knockOff = new PatternCallbackServiceKnockOff();
        IPatternCallbackService service = knockOff;

        knockOff.GetUser.OnCall = (ko, id) => new PatternUser { Id = id, Name = "Mocked" };

        var user = service.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Mocked", user.Name);
    }

    [Fact]
    public void CallbackMethod_MultipleParams_ReceivesAllParams()
    {
        var knockOff = new PatternCallbackServiceKnockOff();
        IPatternCallbackService service = knockOff;

        knockOff.Calculate.OnCall = (ko, name, value, flag) =>
        {
            return flag ? value * 2 : value;
        };

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
    // docs:customization-patterns:callback-indexer
    // ========================================================================

    [Fact]
    public void CallbackIndexer_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new PatternIndexerServiceKnockOff();
        IPatternIndexerService service = knockOff;

        knockOff.StringIndexer.OnGet = (ko, key) => key switch
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

        knockOff.StringIndexer.OnSet = (ko, key, value) =>
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
    public void Priority_WithCallback_OverridesUserMethod()
    {
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        // Set callback → overrides user method
        knockOff.Calculate2.OnCall = (ko, input) => input * 100;

        var result = service.Calculate(5);

        Assert.Equal(500, result); // callback
    }

    [Fact]
    public void Priority_AfterReset_ReturnsToUserMethod()
    {
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        knockOff.Calculate2.OnCall = (ko, input) => input * 100;
        Assert.Equal(500, service.Calculate(5)); // callback

        // Reset clears callback → back to user method
        knockOff.Calculate2.Reset();

        Assert.Equal(10, service.Calculate(5)); // user method
    }

    // ========================================================================
    // docs:customization-patterns:reset-behavior
    // ========================================================================

    [Fact]
    public void Reset_ClearsTrackingAndCallback()
    {
        var knockOff = new PatternUserServiceKnockOff();
        IPatternUserService service = knockOff;

        knockOff.GetUser2.OnCall = (ko, id) => new PatternUser { Name = "Callback" };
        service.GetUser(1);
        service.GetUser(2);

        Assert.Equal(2, knockOff.GetUser2.CallCount);

        // Reset
        knockOff.GetUser2.Reset();

        Assert.Equal(0, knockOff.GetUser2.CallCount);  // Tracking cleared

        // Now uses user method
        var user = service.GetUser(3);
        Assert.Equal("Default User", user.Name);
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
    public void CombiningPatterns_CallbackOverridesDefault()
    {
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;

        // Override for specific IDs
        knockOff.GetById2.OnCall = (ko, id) => id switch
        {
            1 => new PatternUser { Id = 1, Name = "Admin" },
            2 => new PatternUser { Id = 2, Name = "Guest" },
            _ => null  // Fall through to "not found"
        };

        Assert.Equal("Admin", repo.GetById(1)?.Name);
        Assert.Null(repo.GetById(999));  // Still null
    }

    [Fact]
    public void CombiningPatterns_ResetAndNewCallback()
    {
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;

        knockOff.GetById2.OnCall = (ko, id) => new PatternUser { Id = id, Name = "First" };
        Assert.Equal("First", repo.GetById(1)?.Name);

        // Reset and use different callback
        knockOff.GetById2.Reset();
        knockOff.GetById2.OnCall = (ko, id) =>
            new PatternUser { Id = id, Name = $"User-{id}" };

        Assert.Equal("User-999", repo.GetById(999)?.Name);
    }
}
