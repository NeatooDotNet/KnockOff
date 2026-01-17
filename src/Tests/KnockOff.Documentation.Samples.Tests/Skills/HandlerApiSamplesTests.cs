using KnockOff.Documentation.Samples.Skills;

namespace KnockOff.Documentation.Samples.Tests.Skills;

/// <summary>
/// Tests for ~/.claude/skills/knockoff/handler-api.md samples.
/// Verifies handler API code snippets compile and work.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Skills")]
public class HandlerApiSamplesTests : SamplesTestBase
{
    // ========================================================================
    // skill:handler-api:method-handler-example
    // ========================================================================

    [Fact]
    public void MethodHandler_VoidNoParams_TracksCall()
    {
        var knockOff = new HaServiceKnockOff();
        IHaService service = knockOff;

        var tracking = knockOff.Initialize.OnCall((ko) => { });
        service.Initialize();

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void MethodHandler_VoidNoParams_CallbackWorks()
    {
        var knockOff = new HaServiceKnockOff();
        IHaService service = knockOff;
        var called = false;

        knockOff.Initialize.OnCall((ko) => { called = true; });
        service.Initialize();

        Assert.True(called);
    }

    [Fact]
    public void MethodHandler_SingleParam_TracksArg()
    {
        var knockOff = new HaServiceKnockOff();
        IHaService service = knockOff;

        var tracking = knockOff.GetById.OnCall((ko, id) => new HaUser { Id = id });
        service.GetById(42);

        Assert.Equal(42, tracking.LastArg);
    }

    [Fact]
    public void MethodHandler_MultipleParams_TracksArgs()
    {
        var knockOff = new HaServiceKnockOff();
        IHaService service = knockOff;

        var tracking = knockOff.Create.OnCall((ko, name, value) => new HaEntity { Name = name });
        service.Create("Test", 100);

        var args = tracking.LastArgs;
        Assert.Equal("Test", args.name);
        Assert.Equal(100, args.value);
    }

    // ========================================================================
    // skill:handler-api:property-handler-example
    // ========================================================================

    [Fact]
    public void PropertyHandler_TracksGetCount()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;

        _ = service.Name;
        _ = service.Name;
        _ = service.Name;

        Assert.Equal(3, knockOff.Name.GetCount);
    }

    [Fact]
    public void PropertyHandler_TracksSetCount()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;

        service.Name = "First";
        service.Name = "Second";

