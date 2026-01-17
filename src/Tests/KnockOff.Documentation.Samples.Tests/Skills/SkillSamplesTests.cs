using KnockOff.Documentation.Samples.Skills;

namespace KnockOff.Documentation.Samples.Tests.Skills;

/// <summary>
/// Tests for ~/.claude/skills/knockoff/SKILL.md samples.
/// Verifies skill code snippets compile and work.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Skills")]
public class SkillSamplesTests : SamplesTestBase
{
    // ========================================================================
    // skill:SKILL:duality-pattern
    // ========================================================================

    [Fact]
    public void DualityPattern_UserMethod_ReturnsDouble()
    {
        var knockOff = new SkServiceKnockOff();
        ISkService service = knockOff;

        Assert.Equal(10, service.GetValue(5));
    }

    [Fact]
    public void DualityPattern_TrackingAvailable()
    {
        var knockOff = new SkServiceKnockOff();
        ISkService service = knockOff;

        service.GetValue(5);

        Assert.True(knockOff.GetValue2.WasCalled);
        Assert.Equal(5, knockOff.GetValue2.LastArg);
    }

    // ========================================================================
    // skill:SKILL:quick-start-stub
    // ========================================================================

    [Fact]
    public void QuickStart_StubCanBeCreated()
    {
        var knockOff = new SkDataServiceKnockOff();
        Assert.NotNull(knockOff);
    }

    [Fact]
    public void QuickStart_UserMethod_ReturnsConfiguredValue()
    {
        var knockOff = new SkDataServiceKnockOff(count: 100);
        ISkDataService service = knockOff;

        Assert.Equal(100, service.GetCount());
    }

    [Fact]
    public void QuickStart_NullableMethod_ReturnsNullByDefault()
    {
        var knockOff = new SkDataServiceKnockOff();
        ISkDataService service = knockOff;

        Assert.Null(service.GetDescription(1));
    }

    // ========================================================================
    // skill:SKILL:quick-start-usage
    // ========================================================================

    [Fact]
    public void QuickStart_Usage_DemonstratesAllFeatures()
    {
        #region skill-SKILL-quick-start-usage
        var knockOff = new SkDataServiceKnockOff(count: 100);
        ISkDataService service = knockOff;

        // Property - uses generated backing field
        service.Name = "Test";
        Assert.Equal("Test", service.Name);
        Assert.Equal(1, knockOff.Name.SetCount);

        // Nullable method - configure and track with OnCall
        var tracking = knockOff.GetDescription.OnCall((ko, id) => null);
        var description = service.GetDescription(5);
        Assert.Null(description);
        Assert.True(tracking.WasCalled);
        Assert.Equal(5, tracking.LastArg);

        // Non-nullable method - returns constructor value
        Assert.Equal(100, service.GetCount());
        #endregion
    }

    // ========================================================================
    // skill:SKILL:interface-access - Multi-interface tests removed (KO0010)
    // ========================================================================

    // ========================================================================
    // skill:SKILL:multiple-interfaces - Multi-interface tests removed (KO0010)
    // ========================================================================

    // ========================================================================
    // skill:SKILL:oncall-patterns
    // ========================================================================

    [Fact]
    public void OnCallPatterns_NoParams_Works()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;
        var called = false;

        knockOff.Clear.OnCall((ko) => { called = true; });
        service.Clear();

