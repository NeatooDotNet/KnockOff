using KnockOff.Documentation.Samples.Reference;

namespace KnockOff.Documentation.Samples.Tests.Reference;

/// <summary>
/// Tests for InterceptorApiSamples - verifies the code examples compile and work correctly.
/// </summary>
public class InterceptorApiSamplesTests
{
    // ========================================================================
    // Method Interceptor Examples
    // ========================================================================

    [Fact]
    public void MethodInterceptor_VoidNoParams_TracksWasCalled()
    {
        var knockOff = new ApiMethodServiceKnockOff();
        IApiMethodService service = knockOff;

        var tracking = knockOff.Initialize.OnCall((ko) => { });
        Assert.False(tracking.WasCalled);
        service.Initialize();
        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void MethodInterceptor_SingleParam_TracksLastCallArg()
    {
        var knockOff = new ApiMethodServiceKnockOff();
        IApiMethodService service = knockOff;

        var tracking = knockOff.GetById.OnCall((ko, id) => new ApiUser { Id = id });
        service.GetById(42);
        Assert.Equal(42, tracking.LastArg);
    }

    [Fact]
    public void MethodInterceptor_MultipleParams_TracksLastCallArgs()
    {
        var knockOff = new ApiMethodServiceKnockOff();
        IApiMethodService service = knockOff;

        var tracking = knockOff.Log.OnCall((ko, level, message) => { });
        service.Log("error", "Failed");
        var args = tracking.LastArgs;
        Assert.Equal("error", args.level);
        Assert.Equal("Failed", args.message);
    }

    [Fact]
    public void MethodInterceptor_OnCall_OverridesBehavior()
    {
        var knockOff = new ApiMethodServiceKnockOff();
        IApiMethodService service = knockOff;

        knockOff.GetById.OnCall((ko, id) => new ApiUser { Id = id, Name = "Test" });

        var user = service.GetById(99);
        Assert.Equal(99, user.Id);
        Assert.Equal("Test", user.Name);
    }

    // ========================================================================
    // Property Interceptor Examples
    // ========================================================================

    [Fact]
    public void PropertyInterceptor_TracksGetSetCounts()
    {
        var knockOff = new ApiPropertyServiceKnockOff();
        IApiPropertyService service = knockOff;

        _ = service.Name;
        _ = service.Name;
        _ = service.Name;
        service.Name = "First";
        service.Name = "Last";

        Assert.Equal(3, knockOff.Name.GetCount);
        Assert.Equal(2, knockOff.Name.SetCount);
        Assert.Equal("Last", knockOff.Name.LastSetValue);
    }

    [Fact]
    public void PropertyInterceptor_OnGet_OverridesGetter()
    {
        var knockOff = new ApiPropertyServiceKnockOff();
        IApiPropertyService service = knockOff;

        knockOff.Name.OnGet = (ko) => "Always this";

        Assert.Equal("Always this", service.Name);
    }

    [Fact]
    public void PropertyInterceptor_OnSet_BypassesBackingField()
    {
        var knockOff = new ApiPropertyServiceKnockOff();
        IApiPropertyService service = knockOff;
        var captured = new List<string>();

        knockOff.Name.OnSet = (ko, value) => captured.Add(value);

        service.Name = "Test";

        Assert.Single(captured);
        Assert.Equal("Test", captured[0]);
        Assert.Equal("", knockOff.Name.Value); // Backing value NOT updated (still default)
    }

    [Fact]
    public void PropertyInterceptor_Reset_ClearsCounts()
    {
        var knockOff = new ApiPropertyServiceKnockOff();
        IApiPropertyService service = knockOff;

        _ = service.Name;
        service.Name = "Test";
        knockOff.Name.Reset();

        Assert.Equal(0, knockOff.Name.GetCount);
        Assert.Equal(0, knockOff.Name.SetCount);
    }

    // ========================================================================
    // Indexer Interceptor Examples
    // ========================================================================

    [Fact]
    public void Indexer_Backing_PrePopulates()
    {
        var knockOff = new ApiIndexerStoreKnockOff();
        IApiIndexerStore store = knockOff;

        knockOff.Indexer.Backing["Key1"] = "Value1";
        knockOff.Indexer.Backing["Key2"] = "Value2";

        Assert.Equal("Value1", store["Key1"]);
        Assert.Equal("Value2", store["Key2"]);
    }

    [Fact]
    public void Indexer_TracksGetAccess()
    {
        var knockOff = new ApiIndexerStoreKnockOff();
        IApiIndexerStore store = knockOff;

        _ = store["Key1"];
        _ = store["Key2"];

        Assert.Equal(2, knockOff.Indexer.GetCount);
        Assert.Equal("Key2", knockOff.Indexer.LastGetKey);
    }

    [Fact]
    public void Indexer_OnGet_OverridesGetter()
    {
        var knockOff = new ApiIndexerStoreKnockOff();
        IApiIndexerStore store = knockOff;

        knockOff.Indexer.OnGet = (ko, key) =>
            key == "special" ? "Special!" : ko.Indexer.Backing.GetValueOrDefault(key);

        Assert.Equal("Special!", store["special"]);
    }

    [Fact]
    public void Indexer_TracksSetAccess()
    {
        var knockOff = new ApiIndexerStoreKnockOff();
        IApiIndexerStore store = knockOff;

        store["NewKey"] = "NewValue";

        Assert.Equal("NewKey", knockOff.Indexer.LastSetEntry?.Key);
        Assert.Equal("NewValue", knockOff.Indexer.LastSetEntry?.Value);
    }

    // ========================================================================
    // Event Interceptor Examples
    // ========================================================================

    [Fact]
    public void EventInterceptor_TracksSubscription()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;
        EventHandler<string> handler = (sender, data) => { };

        source.DataReceived += handler;

        Assert.Equal(1, knockOff.DataReceived.AddCount);
        Assert.True(knockOff.DataReceived.HasSubscribers);
    }

