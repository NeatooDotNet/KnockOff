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
    public void DualityPattern_Callback_OverridesUserMethod()
    {
        var knockOff = new SkServiceKnockOff();
        ISkService service = knockOff;

        knockOff.ISkService.GetValue.OnCall = (ko, id) => id * 100;

        Assert.Equal(500, service.GetValue(5));
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
    // skill:SKILL:interface-spy-access
    // ========================================================================

    [Fact]
    public void InterfaceSpyAccess_MultipleInterfaces_HasSpiesForAll()
    {
        var knockOff = new SkSpyExampleKnockOff();

        Assert.NotNull(knockOff.ISkUserService);
        Assert.NotNull(knockOff.ISkPropertyStore);
        Assert.NotNull(knockOff.ISkEventSource);
    }

    // ========================================================================
    // skill:SKILL:multiple-interfaces
    // ========================================================================

    [Fact]
    public void MultipleInterfaces_TracksCallsSeparately()
    {
        var knockOff = new SkDataContextKnockOff();
        ISkRepository repo = knockOff;
        ISkUnitOfWork uow = knockOff;

        repo.Save(new object());
        uow.Commit();

        Assert.True(knockOff.ISkRepository.Save.WasCalled);
        Assert.True(knockOff.ISkUnitOfWork.Commit.WasCalled);
    }

    // ========================================================================
    // skill:SKILL:oncall-patterns
    // ========================================================================

    [Fact]
    public void OnCallPatterns_NoParams_Works()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;
        var called = false;

        knockOff.ISkOnCallService.Clear.OnCall = (ko) => { called = true; };
        service.Clear();

        Assert.True(called);
    }

    [Fact]
    public void OnCallPatterns_SingleParam_ReceivesArg()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;

        knockOff.ISkOnCallService.GetById.OnCall = (ko, id) => new SkUser { Id = id };

        var user = service.GetById(42);

        Assert.Equal(42, user.Id);
    }

    [Fact]
    public void OnCallPatterns_MultipleParams_ReceivesAll()
    {
        var knockOff = new SkOnCallKnockOff();
        ISkOnCallService service = knockOff;

        knockOff.ISkOnCallService.Find.OnCall = (ko, name, active) =>
            new List<SkUser> { new() { Name = name } };

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
    public void SmartDefaults_IList_ReturnsEmptyList()
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
    public void Priority_WithCallback_OverridesUserMethod()
    {
        var knockOff = new SkPriorityServiceKnockOff();
        ISkPriorityService service = knockOff;

        knockOff.ISkPriorityService.Calculate.OnCall = (ko, x) => x * 100;

        Assert.Equal(500, service.Calculate(5));
    }

    [Fact]
    public void Priority_AfterReset_ReturnsToUserMethod()
    {
        var knockOff = new SkPriorityServiceKnockOff();
        ISkPriorityService service = knockOff;

        knockOff.ISkPriorityService.Calculate.OnCall = (ko, x) => x * 100;
        Assert.Equal(500, service.Calculate(5));

        knockOff.ISkPriorityService.Calculate.Reset();

        Assert.Equal(10, service.Calculate(5));
    }

    // ========================================================================
    // skill:SKILL:verification-call-tracking
    // ========================================================================

    [Fact]
    public void Verification_WasCalled_TracksInvocation()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.GetUser(1);

        Assert.True(knockOff.ISkVerificationService.GetUser.WasCalled);
    }

    [Fact]
    public void Verification_CallCount_TracksMultipleCalls()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.GetUser(1);
        service.GetUser(2);
        service.GetUser(3);

        Assert.Equal(3, knockOff.ISkVerificationService.GetUser.CallCount);
    }

    [Fact]
    public void Verification_LastCallArg_TracksSingleParam()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.GetUser(42);

        Assert.Equal(42, knockOff.ISkVerificationService.GetUser.LastCallArg);
    }

    [Fact]
    public void Verification_LastCallArgs_TracksMultipleParams()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.Create("Test", 100);

        var args = knockOff.ISkVerificationService.Create.LastCallArgs;
        Assert.Equal("Test", args?.name);
        Assert.Equal(100, args?.value);
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

        Assert.Equal(2, knockOff.ISkVerificationService.Name.GetCount);
    }

    [Fact]
    public void Verification_PropertySet_TracksCount()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.Name = "First";
        service.Name = "Second";
        service.Name = "Third";

        Assert.Equal(3, knockOff.ISkVerificationService.Name.SetCount);
    }

    [Fact]
    public void Verification_PropertySet_TracksLastValue()
    {
        var knockOff = new SkVerificationKnockOff();
        ISkVerificationService service = knockOff;

        service.Name = "LastValue";

        Assert.Equal("LastValue", knockOff.ISkVerificationService.Name.LastSetValue);
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

        knockOff.ISkBackingPropertyStore_StringIndexerBacking["key1"] = "value1";

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

        knockOff.ISkPatternService.GetUser.OnCall = (ko, id) => id switch
        {
            1 => new SkUser { Name = "Admin" },
            2 => new SkUser { Name = "Guest" },
            _ => null
        };

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

        knockOff.ISkPatternService.Connect.OnCall = (ko) =>
            throw new TimeoutException("Connection failed");

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
        knockOff.ISkPatternService.GetNext.OnCall = (ko) => results.Dequeue();

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

        knockOff.ISkAsyncPatternRepository.GetUserAsync.OnCall = (ko, id) =>
            Task.FromResult<SkUser?>(new SkUser { Id = id });

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

        Assert.Equal(1, knockOff.ISkEventPatternSource.DataReceived.SubscribeCount);
        Assert.True(knockOff.ISkEventPatternSource.DataReceived.HasSubscribers);
    }

    [Fact]
    public void PatternEvents_RaiseEvent()
    {
        var knockOff = new SkEventPatternSourceKnockOff();
        ISkEventPatternSource source = knockOff;
        string? received = null;

        source.DataReceived += (s, e) => received = e;
        knockOff.ISkEventPatternSource.DataReceived.Raise("test data");

        Assert.Equal("test data", received);
        Assert.True(knockOff.ISkEventPatternSource.DataReceived.WasRaised);
    }

    // ========================================================================
    // skill:SKILL:pattern-generics
    // ========================================================================

    [Fact]
    public void PatternGenerics_PerTypeCallback()
    {
        var knockOff = new SkGenericSerializerKnockOff();
        ISkGenericSerializer service = knockOff;

        knockOff.ISkGenericSerializer.Deserialize.Of<SkUser>().OnCall = (ko, json) =>
            new SkUser { Name = "FromJson" };

        var user = service.Deserialize<SkUser>("{}");

        Assert.Equal("FromJson", user.Name);
    }

    [Fact]
    public void PatternGenerics_PerTypeCallTracking()
    {
        var knockOff = new SkGenericSerializerKnockOff();
        ISkGenericSerializer service = knockOff;

        knockOff.ISkGenericSerializer.Deserialize.Of<SkUser>().OnCall = (ko, json) => new SkUser();
        knockOff.ISkGenericSerializer.Deserialize.Of<SkOrder>().OnCall = (ko, json) => new SkOrder();

        service.Deserialize<SkUser>("{}");
        service.Deserialize<SkUser>("{}");
        service.Deserialize<SkOrder>("{}");

        Assert.Equal(2, knockOff.ISkGenericSerializer.Deserialize.Of<SkUser>().CallCount);
        Assert.Equal(1, knockOff.ISkGenericSerializer.Deserialize.Of<SkOrder>().CallCount);
        Assert.Equal(3, knockOff.ISkGenericSerializer.Deserialize.TotalCallCount);
    }

    // ========================================================================
    // skill:SKILL:pattern-overloads
    // ========================================================================

    [Fact]
    public void PatternOverloads_SeparateTracking()
    {
        var knockOff = new SkOverloadedServiceKnockOff();
        ISkOverloadedService service = knockOff;

        service.Process("a");
        service.Process("b", 1);
        service.Process("c", 2, true);

        Assert.Equal(1, knockOff.ISkOverloadedService.Process1.CallCount);
        Assert.Equal(1, knockOff.ISkOverloadedService.Process2.CallCount);
        Assert.Equal(1, knockOff.ISkOverloadedService.Process3.CallCount);
    }

    // ========================================================================
    // skill:SKILL:pattern-nested
    // ========================================================================

    [Fact]
    public void PatternNested_NestedClassWorks()
    {
        var knockOff = new SkUserServiceTests.SkRepoNestedKnockOff();
        ISkRepository repo = knockOff;

        repo.Save(new object());

        Assert.True(knockOff.ISkRepository.Save.WasCalled);
    }

    // ========================================================================
    // skill:SKILL:pattern-out-params
    // ========================================================================

    [Fact]
    public void PatternOutParams_CallbackWithDelegate()
    {
        var knockOff = new SkOutParamParserKnockOff();
        ISkOutParamParser parser = knockOff;

        knockOff.ISkOutParamParser.TryParse.OnCall =
            (SkOutParamParserKnockOff.ISkOutParamParser_TryParseHandler.TryParseDelegate)((SkOutParamParserKnockOff ko, string input, out int result) =>
            {
                if (int.TryParse(input, out result))
                    return true;
                result = 0;
                return false;
            });

        var success = parser.TryParse("42", out var value);

        Assert.True(success);
        Assert.Equal(42, value);
        Assert.Equal("42", knockOff.ISkOutParamParser.TryParse.LastCallArg);
    }

    // ========================================================================
    // skill:SKILL:pattern-ref-params
    // ========================================================================

    [Fact]
    public void PatternRefParams_CallbackModifiesValue()
    {
        var knockOff = new SkRefProcessorKnockOff();
        ISkRefProcessor processor = knockOff;

        knockOff.ISkRefProcessor.Increment.OnCall =
            (SkRefProcessorKnockOff.ISkRefProcessor_IncrementHandler.IncrementDelegate)((SkRefProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            });

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x);
        Assert.Equal(5, knockOff.ISkRefProcessor.Increment.LastCallArg); // Original input
    }
}