        Assert.Equal(2, knockOff.Name.SetCount);
    }

    [Fact]
    public void PropertyHandler_TracksLastSetValue()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;

        service.Name = "Last";

        Assert.Equal("Last", knockOff.Name.LastSetValue);
    }

    [Fact]
    public void PropertyHandler_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;

        knockOff.Name.OnGet = (ko) => "Always This";

        Assert.Equal("Always This", service.Name);
    }

    [Fact]
    public void PropertyHandler_OnSet_ExecutesCallback()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;
        string? capturedValue = null;

        knockOff.Name.OnSet = (ko, value) => capturedValue = value;
        service.Name = "Test";

        Assert.Equal("Test", capturedValue);
    }

    [Fact]
    public void PropertyHandler_BackingField_SetterPopulatesValue()
    {
        var knockOff = new HaPropertyServiceKnockOff();
        IHaPropertyService service = knockOff;

        // Set via interface, read back via interface (backing is protected)
        service.Name = "Pre-populated";

        Assert.Equal("Pre-populated", service.Name);
    }

    // ========================================================================
    // skill:handler-api:indexer-handler-example
    // ========================================================================

    [Fact]
    public void IndexerHandler_BackingCanBePrePopulated()
    {
        var knockOff = new HaPropertyStoreKnockOff();
        IHaPropertyStore store = knockOff;

        knockOff.Indexer.Backing["Key1"] = "Value1";
        knockOff.Indexer.Backing["Key2"] = "Value2";

        Assert.Equal("Value1", store["Key1"]);
        Assert.Equal("Value2", store["Key2"]);
    }

    [Fact]
    public void IndexerHandler_TracksGetCount()
    {
        var knockOff = new HaPropertyStoreKnockOff();
        IHaPropertyStore store = knockOff;

        _ = store["Key1"];
        _ = store["Key2"];

        Assert.Equal(2, knockOff.Indexer.GetCount);
    }

    [Fact]
    public void IndexerHandler_TracksLastGetKey()
    {
        var knockOff = new HaPropertyStoreKnockOff();
        IHaPropertyStore store = knockOff;

        _ = store["Key1"];
        _ = store["Key2"];

        Assert.Equal("Key2", knockOff.Indexer.LastGetKey);
    }

    [Fact]
    public void IndexerHandler_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new HaPropertyStoreKnockOff();
        IHaPropertyStore store = knockOff;

        knockOff.Indexer.OnGet = (ko, key) =>
        {
            if (key == "special") return "SpecialValue";
            return ko.Indexer.Backing.GetValueOrDefault(key);
        };

        Assert.Equal("SpecialValue", store["special"]);
    }

    [Fact]
    public void IndexerHandler_TracksSetEntry()
    {
        var knockOff = new HaPropertyStoreKnockOff();
        IHaPropertyStore store = knockOff;

        store["NewKey"] = "NewValue";

        Assert.Equal("NewKey", knockOff.Indexer.LastSetEntry?.Key);
        Assert.Equal("NewValue", knockOff.Indexer.LastSetEntry?.Value);
    }

    // ========================================================================
    // skill:handler-api:event-handler-example
    // ========================================================================

    [Fact]
    public void EventHandler_SubscribeTracking()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;

        source.DataReceived += (s, e) => { };

        Assert.Equal(1, knockOff.DataReceived.AddCount);
        Assert.True(knockOff.DataReceived.HasSubscribers);
    }

    [Fact]
    public void EventHandler_RaiseEvent()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;
        string? received = null;

        source.DataReceived += (s, e) => received = e;
        knockOff.DataReceived.Raise(null, "test data");

        Assert.Equal("test data", received);
    }

    [Fact]
    public void EventHandler_MultipleRaises()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;
        var raiseCount = 0;

        source.DataReceived += (s, e) => raiseCount++;
        knockOff.DataReceived.Raise(null, "first");
        knockOff.DataReceived.Raise(null, "second");
        knockOff.DataReceived.Raise(null, "third");

        Assert.Equal(3, raiseCount);
    }

    [Fact]
    public void EventHandler_CompletedEvent_NonGeneric()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;
        var raised = false;

        source.Completed += (s, e) => raised = true;
        knockOff.Completed.Raise(null, EventArgs.Empty);

        Assert.True(raised);
    }

    [Fact]
    public void EventHandler_ActionEvents()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;
        int? progressValue = null;
        (string? key, int? value) dataUpdate = (null, null);

        source.ProgressChanged += (v) => progressValue = v;
        source.DataUpdated += (k, v) => dataUpdate = (k, v);

        knockOff.ProgressChanged.Raise(75);
        knockOff.DataUpdated.Raise("key", 42);

        Assert.Equal(75, progressValue);
        Assert.Equal("key", dataUpdate.key);
        Assert.Equal(42, dataUpdate.value);
    }

    [Fact]
    public void EventHandler_Reset_ClearsTrackingAndHandlers()
    {
        var knockOff = new HaEventSourceKnockOff();
        IHaEventSource source = knockOff;

        source.DataReceived += (s, e) => { };
        knockOff.DataReceived.Raise(null, "first");

        Assert.Equal(1, knockOff.DataReceived.AddCount);
        Assert.True(knockOff.DataReceived.HasSubscribers);

        knockOff.DataReceived.Reset();

        Assert.Equal(0, knockOff.DataReceived.AddCount);
        Assert.False(knockOff.DataReceived.HasSubscribers); // Handler cleared by reset
    }

    // ========================================================================
    // skill:handler-api:overload-handler-example
    // ========================================================================

    [Fact]
    public void OverloadHandler_SeparateTracking()
    {
        var knockOff = new HaOverloadServiceKnockOff();
        IHaOverloadService service = knockOff;

        // Overloads use single interceptor with overloaded OnCall methods
        var tracking1 = knockOff.Process.OnCall((HaOverloadServiceKnockOff ko, string data) => { });
        var tracking2 = knockOff.Process.OnCall((HaOverloadServiceKnockOff ko, string data, int priority) => { });

        service.Process("a");
        service.Process("b", 1);

        Assert.Equal(1, tracking1.CallCount);
        Assert.Equal(1, tracking2.CallCount);
    }

    [Fact]
    public void OverloadHandler_SeparateCallbacks()
    {
        var knockOff = new HaOverloadServiceKnockOff();
        IHaOverloadService service = knockOff;
        var oneParam = false;
        var twoParam = false;

        // Overloads use single interceptor with overloaded OnCall methods
        knockOff.Process.OnCall((HaOverloadServiceKnockOff ko, string data) => { oneParam = true; });
        knockOff.Process.OnCall((HaOverloadServiceKnockOff ko, string data, int priority) => { twoParam = true; });

        service.Process("a");
        service.Process("b", 1);

        Assert.True(oneParam);
        Assert.True(twoParam);
    }

    [Fact]
    public void OverloadHandler_ReturnMethods()
    {
        var knockOff = new HaOverloadServiceKnockOff();
        IHaOverloadService service = knockOff;

        // Overloads use single interceptor with overloaded OnCall methods
        knockOff.Calculate.OnCall((HaOverloadServiceKnockOff ko, int value) => value * 2);
        knockOff.Calculate.OnCall((HaOverloadServiceKnockOff ko, int a, int b) => a + b);

        Assert.Equal(10, service.Calculate(5));
        Assert.Equal(8, service.Calculate(3, 5));
    }

    // ========================================================================
    // skill:handler-api:out-param-callback
    // ========================================================================

    [Fact]
    public void OutParamCallback_TryParse()
    {
        var knockOff = new HaParserKnockOff();
        IHaParser parser = knockOff;

        knockOff.TryParse.OnCall(
            (HaParserKnockOff.TryParseInterceptor.TryParseDelegate)((HaParserKnockOff ko, string input, out int result) =>
            {
                result = int.Parse(input);
                return true;
            }));

        var success = parser.TryParse("42", out var value);

        Assert.True(success);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OutParamCallback_MultipleOutParams()
    {
        var knockOff = new HaParserKnockOff();
        IHaParser parser = knockOff;

        knockOff.GetData.OnCall(
            (HaParserKnockOff.GetDataInterceptor.GetDataDelegate)((HaParserKnockOff ko, out string name, out int count) =>
            {
                name = "Test";
                count = 42;
            }));

        parser.GetData(out var resultName, out var resultCount);

        Assert.Equal("Test", resultName);
        Assert.Equal(42, resultCount);
    }

    // ========================================================================
    // skill:handler-api:ref-param-callback
    // ========================================================================

    [Fact]
    public void RefParamCallback_Increment()
    {
        var knockOff = new HaProcessorKnockOff();
        IHaProcessor processor = knockOff;

        knockOff.Increment.OnCall(
            (HaProcessorKnockOff.IncrementInterceptor.IncrementDelegate)((HaProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            }));

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x);
    }

    [Fact]
    public void RefParamCallback_MixedParams()
    {
        var knockOff = new HaProcessorKnockOff();
        IHaProcessor processor = knockOff;

        knockOff.TryUpdate.OnCall(
            (HaProcessorKnockOff.TryUpdateInterceptor.TryUpdateDelegate)((HaProcessorKnockOff ko, string key, ref string value) =>
            {
                value = value.ToUpper();
                return true;
            }));

        string val = "test";
        var success = processor.TryUpdate("key", ref val);

        Assert.True(success);
        Assert.Equal("TEST", val);
    }

    // ========================================================================
    // skill:handler-api:ref-param-tracking
    // ========================================================================

    [Fact]
    public void RefParamTracking_CapturesOriginalValue()
    {
        var knockOff = new HaProcessorKnockOff();
        IHaProcessor processor = knockOff;

        var tracking = knockOff.Increment.OnCall(
            (HaProcessorKnockOff.IncrementInterceptor.IncrementDelegate)((HaProcessorKnockOff ko, ref int value) =>
            {
                value = value * 2;
            }));

        int x = 5;
        processor.Increment(ref x);

        Assert.Equal(10, x); // Modified
        Assert.Equal(5, tracking.LastArg); // Original input value
    }

    // ========================================================================
    // skill:handler-api:async-handler-example
    // ========================================================================

    [Fact]
    public async Task AsyncHandler_TaskFromResult()
    {
        var knockOff = new HaAsyncRepositoryKnockOff();
        IHaAsyncRepository repo = knockOff;

        knockOff.GetByIdAsync.OnCall((ko, id) =>
            Task.FromResult<HaUser?>(new HaUser { Id = id }));

        var user = await repo.GetByIdAsync(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    [Fact]
    public async Task AsyncHandler_TaskFromException()
    {
        var knockOff = new HaAsyncRepositoryKnockOff();
        IHaAsyncRepository repo = knockOff;

        knockOff.SaveAsync.OnCall((ko, entity) =>
            Task.FromException<int>(new InvalidOperationException("Failed")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveAsync(new object()));
    }

    // ========================================================================
    // skill:handler-api:generic-handler-example
    // ========================================================================

    [Fact]
    public void GenericHandler_PerTypeCallback()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) =>
            new HaUser { Name = "FromJson" };

        var user = service.Deserialize<HaUser>("{}");

        Assert.Equal("FromJson", user.Name);
    }

    [Fact]
    public void GenericHandler_PerTypeTracking()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) => new HaUser();
        knockOff.Deserialize.Of<HaEntity>().OnCall = (ko, json) => new HaEntity();

        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaEntity>("{}");

        Assert.Equal(2, knockOff.Deserialize.Of<HaUser>().CallCount);
        Assert.Equal("{}", knockOff.Deserialize.Of<HaUser>().LastCallArg);
    }

    [Fact]
    public void GenericHandler_AggregateTracking()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) => new HaUser();
        knockOff.Deserialize.Of<HaEntity>().OnCall = (ko, json) => new HaEntity();

        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaEntity>("{}");
        service.Deserialize<HaEntity>("{}");
        service.Deserialize<HaEntity>("{}");

        Assert.Equal(5, knockOff.Deserialize.TotalCallCount);
        Assert.Contains(typeof(HaUser), knockOff.Deserialize.CalledTypeArguments);
        Assert.Contains(typeof(HaEntity), knockOff.Deserialize.CalledTypeArguments);
    }

    [Fact]
    public void GenericHandler_MultipleTypeParams()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

        var result = service.Convert<string, int>("hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public void GenericHandler_ResetSingleType()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) => new HaUser();
        knockOff.Deserialize.Of<HaEntity>().OnCall = (ko, json) => new HaEntity();

        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaEntity>("{}");

        knockOff.Deserialize.Of<HaUser>().Reset();

        Assert.Equal(0, knockOff.Deserialize.Of<HaUser>().CallCount);
        Assert.Equal(1, knockOff.Deserialize.Of<HaEntity>().CallCount);
    }

    [Fact]
    public void GenericHandler_ResetAllTypes()
    {
        var knockOff = new HaSerializerKnockOff();
        IHaSerializer service = knockOff;

        knockOff.Deserialize.Of<HaUser>().OnCall = (ko, json) => new HaUser();
        knockOff.Deserialize.Of<HaEntity>().OnCall = (ko, json) => new HaEntity();

        service.Deserialize<HaUser>("{}");
        service.Deserialize<HaEntity>("{}");

        knockOff.Deserialize.Reset();

        Assert.Equal(0, knockOff.Deserialize.TotalCallCount);
    }

    // ========================================================================
    // skill:handler-api:smart-defaults-example
    // ========================================================================

    [Fact]
    public void SmartDefaults_IntReturnsZero()
    {
        var knockOff = new HaDefaultsServiceKnockOff();
        IHaDefaultsService service = knockOff;

        Assert.Equal(0, service.GetCount());
    }

    [Fact]
    public void SmartDefaults_ListReturnsEmptyList()
    {
        var knockOff = new HaDefaultsServiceKnockOff();
        IHaDefaultsService service = knockOff;

        var items = service.GetItems();

        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public void SmartDefaults_IListReturnsEmptyList()
    {
        var knockOff = new HaDefaultsServiceKnockOff();
        IHaDefaultsService service = knockOff;

        var list = service.GetIList();

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void SmartDefaults_NullableRefReturnsNull()
    {
        var knockOff = new HaDefaultsServiceKnockOff();
        IHaDefaultsService service = knockOff;

        Assert.Null(service.GetOptional());
    }

    [Fact]
    public void SmartDefaults_InterfaceThrows()
    {
        var knockOff = new HaDefaultsServiceKnockOff();
        IHaDefaultsService service = knockOff;

        Assert.Throws<InvalidOperationException>(() => service.GetDisposable());
    }
}
