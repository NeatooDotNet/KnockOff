/// <summary>
/// Code samples for docs/guides/verification.md
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class VfUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VfOrder
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}

// ============================================================================
// Interfaces
// ============================================================================

public interface IVfEmailService
{
    void SendEmail(string to, string subject, string body);
}

public interface IVfProcessor
{
    void Initialize();
    void Process(string value);
    void Cleanup();
    void Delete();
}

public interface IVfRepository
{
    VfUser? GetById(int id);
    void Save(VfUser user);
}

public interface IVfConnectionService
{
    string Name { get; set; }
    string ConnectionString { get; }
    string Output { set; }
}

public interface IVfKeyValueStore
{
    string? this[string key] { get; set; }
}

public interface IVfEventSource
{
    event EventHandler<string>? DataReceived;
}

public interface IVfSerializer
{
    T? Deserialize<T>(string json) where T : class;
}

// ============================================================================
// KnockOff Stubs
// ============================================================================

[KnockOff]
public partial class VfEmailServiceStub : IVfEmailService { }

[KnockOff]
public partial class VfProcessorStub : IVfProcessor { }

[KnockOff]
public partial class VfRepositoryStub : IVfRepository { }

[KnockOff]
public partial class VfConnectionServiceStub : IVfConnectionService { }

[KnockOff]
public partial class VfKeyValueStoreStub : IVfKeyValueStore { }

[KnockOff]
public partial class VfEventSourceStub : IVfEventSource { }

[KnockOff]
public partial class VfSerializerStub : IVfSerializer { }

// ============================================================================
// Sample Usage
// ============================================================================

public static class VerificationSamples
{
    #region verification-was-called
    public static void WasMethodCalled()
    {
        var stub = new VfEmailServiceStub();
        IVfEmailService service = stub;
        service.SendEmail("user@test.com", "Subject", "Body");

        var deleteStub = new VfProcessorStub();

        Assert.True(stub.SendEmail.WasCalled);
        Assert.False(deleteStub.Delete.WasCalled);
    }
    #endregion

    #region verification-call-count
    public static void HowManyTimes()
    {
        var stub = new VfProcessorStub();
        IVfProcessor service = stub;

        service.Process("a");
        service.Process("b");
        service.Process("c");

        Assert.Equal(3, stub.Process.CallCount);
    }
    #endregion

    #region verification-single-arg
    public static void SingleParameterCapture()
    {
        var stub = new VfRepositoryStub();
        IVfRepository service = stub;

        service.GetById(42);

        int? lastId = stub.GetById.LastCallArg;  // 42

        _ = lastId;
    }
    #endregion

    #region verification-multiple-args
    public static void MultipleParameterCapture()
    {
        var stub = new VfEmailServiceStub();
        IVfEmailService service = stub;

        service.SendEmail("user@test.com", "Subject", "Body");

        var args = stub.SendEmail.LastCallArgs;
        Assert.Equal("user@test.com", args?.to);
        Assert.Equal("Subject", args?.subject);
        Assert.Equal("Body", args?.body);
    }
    #endregion

    #region verification-no-params
    public static void NoParametersMethod()
    {
        var stub = new VfProcessorStub();
        IVfProcessor service = stub;

        service.Initialize();

        Assert.True(stub.Initialize.WasCalled);
        Assert.Equal(1, stub.Initialize.CallCount);
    }
    #endregion

    #region verification-property-counts
    public static void PropertyGetSetCounts()
    {
        var stub = new VfConnectionServiceStub();
        IVfConnectionService service = stub;

        _ = service.Name;
        _ = service.Name;
        service.Name = "First";
        service.Name = "Second";

        Assert.Equal(2, stub.Name.GetCount);
        Assert.Equal(2, stub.Name.SetCount);
        Assert.Equal("Second", stub.Name.LastSetValue);
    }
    #endregion

    #region verification-get-only-property
    public static void GetOnlyPropertyTracking()
    {
        var stub = new VfConnectionServiceStub();
        IVfConnectionService service = stub;

        _ = service.ConnectionString;
        _ = service.ConnectionString;

        Assert.Equal(2, stub.ConnectionString.GetCount);
    }
    #endregion

    #region verification-set-only-property
    public static void SetOnlyPropertyTracking()
    {
        var stub = new VfConnectionServiceStub();
        IVfConnectionService service = stub;

        service.Output = "Line 1";
        service.Output = "Line 2";

        Assert.Equal(2, stub.Output.SetCount);
        Assert.Equal("Line 2", stub.Output.LastSetValue);
    }
    #endregion

    #region verification-indexer
    public static void IndexerTracking()
    {
        var stub = new VfKeyValueStoreStub();
        IVfKeyValueStore store = stub;

        _ = store["Key1"];
        _ = store["Key2"];
        store["Key3"] = "Value";

        Assert.Equal(2, stub.Indexer.GetCount);
        Assert.Equal("Key2", stub.Indexer.LastGetKey);

        Assert.Equal(1, stub.Indexer.SetCount);
        Assert.Equal("Key3", stub.Indexer.LastSetEntry?.Key);
        Assert.Equal("Value", stub.Indexer.LastSetEntry?.Value);
    }
    #endregion

