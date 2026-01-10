using Neatoo;
using Neatoo.Rules;
using System.ComponentModel;

namespace KnockOff.NeatooInterfaceTests.Collections;

/// <summary>
/// Tests for IValidateListBase - non-generic validate list base interface.
/// This interface has the Parent property.
/// </summary>
[KnockOff<IValidateListBase>]
public partial class IValidateListBaseTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateListBase();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsINeatooObject()
    {
        var stub = new Stubs.IValidateListBase();
        INeatooObject neatooObject = stub;
        Assert.NotNull(neatooObject);
    }

    #region Property Tests

    [Fact]
    public void Parent_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        var parentStub = new ValidateBaseStubForList();
        stub.Parent.Value = parentStub;

        Assert.Same(parentStub, list.Parent);
        Assert.Equal(1, stub.Parent.GetCount);
    }

    [Fact]
    public void Parent_CanBeNull()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        stub.Parent.Value = null;

        Assert.Null(list.Parent);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        stub.IsBusy.Value = true;

        Assert.True(list.IsBusy);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        stub.IsValid.Value = false;

        Assert.False(list.IsValid);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        stub.IsSelfValid.Value = false;

        Assert.False(list.IsSelfValid);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.Value = messages;

        Assert.Same(messages, list.PropertyMessages);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        await list.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
        Assert.Equal(1, stub.WaitForTasks.CallCount);
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        await list.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        list.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        list.ClearSelfMessages();

        Assert.True(stub.ClearSelfMessages.WasCalled);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        list.PropertyChanged += (s, e) => { };

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        list.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase list = stub;

        var parentStub = new ValidateBaseStubForList();
        stub.Parent.Value = parentStub;
        _ = list.Parent;
        _ = list.Parent;

        stub.Parent.Reset();

        Assert.Equal(0, stub.Parent.GetCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IValidateListBase.
/// </summary>
[KnockOff]
public partial class ValidateListBaseStub : IValidateListBase
{
}

/// <summary>
/// Standalone stub for IValidateBase used in list tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForList : IValidateBase
{
}

public class IValidateListBaseStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new ValidateListBaseStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new ValidateListBaseStub();
        IValidateListBase list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void Parent_CanBeConfigured()
    {
        var stub = new ValidateListBaseStub();
        IValidateListBase list = stub;

        var parentStub = new ValidateBaseStubForList();
        stub.Parent.OnGet = (ko) => parentStub;

        Assert.Same(parentStub, list.Parent);
    }
}

/// <summary>
/// Tests for IValidateListBase&lt;I&gt; - generic validate list base interface.
/// This extends IValidateListBase and IList&lt;I&gt;.
/// Tests collection interface inheritance.
/// </summary>
[KnockOff<IValidateListBase<IValidateBase>>]
public partial class IValidateListBaseOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateListBase();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsIList()
    {
        var stub = new Stubs.IValidateListBase();
        IList<IValidateBase> ilist = stub;
        Assert.NotNull(ilist);
    }

    [Fact]
    public void Parent_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var parentStub = new ValidateBaseStubForListT();
        stub.Parent.Value = parentStub;

        Assert.Same(parentStub, list.Parent);
    }

    // IList<T> interface members

    [Fact]
    public void Count_CanBeConfigured()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        stub.Count.Value = 5;

        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        stub.Int32Indexer.OnGet = (ko, index) => itemStub;

        _ = list[0];

        Assert.Equal(1, stub.Int32Indexer.GetCount);
        Assert.Equal(0, stub.Int32Indexer.LastGetKey);
    }

    [Fact]
    public void Add_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        list.Add(itemStub);

        Assert.True(stub.Add.WasCalled);
        Assert.Equal(1, stub.Add.CallCount);
    }

    [Fact]
    public void Remove_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        list.Remove(itemStub);

        Assert.True(stub.Remove.WasCalled);
    }

    [Fact]
    public void Clear_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        list.Clear();

        Assert.True(stub.Clear.WasCalled);
    }

    [Fact]
    public void Contains_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        stub.Contains.OnCall = (ko, item) => true;

        var result = list.Contains(itemStub);

        Assert.True(stub.Contains.WasCalled);
        Assert.True(result);
    }

    [Fact]
    public void IndexOf_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        stub.IndexOf.OnCall = (ko, item) => 3;

        var result = list.IndexOf(itemStub);

        Assert.True(stub.IndexOf.WasCalled);
        Assert.Equal(3, result);
    }

    [Fact]
    public void Insert_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        var itemStub = new ValidateBaseStubForListT();
        list.Insert(0, itemStub);

        Assert.True(stub.Insert.WasCalled);
    }

    [Fact]
    public void RemoveAt_TracksCall()
    {
        var stub = new Stubs.IValidateListBase();
        IValidateListBase<IValidateBase> list = stub;

        list.RemoveAt(0);

        Assert.True(stub.RemoveAt.WasCalled);
    }
}

/// <summary>
/// Standalone stub for IValidateListBase&lt;IValidateBase&gt;.
/// </summary>
[KnockOff]
public partial class ValidateListBaseOfTStub : IValidateListBase<IValidateBase>
{
}

/// <summary>
/// Standalone stub for IValidateBase used in generic list tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForListT : IValidateBase
{
}