        Assert.True(called);
    }

    [Fact]
    public void OnCallPatterns_SingleParam_ReceivesArg()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;

        knockOff.GetById.OnCall((ko, id) => new SkUser { Id = id });

        var user = service.GetById(42);

        Assert.Equal(42, user.Id);
    }

    [Fact]
    public void OnCallPatterns_MultipleParams_ReceivesAll()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;

        knockOff.Find.OnCall((ko, name, active) =>
            new List<SkUser> { new() { Name = name } });

        var result = service.Find("Test", true);

        Assert.Single(result);
        Assert.Equal("Test", result[0].Name);
    }

    // ========================================================================
    // skill:SKILL:smart-defaults
    // ========================================================================

    [Fact]
    public void SmartDefaults_ValueType_ReturnsDefault()
    {
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        Assert.Equal(0, service.GetCount());
    }

    [Fact]
    public void SmartDefaults_ConcreteList_ReturnsEmptyList()
    {
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        var items = service.GetItems();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void SmartDefaults_ReturnsEmptyList()
    {
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        var list = service.GetIList();

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void SmartDefaults_NullableRef_ReturnsNull()
    {
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        Assert.Null(service.GetOptional());
    }

    [Fact]
    public void SmartDefaults_Interface_Throws()
    {
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        Assert.Throws<InvalidOperationException>(() => service.GetDisposable());
    }

    // ========================================================================
    // skill:SKILL:customization-user-method
    // ========================================================================

    [Fact]
    public void CustomizationUserMethod_ReturnsValue()
    {
        var knockOff = new SkRepoKnockOff();
        ISkRepoService service = knockOff;

        var user = service.GetById(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    [Fact]
    public async Task CustomizationUserMethodAsync_ReturnsValue()
    {
        var knockOff = new SkRepoKnockOff();
        ISkRepoService service = knockOff;

        var user = await service.GetByIdAsync(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    // ========================================================================
    // skill:SKILL:priority-order
    // ========================================================================

    [Fact]
    public void Priority_NoCallback_UsesUserMethod()
    {
        var knockOff = new SkPriorityServiceKnockOff();
        ISkPriorityService service = knockOff;

        Assert.Equal(10, service.Calculate(5));
    }

    [Fact]
    public void Priority_TrackingMultipleCalls()
    {
        var knockOff = new SkPriorityServiceKnockOff();
        ISkPriorityService service = knockOff;

        service.Calculate(5);
        service.Calculate(10);

        Assert.Equal(2, knockOff.Calculate2.CallCount);
        Assert.Equal(10, knockOff.Calculate2.LastArg);
    }

    [Fact]
    public void Priority_AfterReset_ClearsTracking()
    {
        var knockOff = new SkPriorityServiceKnockOff();
        ISkPriorityService service = knockOff;

        service.Calculate(5);
        Assert.Equal(1, knockOff.Calculate2.CallCount);

        knockOff.Calculate2.Reset();

        Assert.Equal(0, knockOff.Calculate2.CallCount);
    }

    // ========================================================================
    // skill:SKILL:verification-call-tracking
    // ========================================================================

    [Fact]
    public void Verification_WasCalled_TracksInvocation()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        var tracking = knockOff.GetUser.OnCall((ko, id) => new SkUser { Id = id });
        service.GetUser(1);

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void Verification_CallCount_TracksMultipleCalls()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        var tracking = knockOff.GetUser.OnCall((ko, id) => new SkUser { Id = id });
        service.GetUser(1);
        service.GetUser(2);
        service.GetUser(3);

        Assert.Equal(3, tracking.CallCount);
    }

    [Fact]
    public void Verification_LastCallArg_TracksSingleParam()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        var tracking = knockOff.GetUser.OnCall((ko, id) => new SkUser { Id = id });
        service.GetUser(42);

        Assert.Equal(42, tracking.LastArg);
    }

    [Fact]
    public void Verification_LastCallArgs_TracksMultipleParams()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        var tracking = knockOff.Create.OnCall((ko, name, value) => { });
        service.Create("Test", 100);

        var args = tracking.LastArgs;
        Assert.Equal("Test", args.name);
        Assert.Equal(100, args.value);
    }

    // ========================================================================
    // skill:SKILL:verification-property-tracking
    // ========================================================================

    [Fact]
    public void Verification_PropertyGet_TracksCount()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        _ = service.Name;
        _ = service.Name;

        Assert.Equal(2, knockOff.Name.GetCount);
    }

    [Fact]
    public void Verification_PropertySet_TracksCount()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.Name = "First";
        service.Name = "Second";
        service.Name = "Third";

        Assert.Equal(3, knockOff.Name.SetCount);
    }

    [Fact]
    public void Verification_PropertySet_TracksLastValue()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.Name = "LastValue";

        Assert.Equal("LastValue", knockOff.Name.LastSetValue);
    }

    // ========================================================================
    // skill:SKILL:property-value-pattern (recommended for static values)
    // ========================================================================

    [Fact]
    public void PropertyValue_StaticValues_Recommended()
    {
        var knockOff = new SkBackingServiceKnockOff();
        ISkBackingService service = knockOff;

        #region skill-SKILL-property-value-pattern
        // Use Value for static test data (recommended)
        knockOff.Name.Value = "John Doe";

        // Accessing the property returns the pre-set value
        var name = service.Name;  // "John Doe"
        #endregion

        Assert.Equal("John Doe", name);
    }

    // ========================================================================
    // skill:SKILL:customization-callbacks-property (for dynamic values)
    // ========================================================================

    [Fact]
    public void PropertyCallbacks_DynamicValues()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        #region skill-SKILL-customization-callbacks-property
        // Use OnGet for dynamic/computed values
        knockOff.Name.OnGet = (ko) => $"Call #{ko.Name.GetCount}";

        // Use OnSet to capture or validate
        // Note: Value does NOT go to backing field when OnSet is set
        #endregion

        Assert.Equal("Call #1", service.Name);
        Assert.Equal("Call #2", service.Name);
    }

    // ========================================================================
    // skill:SKILL:backing-properties
    // ========================================================================

    [Fact]
    public void BackingProperties_SetterPopulatesBackingField()
    {
        var knockOff = new SkBackingServiceKnockOff();
        ISkBackingService service = knockOff;

        // Set via interface, get back via interface
        service.Name = "Pre-populated";

        Assert.Equal("Pre-populated", service.Name);
    }

    // ========================================================================
    // skill:SKILL:backing-indexers
    // ========================================================================

    [Fact]
    public void BackingIndexers_CanBePrePopulated()
    {
        var knockOff = new SkBackingPropertyStoreKnockOff();
        ISkBackingPropertyStore store = knockOff;

        knockOff.Indexer.Backing["key1"] = "value1";

        Assert.Equal("value1", store["key1"]);
    }

    // ========================================================================
    // skill:SKILL:pattern-conditional
    // ========================================================================

    [Fact]
    public void PatternConditional_SwitchExpression_Works()
    {
        var knockOff = new SkPatternServiceKnockOff();
        ISkPatternService service = knockOff;

        knockOff.GetUser.OnCall((ko, id) => id switch
        {
            1 => new SkUser { Name = "Admin" },
            2 => new SkUser { Name = "Guest" },
            _ => null
        });

        Assert.Equal("Admin", service.GetUser(1)?.Name);
        Assert.Equal("Guest", service.GetUser(2)?.Name);
        Assert.Null(service.GetUser(999));
    }

    // ========================================================================
    // skill:SKILL:pattern-exceptions
    // ========================================================================

    [Fact]
    public void PatternExceptions_ThrowsFromCallback()
    {
        var knockOff = new SkPatternServiceKnockOff();
        ISkPatternService service = knockOff;

        knockOff.Connect.OnCall((ko) =>
            throw new TimeoutException("Connection failed"));

        Assert.Throws<TimeoutException>(() => service.Connect());
    }

    // ========================================================================
    // skill:SKILL:pattern-sequential
    // ========================================================================

    [Fact]
    public void PatternSequential_QueueBasedReturns()
    {
        var knockOff = new SkPatternServiceKnockOff();
        ISkPatternService service = knockOff;

        var results = new Queue<int>([1, 2, 3]);
        knockOff.GetNext.OnCall((ko) => results.Dequeue());

        Assert.Equal(1, service.GetNext());
        Assert.Equal(2, service.GetNext());
        Assert.Equal(3, service.GetNext());
    }

    // ========================================================================
    // skill:SKILL:pattern-async
    // ========================================================================

    [Fact]
    public async Task PatternAsync_TaskFromResult()
    {
        var knockOff = new SkAsyncPatternRepositoryKnockOff();
        ISkAsyncPatternRepository service = knockOff;

        knockOff.GetUserAsync.OnCall((ko, id) =>
            Task.FromResult<SkUser?>(new SkUser { Id = id }));

        var user = await service.GetUserAsync(42);

        Assert.Equal(42, user?.Id);
    }

    // ========================================================================
    // skill:SKILL:pattern-events
    // ========================================================================

    [Fact]
    public void PatternEvents_SubscribeTracking()
    {
        var knockOff = new SkEventPatternSourceKnockOff();
        ISkEventPatternSource source = knockOff;

        source.DataReceived += (s, e) => { };

        Assert.Equal(1, knockOff.DataReceived.AddCount);
        Assert.True(knockOff.DataReceived.HasSubscribers);
    }

    [Fact]
    public void PatternEvents_RaiseEvent()
    {
        var knockOff = new SkEventPatternSourceKnockOff();
        ISkEventPatternSource source = knockOff;
        string? received = null;

        source.DataReceived += (s, e) => received = e;
        knockOff.DataReceived.Raise(null, "test data");

        Assert.Equal("test data", received);
    }

    // ========================================================================
    // skill:SKILL:pattern-generics
    // ========================================================================

    [Fact]
    public void PatternGenerics_PerTypeCallback()
    {
        var knockOff = new SkGenericSerializerKnockOff();
        ISkGenericSerializer service = knockOff;

        knockOff.Deserialize.Of<SkUser>().OnCall((ko, json) =>
            new SkUser { Name = "FromJson" });

        var user = service.Deserialize<SkUser>("{}");

        Assert.Equal("FromJson", user.Name);
    }

    [Fact]
    public void PatternGenerics_PerTypeCallTracking()
    {
        var knockOff = new SkGenericSerializerKnockOff();
        ISkGenericSerializer service = knockOff;

        knockOff.Deserialize.Of<SkUser>().OnCall((ko, json) => new SkUser());
        knockOff.Deserialize.Of<SkOrder>().OnCall((ko, json) => new SkOrder());

        service.Deserialize<SkUser>("{}");
        service.Deserialize<SkUser>("{}");
        service.Deserialize<SkOrder>("{}");

        Assert.Equal(2, knockOff.Deserialize.Of<SkUser>().CallCount);
        Assert.Equal(1, knockOff.Deserialize.Of<SkOrder>().CallCount);
        Assert.Equal(3, knockOff.Deserialize.TotalCallCount);
    }

    // ========================================================================
    // skill:SKILL:pattern-overloads
    // ========================================================================

    [Fact]
    public void PatternOverloads_SeparateTracking()
    {
        var knockOff = new SkOverloadedServiceKnockOff();
        ISkOverloadedService service = knockOff;

        // Each overload has a separate OnCall method differentiated by delegate type
        var tracking1 = knockOff.Process.OnCall((SkOverloadedServiceKnockOff.ProcessInterceptor.ProcessDelegate_String_void)((ko, data) => { }));
        var tracking2 = knockOff.Process.OnCall((SkOverloadedServiceKnockOff.ProcessInterceptor.ProcessDelegate_String_Int32_void)((ko, data, priority) => { }));
        var tracking3 = knockOff.Process.OnCall((SkOverloadedServiceKnockOff.ProcessInterceptor.ProcessDelegate_String_Int32_Boolean_void)((ko, data, priority, async) => { }));

        service.Process("a");
        service.Process("b", 1);
        service.Process("c", 2, true);

        Assert.Equal(1, tracking1.CallCount);
        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(1, tracking3.CallCount);
    }

    // ========================================================================
    // skill:SKILL:pattern-nested
    // ========================================================================

    [Fact]
    public void PatternNested_NestedClassWorks()
    {
        var knockOff = new SkUserServiceTests.SkRepoNestedKnockOff();
        ISkRepository repo = knockOff;

        var tracking = knockOff.Save.OnCall((ko, entity) => { });
        repo.Save(new object());

        Assert.True(tracking.WasCalled);
    }

    // ========================================================================
    // skill:SKILL:pattern-out-params
    // ========================================================================

    [Fact]
    public void PatternOutParams_CallbackWithDelegate()
    {
        var knockOff = new SkOutParamParserKnockOff();
        ISkOutParamParser parser = knockOff;

        var tracking = knockOff.TryParse.OnCall(
            (SkOutParamParserKnockOff.TryParseInterceptor.TryParseDelegate)((SkOutParamParserKnockOff ko, string input, out int result) =>
            {
                if (int.TryParse(input, out result))
                    return true;
                result = 0;
                return false;
            }));

        var success = parser.TryParse("42", out var value);

        Assert.True(success);
        Assert.Equal(42, value);
        Assert.Equal("42", tracking.LastArg);
    }

    // ========================================================================
    // skill:SKILL:pattern-ref-params
    // ========================================================================

    [Fact]
    public void PatternRefParams_CallbackModifiesValue()
    {
        var knockOff = new SkRefProcessorKnockOff();
        ISkRefProcessor processor = knockOff;

        var tracking = knockOff.Increment.OnCall(
            (SkRefProcessorKnockOff.IncrementInterceptor.IncrementDelegate)((SkRefProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            }));

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x);
        Assert.Equal(5, tracking.LastArg); // Original input
    }

    // ========================================================================
    // skill:SKILL:inline-stub-usage
    // ========================================================================

    [Fact]
    public void InlineStub_Usage_DemonstratesPattern()
    {
        #region skill-SKILL-inline-stub-usage
        var stub = new SkInlineUserServiceTests.Stubs.ISkInlineUserService();
        stub.GetUser.OnCall((ko, id) => new SkUser { Id = id });

        ISkInlineUserService service = stub;  // Implicit conversion
        var user = service.GetUser(42);

        Assert.True(stub.GetUser.WasCalled);
        #endregion
        Assert.Equal(42, user?.Id);
    }

    // ========================================================================
    // skill:SKILL:delegate-stubs-usage
    // ========================================================================

    [Fact]
    public void DelegateStub_Usage_DemonstratesPattern()
    {
        #region skill-SKILL-delegate-stubs-usage
        var stub = new SkValidationTests.Stubs.SkIsUniqueRule();
        stub.Interceptor.OnCall = (ko, value) => value != "duplicate";

        SkIsUniqueRule rule = stub;  // Implicit conversion
        Assert.True(rule("unique"));
        Assert.False(rule("duplicate"));

        Assert.Equal(2, stub.Interceptor.CallCount);
        Assert.Equal("duplicate", stub.Interceptor.LastCallArg);
        #endregion
    }

    // ========================================================================
    // skill:SKILL:smart-defaults-usage
    // ========================================================================

    [Fact]
    public void SmartDefaults_Usage_DemonstratesPattern()
    {
        #region skill-SKILL-smart-defaults-usage
        var knockOff = new SkSmartDefaultKnockOff();
        ISkSmartDefaultService service = knockOff;

        // No configuration needed:
        var count = service.GetCount();       // 0 (value type)
        var items = service.GetItems();       // new List<string>() (has new())
        var list = service.GetIList();        // new List<string>() (IList<T> -> List<T>)
        var optional = service.GetOptional(); // null (nullable ref)
        #endregion

        Assert.Equal(0, count);
        Assert.NotNull(items);
        Assert.Empty(items);
        Assert.NotNull(list);
        Assert.Null(optional);

        // Only throws for types that can't be safely defaulted:
        Assert.Throws<InvalidOperationException>(() => service.GetDisposable());
    }

    // ========================================================================
    // skill:SKILL:interface-class-access
    // ========================================================================

    [Fact]
    public void InterfaceClassAccess_DemonstratesPattern()
    {
        #region skill-SKILL-interface-class-access
        // Interface stubs: implicit conversion
        var interfaceKnockOff = new SkAccessDemoServiceKnockOff();
        ISkAccessDemoService service = interfaceKnockOff;

        // Class stubs: use .Object
        var classStub = new SkAccessDemoTests.Stubs.SkAccessDemoEmailService();
        SkAccessDemoEmailService emailService = classStub.Object;
        #endregion

        Assert.NotNull(service);
        Assert.NotNull(emailService);
    }
}
