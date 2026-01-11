using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace KnockOff.Tests;

/// <summary>
/// Tests for KnockOff stubs of the top 30 most commonly used .NET interfaces.
/// Each test verifies that the generated stub correctly implements the interface
/// and that interceptors track invocations properly.
/// All interfaces are tested using inline stubs (via [KnockOff&lt;T&gt;]).
/// </summary>
public class BclInterfaceTests
{
    #region 1. IDisposable / IAsyncDisposable

    [Fact]
    public void IDisposable_Dispose_TracksInvocation()
    {
        var stub = new DisposableStubTests.Stubs.IDisposable();
        IDisposable disposable = stub;

        disposable.Dispose();

        Assert.True(stub.Dispose.WasCalled);
        Assert.Equal(1, stub.Dispose.CallCount);
    }

    [Fact]
    public void IDisposable_Dispose_CanBeCalledMultipleTimes()
    {
        var stub = new DisposableStubTests.Stubs.IDisposable();
        IDisposable disposable = stub;

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, stub.Dispose.CallCount);
    }

    [Fact]
    public void IDisposable_OnCall_CustomBehavior()
    {
        var stub = new DisposableStubTests.Stubs.IDisposable();
        var disposed = false;
        stub.Dispose.OnCall = ko => disposed = true;
        IDisposable disposable = stub;

        disposable.Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public async Task IAsyncDisposable_DisposeAsync_TracksInvocation()
    {
        var stub = new AsyncDisposableStubTests.Stubs.IAsyncDisposable();
        IAsyncDisposable disposable = stub;

        await disposable.DisposeAsync();

        Assert.True(stub.DisposeAsync.WasCalled);
        Assert.Equal(1, stub.DisposeAsync.CallCount);
    }

    #endregion

    #region 2. IEnumerable<T> / IEnumerable

    [Fact]
    public void IEnumerable_GetEnumerator_TracksInvocation()
    {
        var stub = new EnumerableStubTests.Stubs.IEnumerable();
        IEnumerable enumerable = stub;

        // GetEnumerator will throw because no OnCall is set, but we can verify tracking
        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());
        Assert.True(stub.GetEnumerator.WasCalled);
    }

    [Fact]
    public void IEnumerableString_GetEnumerator_TracksInvocation()
    {
        var stub = new EnumerableStringStubTests.Stubs.IEnumerable();
        IEnumerable<string> enumerable = stub;

        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());
        Assert.True(stub.GetEnumerator.WasCalled);
    }

    [Fact]
    public void IEnumerableString_GetEnumerator_OnCall_ReturnsCustomEnumerator()
    {
        var stub = new EnumerableStringStubTests.Stubs.IEnumerable();
        var items = new List<string> { "a", "b", "c" };
        stub.GetEnumerator.OnCall = ko => items.GetEnumerator();
        IEnumerable<string> enumerable = stub;

        using var enumerator = enumerable.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current);
    }

    [Fact]
    public void IEnumerableInt_GetEnumerator_TracksInvocation()
    {
        var stub = new EnumerableIntStubTests.Stubs.IEnumerable();
        IEnumerable<int> enumerable = stub;

        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());
        Assert.True(stub.GetEnumerator.WasCalled);
    }

    [Fact]
    public void IEnumerableUser_GetEnumerator_TracksInvocation()
    {
        var stub = new EnumerableUserStubTests.Stubs.IEnumerable();
        IEnumerable<User> enumerable = stub;

        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());
        Assert.True(stub.GetEnumerator.WasCalled);
    }

    #endregion

    #region 4. IList<T> / IList

    [Fact]
    public void IList_Add_TracksInvocation()
    {
        var stub = new ListStubTests.Stubs.IList();
        IList list = stub;

        list.Add("test");

        Assert.True(stub.Add.WasCalled);
        Assert.Equal("test", stub.Add.LastCallArg);
    }

    [Fact]
    public void IList_Indexer_TracksGetAndSet()
    {
        var stub = new ListStubTests.Stubs.IList();
        IList list = stub;

        list[0] = "value";
        var _ = list[1];

        Assert.Equal(1, stub.Indexer.SetCount);
        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IList_Contains_TracksInvocation()
    {
        var stub = new ListStubTests.Stubs.IList();
        IList list = stub;

        var result = list.Contains("test");

        Assert.True(stub.Contains.WasCalled);
        Assert.Equal("test", stub.Contains.LastCallArg);
    }

    [Fact]
    public void IListString_Add_TracksInvocation()
    {
        var stub = new ListStringStubTests.Stubs.IList();
        IList<string> list = stub;

        list.Add("item1");
        list.Add("item2");

        Assert.Equal(2, stub.Add.CallCount);
        Assert.Equal("item2", stub.Add.LastCallArg);
    }

    [Fact]
    public void IListString_Indexer_GetSet_TracksInvocations()
    {
        var stub = new ListStringStubTests.Stubs.IList();
        IList<string> list = stub;

        list[0] = "value";
        var _ = list[1];

        Assert.Equal(1, stub.Indexer.SetCount);
        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IListInt_Add_TracksInvocation()
    {
        var stub = new ListIntStubTests.Stubs.IList();
        IList<int> list = stub;

        list.Add(42);
        list.Add(100);

        Assert.Equal(2, stub.Add.CallCount);
        Assert.Equal(100, stub.Add.LastCallArg);
    }

    [Fact]
    public void IListUser_Add_TracksInvocation()
    {
        var stub = new ListUserStubTests.Stubs.IList();
        IList<User> list = stub;
        var user = new User { Id = 1, Name = "Test" };

        list.Add(user);

        Assert.True(stub.Add.WasCalled);
        Assert.Same(user, stub.Add.LastCallArg);
    }

    #endregion

    #region 5. ICollection<T> / ICollection

    [Fact]
    public void ICollection_Count_TracksGet()
    {
        var stub = new CollectionStubTests.Stubs.ICollection();
        ICollection collection = stub;

        var _ = collection.Count;

        Assert.Equal(1, stub.Count.GetCount);
    }

    [Fact]
    public void ICollection_CopyTo_TracksInvocation()
    {
        var stub = new CollectionStubTests.Stubs.ICollection();
        ICollection collection = stub;
        var array = new object[10];

        collection.CopyTo(array, 0);

        Assert.True(stub.CopyTo.WasCalled);
    }

    [Fact]
    public void ICollectionString_Add_TracksInvocation()
    {
        var stub = new CollectionStringStubTests.Stubs.ICollection();
        ICollection<string> collection = stub;

        collection.Add("item");

        Assert.True(stub.Add.WasCalled);
        Assert.Equal("item", stub.Add.LastCallArg);
    }

    [Fact]
    public void ICollectionString_Clear_TracksInvocation()
    {
        var stub = new CollectionStringStubTests.Stubs.ICollection();
        ICollection<string> collection = stub;

        collection.Clear();

        Assert.True(stub.Clear.WasCalled);
    }

    [Fact]
    public void ICollectionInt_Contains_TracksInvocation()
    {
        var stub = new CollectionIntStubTests.Stubs.ICollection();
        ICollection<int> collection = stub;

        collection.Contains(42);

        Assert.True(stub.Contains.WasCalled);
        Assert.Equal(42, stub.Contains.LastCallArg);
    }

    #endregion

    #region 6. IDictionary<TKey, TValue> / IDictionary

    [Fact]
    public void IDictionary_Add_TracksInvocation()
    {
        var stub = new DictionaryStubTests.Stubs.IDictionary();
        IDictionary dict = stub;

        dict.Add("key", "value");

        Assert.True(stub.Add.WasCalled);
    }

    [Fact]
    public void IDictionary_Indexer_TracksGetAndSet()
    {
        var stub = new DictionaryStubTests.Stubs.IDictionary();
        IDictionary dict = stub;

        dict["key"] = "value";
        var _ = dict["otherKey"];

        Assert.Equal(1, stub.Indexer.SetCount);
        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IDictionaryStringInt_Add_TracksInvocation()
    {
        var stub = new DictionaryStringIntStubTests.Stubs.IDictionary();
        IDictionary<string, int> dict = stub;

        dict.Add("key", 42);

        Assert.True(stub.Add.WasCalled);
    }

    [Fact]
    public void IDictionaryStringInt_TryGetValue_TracksInvocation()
    {
        var stub = new DictionaryStringIntStubTests.Stubs.IDictionary();
        IDictionary<string, int> dict = stub;

        dict.TryGetValue("key", out _);

        Assert.True(stub.TryGetValue.WasCalled);
    }

    [Fact]
    public void IDictionaryIntUser_Indexer_TracksInvocations()
    {
        var stub = new DictionaryIntUserStubTests.Stubs.IDictionary();
        IDictionary<int, User> dict = stub;
        var user = new User { Id = 1, Name = "Test" };

        dict[1] = user;
        var _ = dict[2];

        Assert.Equal(1, stub.Indexer.SetCount);
        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IDictionaryStringString_ContainsKey_TracksInvocation()
    {
        var stub = new DictionaryStringStringStubTests.Stubs.IDictionary();
        IDictionary<string, string> dict = stub;

        dict.ContainsKey("test");

        Assert.True(stub.ContainsKey.WasCalled);
        Assert.Equal("test", stub.ContainsKey.LastCallArg);
    }

    #endregion

    #region 7. IReadOnlyList<T> / IReadOnlyCollection<T> / IReadOnlyDictionary<TKey, TValue>

    [Fact]
    public void IReadOnlyListString_Indexer_TracksGet()
    {
        var stub = new ReadOnlyListStringStubTests.Stubs.IReadOnlyList();
        IReadOnlyList<string> list = stub;

        var _ = list[0];

        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IReadOnlyListInt_Count_TracksGet()
    {
        var stub = new ReadOnlyListIntStubTests.Stubs.IReadOnlyList();
        IReadOnlyList<int> list = stub;

        var _ = list.Count;

        Assert.Equal(1, stub.Count.GetCount);
    }

    [Fact]
    public void IReadOnlyCollectionString_Count_TracksGet()
    {
        var stub = new ReadOnlyCollectionStringStubTests.Stubs.IReadOnlyCollection();
        IReadOnlyCollection<string> collection = stub;

        var _ = collection.Count;

        Assert.Equal(1, stub.Count.GetCount);
    }

    [Fact]
    public void IReadOnlyDictionaryStringInt_Indexer_TracksGet()
    {
        var stub = new ReadOnlyDictionaryStringIntStubTests.Stubs.IReadOnlyDictionary();
        IReadOnlyDictionary<string, int> dict = stub;

        var _ = dict["key"];

        Assert.Equal(1, stub.Indexer.GetCount);
    }

    [Fact]
    public void IReadOnlyDictionaryStringInt_ContainsKey_TracksInvocation()
    {
        var stub = new ReadOnlyDictionaryStringIntStubTests.Stubs.IReadOnlyDictionary();
        IReadOnlyDictionary<string, int> dict = stub;

        dict.ContainsKey("test");

        Assert.True(stub.ContainsKey.WasCalled);
    }

    #endregion

    #region 8. ISet<T>

    [Fact]
    public void ISetString_Add_TracksInvocation()
    {
        var stub = new SetStringStubTests.Stubs.ISet();
        ISet<string> set = stub;

        set.Add("item");

        Assert.True(stub.Add.WasCalled);
        Assert.Equal("item", stub.Add.LastCallArg);
    }

    [Fact]
    public void ISetString_Contains_TracksInvocation()
    {
        var stub = new SetStringStubTests.Stubs.ISet();
        ISet<string> set = stub;

        set.Contains("item");

        Assert.True(stub.Contains.WasCalled);
    }

    [Fact]
    public void ISetInt_UnionWith_TracksInvocation()
    {
        var stub = new SetIntStubTests.Stubs.ISet();
        ISet<int> set = stub;
        var other = new[] { 1, 2, 3 };

        set.UnionWith(other);

        Assert.True(stub.UnionWith.WasCalled);
    }

    [Fact]
    public void ISetInt_IntersectWith_TracksInvocation()
    {
        var stub = new SetIntStubTests.Stubs.ISet();
        ISet<int> set = stub;
        var other = new[] { 1, 2, 3 };

        set.IntersectWith(other);

        Assert.True(stub.IntersectWith.WasCalled);
    }

    #endregion

    #region 10. IComparable<T> / IComparable

    [Fact]
    public void IComparable_CompareTo_TracksInvocation()
    {
        var stub = new ComparableStubTests.Stubs.IComparable();
        IComparable comparable = stub;

        comparable.CompareTo("other");

        Assert.True(stub.CompareTo.WasCalled);
        Assert.Equal("other", stub.CompareTo.LastCallArg);
    }

    [Fact]
    public void IComparableString_CompareTo_TracksInvocation()
    {
        var stub = new ComparableStringStubTests.Stubs.IComparable();
        IComparable<string> comparable = stub;

        comparable.CompareTo("other");

        Assert.True(stub.CompareTo.WasCalled);
        Assert.Equal("other", stub.CompareTo.LastCallArg);
    }

    [Fact]
    public void IComparableInt_CompareTo_TracksInvocation()
    {
        var stub = new ComparableIntStubTests.Stubs.IComparable();
        IComparable<int> comparable = stub;

        comparable.CompareTo(42);

        Assert.True(stub.CompareTo.WasCalled);
        Assert.Equal(42, stub.CompareTo.LastCallArg);
    }

    [Fact]
    public void IComparableInt_CompareTo_OnCall_CustomBehavior()
    {
        var stub = new ComparableIntStubTests.Stubs.IComparable();
        stub.CompareTo.OnCall = (ko, other) => other > 50 ? 1 : -1;
        IComparable<int> comparable = stub;

        var result1 = comparable.CompareTo(60);
        var result2 = comparable.CompareTo(40);

        Assert.Equal(1, result1);
        Assert.Equal(-1, result2);
    }

    #endregion

    #region 11. IComparer<T> / IComparer

    [Fact]
    public void IComparer_Compare_TracksInvocation()
    {
        var stub = new ComparerStubTests.Stubs.IComparer();
        IComparer comparer = stub;

        comparer.Compare("a", "b");

        Assert.True(stub.Compare.WasCalled);
    }

    [Fact]
    public void IComparerString_Compare_TracksInvocation()
    {
        var stub = new ComparerStringStubTests.Stubs.IComparer();
        IComparer<string> comparer = stub;

        comparer.Compare("a", "b");

        Assert.True(stub.Compare.WasCalled);
    }

    [Fact]
    public void IComparerInt_Compare_OnCall_CustomBehavior()
    {
        var stub = new ComparerIntStubTests.Stubs.IComparer();
        stub.Compare.OnCall = (ko, x, y) => x - y;
        IComparer<int> comparer = stub;

        var result = comparer.Compare(10, 5);

        Assert.Equal(5, result);
    }

    #endregion

    #region 13. ICloneable

    [Fact]
    public void ICloneable_Clone_TracksInvocation()
    {
        var stub = new CloneableStubTests.Stubs.ICloneable();
        ICloneable cloneable = stub;

        cloneable.Clone();

        Assert.True(stub.Clone.WasCalled);
    }

    [Fact]
    public void ICloneable_Clone_OnCall_ReturnsCustomObject()
    {
        var stub = new CloneableStubTests.Stubs.ICloneable();
        var clonedObject = new object();
        stub.Clone.OnCall = ko => clonedObject;
        ICloneable cloneable = stub;

        var result = cloneable.Clone();

        Assert.Same(clonedObject, result);
    }

    #endregion

    #region 15. IServiceProvider

    [Fact]
    public void IServiceProvider_GetService_TracksInvocation()
    {
        var stub = new ServiceProviderStubTests.Stubs.IServiceProvider();
        IServiceProvider provider = stub;

        provider.GetService(typeof(string));

        Assert.True(stub.GetService.WasCalled);
        Assert.Equal(typeof(string), stub.GetService.LastCallArg);
    }

    [Fact]
    public void IServiceProvider_GetService_OnCall_ReturnsCustomService()
    {
        var stub = new ServiceProviderStubTests.Stubs.IServiceProvider();
        var service = new List<string>();
        stub.GetService.OnCall = (ko, type) => type == typeof(IList<string>) ? service : null;
        IServiceProvider provider = stub;

        var result = provider.GetService(typeof(IList<string>));

        Assert.Same(service, result);
    }

    #endregion

    #region 16. IObservable<T> / IObserver<T>

    [Fact]
    public void IObservableString_Subscribe_TracksInvocation()
    {
        var stub = new ObservableStringStubTests.Stubs.IObservable();
        IObservable<string> observable = stub;
        var observer = new ObserverStringStubTests.Stubs.IObserver();

        // Subscribe throws because no OnCall is set (IDisposable has no smart default)
        Assert.Throws<InvalidOperationException>(() => observable.Subscribe(observer));
        Assert.True(stub.Subscribe.WasCalled);
    }

    [Fact]
    public void IObserverString_OnNext_TracksInvocation()
    {
        var stub = new ObserverStringStubTests.Stubs.IObserver();
        IObserver<string> observer = stub;

        observer.OnNext("value");

        Assert.True(stub.OnNext.WasCalled);
        Assert.Equal("value", stub.OnNext.LastCallArg);
    }

    [Fact]
    public void IObserverString_OnError_TracksInvocation()
    {
        var stub = new ObserverStringStubTests.Stubs.IObserver();
        IObserver<string> observer = stub;
        var error = new Exception("test error");

        observer.OnError(error);

        Assert.True(stub.OnError.WasCalled);
        Assert.Same(error, stub.OnError.LastCallArg);
    }

    [Fact]
    public void IObserverString_OnCompleted_TracksInvocation()
    {
        var stub = new ObserverStringStubTests.Stubs.IObserver();
        IObserver<string> observer = stub;

        observer.OnCompleted();

        Assert.True(stub.OnCompleted.WasCalled);
    }

    [Fact]
    public void IObserverInt_OnNext_TracksInvocation()
    {
        var stub = new ObserverIntStubTests.Stubs.IObserver();
        IObserver<int> observer = stub;

        observer.OnNext(42);

        Assert.True(stub.OnNext.WasCalled);
        Assert.Equal(42, stub.OnNext.LastCallArg);
    }

    #endregion

    #region 17. INotifyPropertyChanged / INotifyPropertyChanging

    [Fact]
    public void INotifyPropertyChanged_Event_CanSubscribe()
    {
        var stub = new NotifyPropertyChangedStubTests.Stubs.INotifyPropertyChanged();
        INotifyPropertyChanged notifier = stub;
        var eventFired = false;

        notifier.PropertyChanged += (s, e) => eventFired = true;

        // Raise the event through the interceptor's Handler
        stub.PropertyChangedInterceptor.Handler?.Invoke(stub, new PropertyChangedEventArgs("TestProperty"));

        Assert.True(eventFired);
    }

    [Fact]
    public void INotifyPropertyChanged_Event_TracksSubscription()
    {
        var stub = new NotifyPropertyChangedStubTests.Stubs.INotifyPropertyChanged();
        INotifyPropertyChanged notifier = stub;

        notifier.PropertyChanged += (s, e) => { };
        notifier.PropertyChanged += (s, e) => { };

        Assert.Equal(2, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void INotifyPropertyChanging_Event_CanSubscribe()
    {
        var stub = new NotifyPropertyChangingStubTests.Stubs.INotifyPropertyChanging();
        INotifyPropertyChanging notifier = stub;
        var eventFired = false;

        notifier.PropertyChanging += (s, e) => eventFired = true;

        stub.PropertyChangingInterceptor.Handler?.Invoke(stub, new PropertyChangingEventArgs("TestProperty"));

        Assert.True(eventFired);
    }

    #endregion

    #region 18. IQueryable<T> / IQueryable

    [Fact]
    public void IQueryable_Expression_TracksGet()
    {
        var stub = new QueryableStubTests.Stubs.IQueryable();
        IQueryable queryable = stub;

        var _ = queryable.Expression;

        Assert.Equal(1, stub.Expression.GetCount);
    }

    [Fact]
    public void IQueryable_ElementType_TracksGet()
    {
        var stub = new QueryableStubTests.Stubs.IQueryable();
        IQueryable queryable = stub;

        var _ = queryable.ElementType;

        Assert.Equal(1, stub.ElementType.GetCount);
    }

    [Fact]
    public void IQueryable_Provider_TracksGet()
    {
        var stub = new QueryableStubTests.Stubs.IQueryable();
        IQueryable queryable = stub;

        var _ = queryable.Provider;

        Assert.Equal(1, stub.Provider.GetCount);
    }

    [Fact]
    public void IQueryableString_Expression_TracksGet()
    {
        var stub = new QueryableStringStubTests.Stubs.IQueryable();
        IQueryable<string> queryable = stub;

        var _ = queryable.Expression;

        Assert.Equal(1, stub.Expression.GetCount);
    }

    #endregion

    #region 19. Data Access: IDataReader / IDbTransaction / IDataRecord

    [Fact]
    public void IDataReader_Read_TracksInvocation()
    {
        var stub = new DataReaderStubTests.Stubs.IDataReader();
        IDataReader reader = stub;

        reader.Read();

        Assert.True(stub.Read.WasCalled);
    }

    [Fact]
    public void IDataReader_Close_TracksInvocation()
    {
        var stub = new DataReaderStubTests.Stubs.IDataReader();
        IDataReader reader = stub;

        reader.Close();

        Assert.True(stub.Close.WasCalled);
    }

    [Fact]
    public void IDataReader_GetValue_TracksInvocation()
    {
        var stub = new DataReaderStubTests.Stubs.IDataReader();
        IDataReader reader = stub;

        reader.GetValue(0);

        Assert.True(stub.GetValue.WasCalled);
        Assert.Equal(0, stub.GetValue.LastCallArg);
    }

    [Fact]
    public void IDbTransaction_Commit_TracksInvocation()
    {
        var stub = new DbTransactionStubTests.Stubs.IDbTransaction();
        IDbTransaction transaction = stub;

        transaction.Commit();

        Assert.True(stub.Commit.WasCalled);
    }

    [Fact]
    public void IDbTransaction_Rollback_TracksInvocation()
    {
        var stub = new DbTransactionStubTests.Stubs.IDbTransaction();
        IDbTransaction transaction = stub;

        transaction.Rollback();

        Assert.True(stub.Rollback.WasCalled);
    }

    [Fact]
    public void IDataRecord_GetString_TracksInvocation()
    {
        var stub = new DataRecordStubTests.Stubs.IDataRecord();
        IDataRecord record = stub;

        // GetString throws because string has no smart default for reference types
        Assert.Throws<InvalidOperationException>(() => record.GetString(0));
        Assert.True(stub.GetString.WasCalled);
        Assert.Equal(0, stub.GetString.LastCallArg);
    }

    [Fact]
    public void IDataRecord_GetInt32_TracksInvocation()
    {
        var stub = new DataRecordStubTests.Stubs.IDataRecord();
        IDataRecord record = stub;

        record.GetInt32(0);

        Assert.True(stub.GetInt32.WasCalled);
    }

    #endregion

    #region 20. IAsyncResult (Legacy)

    [Fact]
    public void IAsyncResult_IsCompleted_TracksGet()
    {
        var stub = new AsyncResultStubTests.Stubs.IAsyncResult();
        IAsyncResult asyncResult = stub;

        var _ = asyncResult.IsCompleted;

        Assert.Equal(1, stub.IsCompleted.GetCount);
    }

    [Fact]
    public void IAsyncResult_AsyncWaitHandle_TracksGet()
    {
        var stub = new AsyncResultStubTests.Stubs.IAsyncResult();
        IAsyncResult asyncResult = stub;

        var _ = asyncResult.AsyncWaitHandle;

        Assert.Equal(1, stub.AsyncWaitHandle.GetCount);
    }

    [Fact]
    public void IAsyncResult_CompletedSynchronously_TracksGet()
    {
        var stub = new AsyncResultStubTests.Stubs.IAsyncResult();
        IAsyncResult asyncResult = stub;

        var _ = asyncResult.CompletedSynchronously;

        Assert.Equal(1, stub.CompletedSynchronously.GetCount);
    }

    #endregion

    #region 21. ISerializable / IXmlSerializable

    [Fact]
    public void ISerializable_Stub_ImplementsInterface()
    {
        var stub = new SerializableStubTests.Stubs.ISerializable();

        // Can't easily call GetObjectData without valid SerializationInfo,
        // but we can verify the stub was created and implements the interface
        Assert.NotNull(stub);
        Assert.IsAssignableFrom<ISerializable>(stub);
    }

    [Fact]
    public void IXmlSerializable_GetSchema_TracksInvocation()
    {
        var stub = new XmlSerializableStubTests.Stubs.IXmlSerializable();
        IXmlSerializable serializable = stub;

        serializable.GetSchema();

        Assert.True(stub.GetSchema.WasCalled);
    }

    #endregion

    #region 22. IStructuralComparable

    [Fact]
    public void IStructuralComparable_CompareTo_TracksInvocation()
    {
        var stub = new StructuralComparableStubTests.Stubs.IStructuralComparable();
        IStructuralComparable comparable = stub;
        IComparer comparer = Comparer.Default;

        comparable.CompareTo(new object(), comparer);

        Assert.True(stub.CompareTo.WasCalled);
    }

    #endregion

    #region 24. IFormatProvider

    [Fact]
    public void IFormatProvider_GetFormat_TracksInvocation()
    {
        var stub = new FormatProviderStubTests.Stubs.IFormatProvider();
        IFormatProvider provider = stub;

        provider.GetFormat(typeof(string));

        Assert.True(stub.GetFormat.WasCalled);
        Assert.Equal(typeof(string), stub.GetFormat.LastCallArg);
    }

    #endregion

    #region 25. IAsyncEnumerable<T> / IAsyncEnumerator<T>

    [Fact]
    public void IAsyncEnumerableString_GetAsyncEnumerator_TracksInvocation()
    {
        var stub = new AsyncEnumerableStringStubTests.Stubs.IAsyncEnumerable();
        IAsyncEnumerable<string> enumerable = stub;

        Assert.Throws<InvalidOperationException>(() => enumerable.GetAsyncEnumerator());
        Assert.True(stub.GetAsyncEnumerator.WasCalled);
    }

    [Fact]
    public async Task IAsyncEnumeratorString_MoveNextAsync_TracksInvocation()
    {
        var stub = new AsyncEnumeratorStringStubTests.Stubs.IAsyncEnumerator();
        IAsyncEnumerator<string> enumerator = stub;

        await enumerator.MoveNextAsync();

        Assert.True(stub.MoveNextAsync.WasCalled);
    }

    [Fact]
    public void IAsyncEnumeratorString_Current_TracksGet()
    {
        var stub = new AsyncEnumeratorStringStubTests.Stubs.IAsyncEnumerator();
        IAsyncEnumerator<string> enumerator = stub;

        var _ = enumerator.Current;

        Assert.Equal(1, stub.Current.GetCount);
    }

    #endregion

    #region 26. IProgress<T>

    [Fact]
    public void IProgressInt_Report_TracksInvocation()
    {
        var stub = new ProgressIntStubTests.Stubs.IProgress();
        IProgress<int> progress = stub;

        progress.Report(50);

        Assert.True(stub.Report.WasCalled);
        Assert.Equal(50, stub.Report.LastCallArg);
    }

    [Fact]
    public void IProgressString_Report_TracksInvocation()
    {
        var stub = new ProgressStringStubTests.Stubs.IProgress();
        IProgress<string> progress = stub;

        progress.Report("50% complete");

        Assert.True(stub.Report.WasCalled);
        Assert.Equal("50% complete", stub.Report.LastCallArg);
    }

    [Fact]
    public void IProgressInt_Report_OnCall_CustomBehavior()
    {
        var stub = new ProgressIntStubTests.Stubs.IProgress();
        var reportedValues = new List<int>();
        stub.Report.OnCall = (ko, value) => reportedValues.Add(value);
        IProgress<int> progress = stub;

        progress.Report(25);
        progress.Report(50);
        progress.Report(100);

        Assert.Equal(new[] { 25, 50, 100 }, reportedValues);
    }

    #endregion

    #region 27. ICustomFormatter

    [Fact]
    public void ICustomFormatter_Format_TracksInvocation()
    {
        var stub = new CustomFormatterStubTests.Stubs.ICustomFormatter();
        ICustomFormatter formatter = stub;

        // Format throws because string has no smart default for reference types
        Assert.Throws<InvalidOperationException>(() => formatter.Format("G", 42, null));
        Assert.True(stub.Format.WasCalled);
    }

    [Fact]
    public void ICustomFormatter_Format_OnCall_ReturnsCustomFormat()
    {
        var stub = new CustomFormatterStubTests.Stubs.ICustomFormatter();
        stub.Format.OnCall = (ko, format, arg, formatProvider) => $"[{format}:{arg}]";
        ICustomFormatter formatter = stub;

        var result = formatter.Format("X", 255, null);

        Assert.Equal("[X:255]", result);
    }

    #endregion

    #region 28. IDictionaryEnumerator

    [Fact]
    public void IDictionaryEnumerator_MoveNext_TracksInvocation()
    {
        var stub = new DictionaryEnumeratorStubTests.Stubs.IDictionaryEnumerator();
        IDictionaryEnumerator enumerator = stub;

        enumerator.MoveNext();

        Assert.True(stub.MoveNext.WasCalled);
    }

    [Fact]
    public void IDictionaryEnumerator_Key_TracksGet()
    {
        var stub = new DictionaryEnumeratorStubTests.Stubs.IDictionaryEnumerator();
        IDictionaryEnumerator enumerator = stub;

        var _ = enumerator.Key;

        Assert.Equal(1, stub.Key.GetCount);
    }

    [Fact]
    public void IDictionaryEnumerator_Value_TracksGet()
    {
        var stub = new DictionaryEnumeratorStubTests.Stubs.IDictionaryEnumerator();
        IDictionaryEnumerator enumerator = stub;

        var _ = enumerator.Value;

        Assert.Equal(1, stub.Value.GetCount);
    }

    #endregion

    #region 29. IGrouping<TKey, TElement>

    [Fact]
    public void IGroupingStringInt_Key_TracksGet()
    {
        var stub = new GroupingStringIntStubTests.Stubs.IGrouping();
        IGrouping<string, int> grouping = stub;

        var _ = grouping.Key;

        Assert.Equal(1, stub.Key.GetCount);
    }

    [Fact]
    public void IGroupingIntUser_Key_TracksGet()
    {
        var stub = new GroupingIntUserStubTests.Stubs.IGrouping();
        IGrouping<int, User> grouping = stub;

        var _ = grouping.Key;

        Assert.Equal(1, stub.Key.GetCount);
    }

    #endregion

    #region 30. IOrderedEnumerable<T> / IOrderedQueryable<T>

    [Fact]
    public void IOrderedEnumerableString_CreateOrderedEnumerable_TracksInvocation()
    {
        var stub = new OrderedEnumerableStringStubTests.Stubs.IOrderedEnumerable();
        IOrderedEnumerable<string> ordered = stub;

        // CreateOrderedEnumerable throws because IOrderedEnumerable<T> has no smart default
        Assert.Throws<InvalidOperationException>(() => ordered.CreateOrderedEnumerable<int>(x => x.Length, null, false));
        Assert.True(stub.CreateOrderedEnumerable.WasCalled);
    }

    [Fact]
    public void IOrderedQueryableUser_Expression_TracksGet()
    {
        var stub = new OrderedQueryableUserStubTests.Stubs.IOrderedQueryable();
        IOrderedQueryable<User> ordered = stub;

        var _ = ordered.Expression;

        Assert.Equal(1, stub.Expression.GetCount);
    }

    #endregion
}