    [Fact]
    public void EventInterceptor_RaisesEvent()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;
        string? received = null;

        source.DataReceived += (sender, data) => received = data;
        knockOff.DataReceived.Raise(null, "test data");

        Assert.Equal("test data", received);
    }

    [Fact]
    public void EventInterceptor_TracksUnsubscription()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;
        EventHandler<string> handler = (sender, data) => { };

        source.DataReceived += handler;
        source.DataReceived -= handler;

        Assert.Equal(1, knockOff.DataReceived.RemoveCount);
        Assert.False(knockOff.DataReceived.HasSubscribers);
    }

    [Fact]
    public void EventInterceptor_Reset_ClearsCountsAndHandlers()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;

        source.DataReceived += (sender, data) => { };
        knockOff.DataReceived.Reset();

        Assert.Equal(0, knockOff.DataReceived.AddCount);
        Assert.False(knockOff.DataReceived.HasSubscribers);
    }

    [Fact]
    public void EventInterceptor_ActionEvents_Raise()
    {
        var knockOff = new ApiEventSourceKnockOff();
        IApiEventSource source = knockOff;
        int progress = 0;
        (string key, int value)? updated = null;

        source.ProgressChanged += p => progress = p;
        source.DataUpdated += (k, v) => updated = (k, v);

        knockOff.ProgressChanged.Raise(75);
        knockOff.DataUpdated.Raise("key", 42);

        Assert.Equal(75, progress);
        Assert.Equal(("key", 42), updated);
    }

    // ========================================================================
    // Async Method Examples
    // ========================================================================

    [Fact]
    public async Task AsyncMethod_OnCall_ReturnsTask()
    {
        var knockOff = new ApiAsyncRepositoryKnockOff();
        IApiAsyncRepository service = knockOff;

        knockOff.GetByIdAsync.OnCall((ko, id) =>
            Task.FromResult<ApiUser?>(new ApiUser { Id = id }));

        var user = await service.GetByIdAsync(42);
        Assert.Equal(42, user?.Id);
    }

    [Fact]
    public async Task AsyncMethod_OnCall_ThrowsException()
    {
        var knockOff = new ApiAsyncRepositoryKnockOff();
        IApiAsyncRepository service = knockOff;

        knockOff.SaveAsync.OnCall((ko, entity) =>
            Task.FromException<int>(new DbException("Failed")));

        await Assert.ThrowsAsync<DbException>(() => service.SaveAsync(new ApiEntity()));
    }

    // ========================================================================
    // Generic Method Interceptor Examples
    // ========================================================================

    [Fact]
    public void GenericMethod_OfT_ConfiguresPerType()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) =>
            new ApiUser { Id = 1, Name = "FromCallback" };

        var user = service.Deserialize<ApiUser>("{}");
        Assert.Equal("FromCallback", user.Name);
    }

    [Fact]
    public void GenericMethod_OfT_TracksPerType()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) => new ApiUser();
        knockOff.Deserialize.Of<ApiOrder>().OnCall = (ko, json) => new ApiOrder();

        service.Deserialize<ApiUser>("{}");
        service.Deserialize<ApiUser>("{}");
        service.Deserialize<ApiOrder>("{}");

        Assert.Equal(2, knockOff.Deserialize.Of<ApiUser>().CallCount);
        Assert.Equal(1, knockOff.Deserialize.Of<ApiOrder>().CallCount);
    }

    [Fact]
    public void GenericMethod_AggregateTracking()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) => new ApiUser();
        knockOff.Deserialize.Of<ApiOrder>().OnCall = (ko, json) => new ApiOrder();

        service.Deserialize<ApiUser>("{}");
        service.Deserialize<ApiUser>("{}");
        service.Deserialize<ApiOrder>("{}");

        Assert.Equal(3, knockOff.Deserialize.TotalCallCount);
        Assert.True(knockOff.Deserialize.WasCalled);
        Assert.Equal(2, knockOff.Deserialize.CalledTypeArguments.Count);
    }

    [Fact]
    public void GenericMethod_MultipleTypeParams()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

        var result = service.Convert<string, int>("hello");
        Assert.Equal(5, result);
    }

    [Fact]
    public void GenericMethod_Reset_SingleType()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) => new ApiUser();
        service.Deserialize<ApiUser>("{}");

        knockOff.Deserialize.Of<ApiUser>().Reset();

        Assert.Equal(0, knockOff.Deserialize.Of<ApiUser>().CallCount);
        Assert.Null(knockOff.Deserialize.Of<ApiUser>().OnCall);
    }

    [Fact]
    public void GenericMethod_Reset_AllTypes()
    {
        var knockOff = new ApiSerializerKnockOff();
        IApiSerializer service = knockOff;

        knockOff.Deserialize.Of<ApiUser>().OnCall = (ko, json) => new ApiUser();
        knockOff.Deserialize.Of<ApiOrder>().OnCall = (ko, json) => new ApiOrder();
        service.Deserialize<ApiUser>("{}");
        service.Deserialize<ApiOrder>("{}");

        knockOff.Deserialize.Reset();

        Assert.Equal(0, knockOff.Deserialize.TotalCallCount);
        Assert.Empty(knockOff.Deserialize.CalledTypeArguments);
    }
}
