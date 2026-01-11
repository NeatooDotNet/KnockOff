using Neatoo;
using Neatoo.Rules;
using System.ComponentModel;

namespace KnockOff.NeatooInterfaceTests.Collections;

/// <summary>
/// Tests for IEntityListBase - non-generic entity list base interface.
/// This interface extends IValidateListBase and adds Root property.
/// </summary>
[KnockOff<IEntityListBase>]
public partial class IEntityListBaseTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityListBase();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsIValidateListBase()
    {
        var stub = new Stubs.IEntityListBase();
        IValidateListBase baseList = stub;
        Assert.NotNull(baseList);
    }

    #region Property Tests

    [Fact]
    public void Root_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        var rootStub = new ValidateBaseStubForEntityList();
        stub.Root.Value = rootStub;

        Assert.Same(rootStub, list.Root);
        Assert.Equal(1, stub.Root.GetCount);
    }

    [Fact]
    public void Root_CanBeNull()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.Root.Value = null;

        Assert.Null(list.Root);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsModified.Value = true;

        Assert.True(list.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsSelfModified.Value = true;

        Assert.True(list.IsSelfModified);
    }

    [Fact]
    public void IsChild_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsChild.Value = true;

        Assert.True(list.IsChild);
    }

    [Fact]
    public void IsSavable_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsSavable.Value = true;

        Assert.True(list.IsSavable);
    }

    // Inherited from IValidateListBase

    [Fact]
    public void Parent_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        var parentStub = new ValidateBaseStubForEntityList();
        stub.Parent.Value = parentStub;

        Assert.Same(parentStub, list.Parent);
    }

    [Fact]
    public void IsBusy_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsBusy.Value = true;

        Assert.True(list.IsBusy);
    }

    [Fact]
    public void IsValid_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        stub.IsValid.Value = false;

        Assert.False(list.IsValid);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        await list.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
    }

    [Fact]
    public async Task RunRules_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        await list.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        list.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        list.PropertyChanged += (s, e) => { };

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        list.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase list = stub;

        var rootStub = new ValidateBaseStubForEntityList();
        stub.Root.Value = rootStub;
        _ = list.Root;
        _ = list.Root;

        stub.Root.Reset();

        Assert.Equal(0, stub.Root.GetCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IEntityListBase.
/// </summary>
[KnockOff]
public partial class EntityListBaseStub : IEntityListBase
{
}

/// <summary>
/// Standalone stub for IValidateBase used in entity list tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForEntityList : IValidateBase
{
}

public class IEntityListBaseStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new EntityListBaseStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new EntityListBaseStub();
        IEntityListBase list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void StandaloneStub_ImplementsIValidateListBase()
    {
        var stub = new EntityListBaseStub();
        IValidateListBase baseList = stub;
        Assert.NotNull(baseList);
    }

    [Fact]
    public void Root_CanBeConfigured()
    {
        var stub = new EntityListBaseStub();
        IEntityListBase list = stub;

        var rootStub = new ValidateBaseStubForEntityList();
        stub.Root.OnGet = (ko) => rootStub;

        Assert.Same(rootStub, list.Root);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new EntityListBaseStub();
        IEntityListBase list = stub;

        stub.IsModified.OnGet = (ko) => true;

        Assert.True(list.IsModified);
    }
}

/// <summary>
/// Tests for IEntityListBase&lt;I&gt; - generic entity list base interface.
/// This extends IEntityListBase and adds RemoveAt overloads.
/// Tests multiple interface inheritance: IEntityListBase, IValidateListBase&lt;I&gt;.
/// </summary>
[KnockOff<IEntityListBase<IEntityBase>>]
public partial class IEntityListBaseOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityListBase();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsIList()
    {
        var stub = new Stubs.IEntityListBase();
        IList<IEntityBase> ilist = stub;
        Assert.NotNull(ilist);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        stub.IsModified.Value = true;

        Assert.True(list.IsModified);
    }

    // IList<T> interface members

    [Fact]
    public void Count_CanBeConfigured()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        stub.Count.Value = 10;

        Assert.Equal(10, list.Count);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        var itemStub = new EntityBaseStubForListT();
        stub.Indexer.OnGet = (ko, index) => itemStub;

        _ = list[0];

        Assert.Equal(1, stub.Indexer.GetCount);
        Assert.Equal(0, stub.Indexer.LastGetKey);
    }

    [Fact]
    public void Add_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        var itemStub = new EntityBaseStubForListT();
        list.Add(itemStub);

        Assert.True(stub.Add.WasCalled);
    }

    [Fact]
    public void Remove_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        var itemStub = new EntityBaseStubForListT();
        list.Remove(itemStub);

        Assert.True(stub.Remove.WasCalled);
    }

    [Fact]
    public void Clear_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        list.Clear();

        Assert.True(stub.Clear.WasCalled);
    }

    [Fact]
    public void Contains_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        var itemStub = new EntityBaseStubForListT();
        stub.Contains.OnCall = (ko, item) => true;

        var result = list.Contains(itemStub);

        Assert.True(stub.Contains.WasCalled);
        Assert.True(result);
    }

    // IEntityListBase<I> specific - RemoveAt with soft delete flag
    // Note: This may test overload handling

    [Fact]
    public void RemoveAt_TracksCall()
    {
        var stub = new Stubs.IEntityListBase();
        IEntityListBase<IEntityBase> list = stub;

        list.RemoveAt(0);

        Assert.True(stub.RemoveAt.WasCalled);
    }
}

/// <summary>
/// Standalone stub for IEntityListBase&lt;IEntityBase&gt;.
/// </summary>
[KnockOff]
public partial class EntityListBaseOfTStub : IEntityListBase<IEntityBase>
{
}

/// <summary>
/// Standalone stub for IValidateBase used in generic entity list tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForEntityListT : IValidateBase
{
}

/// <summary>
/// Standalone stub for IEntityBase used in generic entity list tests.
/// </summary>
[KnockOff]
public partial class EntityBaseStubForListT : IEntityBase
{
}