    #region verification-events
    public static void EventSubscriptionTracking()
    {
        var stub = new VfEventSourceStub();
        IVfEventSource source = stub;

        EventHandler<string>? handler = (s, e) => { };
        source.DataReceived += handler;
        source.DataReceived -= handler;

        Assert.Equal(1, stub.DataReceived.AddCount);
        Assert.Equal(1, stub.DataReceived.RemoveCount);
        Assert.False(stub.DataReceived.HasSubscribers);
    }
    #endregion

    #region verification-generic-per-type
    public static void GenericMethodPerType()
    {
        var stub = new VfSerializerStub();
        IVfSerializer service = stub;

        service.Deserialize<VfUser>("{}");
        service.Deserialize<VfUser>("{}");
        service.Deserialize<VfOrder>("{}");

        Assert.Equal(2, stub.Deserialize.Of<VfUser>().CallCount);
        Assert.Equal(1, stub.Deserialize.Of<VfOrder>().CallCount);
    }
    #endregion

    #region verification-generic-aggregate
    public static void GenericMethodAggregate()
    {
        var stub = new VfSerializerStub();
        IVfSerializer service = stub;

        service.Deserialize<VfUser>("{}");
        service.Deserialize<VfUser>("{}");
        service.Deserialize<VfOrder>("{}");

        Assert.Equal(3, stub.Deserialize.TotalCallCount);
        Assert.True(stub.Deserialize.WasCalled);
        Assert.Equal(2, stub.Deserialize.CalledTypeArguments.Count);
    }
    #endregion

    #region verification-reset
    public static void ResetTracking()
    {
        var processStub = new VfProcessorStub();
        var nameStub = new VfConnectionServiceStub();
        var eventStub = new VfEventSourceStub();
        var serializerStub = new VfSerializerStub();

        // Method
        processStub.Process.Reset();
        Assert.Equal(0, processStub.Process.CallCount);
        Assert.False(processStub.Process.WasCalled);

        // Property
        nameStub.Name.Reset();
        Assert.Equal(0, nameStub.Name.GetCount);
        Assert.Equal(0, nameStub.Name.SetCount);

        // Event
        eventStub.DataReceived.Reset();
        Assert.Equal(0, eventStub.DataReceived.AddCount);
        Assert.False(eventStub.DataReceived.HasSubscribers);

        // Generic method (single type)
        serializerStub.Deserialize.Of<VfUser>().Reset();

        // Generic method (all types)
        serializerStub.Deserialize.Reset();
    }
    #endregion

    #region verification-cross-interceptor
    public static void CrossInterceptorVerification()
    {
        var stub = new VfProcessorStub();
        IVfProcessor service = stub;

        stub.Process.OnCall = (ko, value) =>
        {
            if (!ko.Initialize.WasCalled)
                throw new InvalidOperationException("Not initialized");
        };

        // This throws because Initialize wasn't called first
        Assert.Throws<InvalidOperationException>(() => service.Process("test"));

        // Now initialize and try again
        service.Initialize();
        service.Process("test");  // Succeeds
    }
    #endregion

    #region verification-exact-count
    public static void VerifyExactCallCount()
    {
        var stub = new VfRepositoryStub();
        IVfRepository service = stub;
        service.Save(new VfUser());

        Assert.Equal(1, stub.Save.CallCount);  // Called exactly once
    }
    #endregion

    #region verification-never-called
    public static void VerifyNeverCalled()
    {
        var stub = new VfProcessorStub();

        Assert.False(stub.Delete.WasCalled);
        Assert.Equal(0, stub.Delete.CallCount);
    }
    #endregion

    #region verification-call-order
    public static void VerifyCallOrder()
    {
        var stub = new VfProcessorStub();
        IVfProcessor service = stub;

        var callOrder = new List<string>();

        stub.Initialize.OnCall = (ko) => callOrder.Add("Initialize");
        stub.Process.OnCall = (ko, value) => callOrder.Add("Process");
        stub.Cleanup.OnCall = (ko) => callOrder.Add("Cleanup");

        service.Initialize();
        service.Process("test");
        service.Cleanup();

        Assert.Equal(["Initialize", "Process", "Cleanup"], callOrder);
    }
    #endregion

    #region verification-history
    public static void VerifyAllArgumentsHistory()
    {
        var stub = new VfEmailServiceStub();
        IVfEmailService service = stub;

        var allCalls = new List<(string to, string subject, string body)>();

        stub.SendEmail.OnCall = (ko, to, subject, body) =>
        {
            allCalls.Add((to, subject, body));
        };

        service.SendEmail("a@test.com", "S1", "B1");
        service.SendEmail("b@test.com", "S2", "B2");

        Assert.Equal(2, allCalls.Count);
        Assert.Equal("a@test.com", allCalls[0].to);
        Assert.Equal("b@test.com", allCalls[1].to);
    }
    #endregion
}

// Minimal Assert for compilation
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void True(bool condition) { }
    public static void False(bool condition) { }
    public static void Throws<TException>(Action action) where TException : Exception { }
}
