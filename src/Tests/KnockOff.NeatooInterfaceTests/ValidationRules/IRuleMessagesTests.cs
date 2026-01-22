using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for IRuleMessages - collection of rule messages.
/// This interface extends IList&lt;IRuleMessage&gt; which is a complex BCL interface.
/// This tests interface inheritance with collection interfaces.
/// </summary>
[KnockOff<IRuleMessages>]
public partial class IRuleMessagesTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRuleMessages();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IRuleMessages();
        IRuleMessages messages = stub;
        Assert.NotNull(messages);
    }

    [Fact]
    public void InlineStub_ImplementsIList()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsICollection()
    {
        var stub = new Stubs.IRuleMessages();
        ICollection<IRuleMessage> collection = stub;
        Assert.NotNull(collection);
    }

    [Fact]
    public void InlineStub_ImplementsIEnumerable()
    {
        var stub = new Stubs.IRuleMessages();
        IEnumerable<IRuleMessage> enumerable = stub;
        Assert.NotNull(enumerable);
    }

    #region IRuleMessages Specific Method Tests

    [Fact]
    public void Add_StringOverload_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IRuleMessages messages = stub;

        messages.Add("PropertyName", "Error message");

        // The Add(string, string) method is specific to IRuleMessages
        stub.Add.Verify();
    }

    [Fact]
    public void Add_StringOverload_CapturesArguments()
    {
        var stub = new Stubs.IRuleMessages();
        IRuleMessages messages = stub;
        string? capturedProp = null;
        string? capturedMsg = null;

        stub.Add.OnCall((prop, msg) =>
        {
            capturedProp = prop;
            capturedMsg = msg;
        });

        messages.Add("Name", "Required");

        Assert.Equal("Name", capturedProp);
        Assert.Equal("Required", capturedMsg);
    }

    #endregion

    #region IList<IRuleMessage> Inherited Method Tests

    [Fact]
    public void Add_IRuleMessage_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        list.Add(messageStub);

        // The IList.Add method should also be tracked
        stub.Add.Verify();
    }

    [Fact]
    public void Count_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        stub.Count.Value = 5;

        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void Indexer_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        stub.Indexer.OnGet = (index) => messageStub;

        var result = list[0];

        Assert.Same(messageStub, result);
    }

    [Fact]
    public void Clear_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        list.Clear();

        stub.Clear.Verify();
    }

    [Fact]
    public void Contains_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        stub.Contains.OnCall((item) => true);

        var result = list.Contains(messageStub);

        stub.Contains.Verify();
        Assert.True(result);
    }

    [Fact]
    public void IndexOf_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        stub.IndexOf.OnCall((item) => 3);

        var result = list.IndexOf(messageStub);

        stub.IndexOf.Verify();
        Assert.Equal(3, result);
    }

    [Fact]
    public void Insert_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        list.Insert(0, messageStub);

        stub.Insert.Verify();
    }

    [Fact]
    public void Remove_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        var messageStub = new RuleMessageStubForList();
        list.Remove(messageStub);

        stub.Remove.Verify();
    }

    [Fact]
    public void RemoveAt_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IList<IRuleMessage> list = stub;

        list.RemoveAt(0);

        stub.RemoveAt.Verify();
    }

    #endregion

    #region ICollection<IRuleMessage> Inherited Method Tests

    [Fact]
    public void IsReadOnly_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessages();
        ICollection<IRuleMessage> collection = stub;

        stub.IsReadOnly.Value = true;

        Assert.True(collection.IsReadOnly);
    }

    [Fact]
    public void CopyTo_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        ICollection<IRuleMessage> collection = stub;

        var array = new IRuleMessage[5];
        collection.CopyTo(array, 0);

        stub.CopyTo.Verify();
    }

    #endregion

    #region IEnumerable<IRuleMessage> Inherited Method Tests

    [Fact]
    public void GetEnumerator_TracksCall()
    {
        var stub = new Stubs.IRuleMessages();
        IEnumerable<IRuleMessage> enumerable = stub;

        var emptyEnumerator = new List<IRuleMessage>().GetEnumerator();
        stub.GetEnumerator.OnCall(() => emptyEnumerator);

        var enumerator = enumerable.GetEnumerator();

        stub.GetEnumerator.Verify();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IRuleMessages();
        IRuleMessages messages = stub;

        messages.Add("Prop", "Msg");
        messages.Add("Prop2", "Msg2");

        stub.Add.Reset();

        stub.Add.Verify(Times.Never);
        Assert.Equal(0, stub.Add.CallCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRuleMessages.
/// </summary>
[KnockOff]
public partial class RuleMessagesStub : IRuleMessages
{
}

/// <summary>
/// Stub for IRuleMessage used in IRuleMessages tests.
/// </summary>
[KnockOff]
public partial class RuleMessageStubForList : IRuleMessage
{
}

public class IRuleMessagesStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RuleMessagesStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RuleMessagesStub();
        IRuleMessages messages = stub;
        Assert.NotNull(messages);
    }

    [Fact]
    public void StandaloneStub_ImplementsIList()
    {
        var stub = new RuleMessagesStub();
        IList<IRuleMessage> list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void Add_StringOverload_TracksCall()
    {
        var stub = new RuleMessagesStub();
        IRuleMessages messages = stub;

        // Configure callback to enable tracking (string overload)
        var tracking = stub.Add.OnCall((string propertyName, string message) => { });

        messages.Add("Property", "Message");

        // Tracking is available via the returned tracking object
        tracking.Verify();
    }

    [Fact]
    public void Count_CanBeConfigured()
    {
        var stub = new RuleMessagesStub();
        IRuleMessages messages = stub;

        stub.Count.Value = 10;

        Assert.Equal(10, messages.Count);
    }
}
