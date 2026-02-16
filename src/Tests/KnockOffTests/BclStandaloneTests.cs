using System.ComponentModel;

namespace KnockOff.Tests;

/// <summary>
/// Tests for standalone KnockOff stubs of BCL interfaces.
/// Standalone stubs implement interfaces directly (vs inline stubs that nest inside test classes).
/// Each test verifies that interceptors are properties on the stub class.
/// Both simple interfaces and collection interfaces with inheritance chains are fully supported.
/// </summary>
public class BclStandaloneTests
{
    #region IDisposable

    [Fact]
    public void DisposableKnockOff_Dispose_TracksInvocation()
    {
        var knockOff = new DisposableKnockOff();
        var tracking = knockOff.Dispose.Call(() => { });
        IDisposable disposable = knockOff;

        disposable.Dispose();

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public void DisposableKnockOff_OnCall_CustomBehavior()
    {
        var knockOff = new DisposableKnockOff();
        var disposed = false;
        knockOff.Dispose.Call(() => disposed = true);
        IDisposable disposable = knockOff;

        disposable.Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public void DisposableKnockOff_Reset_ClearsTracking()
    {
        var knockOff = new DisposableKnockOff();
        var tracking = knockOff.Dispose.Call(() => { });
        IDisposable disposable = knockOff;

        disposable.Dispose();
        disposable.Dispose();

        tracking.Verify(Called.Exactly(2));

        knockOff.Dispose.Reset();

        tracking.Verify(Called.Never);
        tracking.Verify(Called.Never);
    }

    #endregion

    #region IAsyncDisposable

    [Fact]
    public async Task AsyncDisposableKnockOff_DisposeAsync_TracksInvocation()
    {
        var knockOff = new AsyncDisposableKnockOff();
        var tracking = knockOff.DisposeAsync.Call(() => { });
        IAsyncDisposable disposable = knockOff;

        await disposable.DisposeAsync();

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public async Task AsyncDisposableKnockOff_OnCall_CustomBehavior()
    {
        var knockOff = new AsyncDisposableKnockOff();
        var disposed = false;
        knockOff.DisposeAsync.Call(() =>
        {
            disposed = true;
        });
        IAsyncDisposable disposable = knockOff;

        await disposable.DisposeAsync();

        Assert.True(disposed);
    }

    #endregion

    #region IComparable<T>

    [Fact]
    public void ComparableStringKnockOff_CompareTo_TracksInvocation()
    {
        var knockOff = new ComparableStringKnockOff();
        var tracking = knockOff.CompareTo.Return((other) => 0);
        IComparable<string> comparable = knockOff;

        comparable.CompareTo("other");

        tracking.Verify();
        Assert.Equal("other", tracking.LastArgs);
    }

    [Fact]
    public void ComparableStringKnockOff_OnCall_CustomBehavior()
    {
        var knockOff = new ComparableStringKnockOff();
        knockOff.CompareTo.Return((other) => other?.Length ?? 0);
        IComparable<string> comparable = knockOff;

        var result = comparable.CompareTo("test");

        Assert.Equal(4, result);
    }

    [Fact]
    public void ComparableStringKnockOff_MultipleInvocations_TracksAll()
    {
        var knockOff = new ComparableStringKnockOff();
        var tracking = knockOff.CompareTo.Return((other) => 0);
        IComparable<string> comparable = knockOff;

        comparable.CompareTo("a");
        comparable.CompareTo("bb");
        comparable.CompareTo("ccc");

        tracking.Verify(Called.Exactly(3));
        Assert.Equal("ccc", tracking.LastArgs);
    }

    #endregion

    #region IComparer<T>

    [Fact]
    public void ComparerStringKnockOff_Compare_TracksInvocation()
    {
        var knockOff = new ComparerStringKnockOff();
        var tracking = knockOff.Compare.Return((x, y) => 0);
        IComparer<string> comparer = knockOff;

        comparer.Compare("a", "b");

        tracking.Verify();
    }

    [Fact]
    public void ComparerStringKnockOff_OnCall_CustomBehavior()
    {
        var knockOff = new ComparerStringKnockOff();
        knockOff.Compare.Return((x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));
        IComparer<string> comparer = knockOff;

        var result = comparer.Compare("A", "a");

        Assert.Equal(0, result);
    }

    [Fact]
    public void ComparerStringKnockOff_OnCall_LengthComparer()
    {
        var knockOff = new ComparerStringKnockOff();
        knockOff.Compare.Return((x, y) => (x?.Length ?? 0) - (y?.Length ?? 0));
        IComparer<string> comparer = knockOff;

        var result1 = comparer.Compare("aaa", "b");
        var result2 = comparer.Compare("a", "bbb");

        Assert.Equal(2, result1);
        Assert.Equal(-2, result2);
    }

    #endregion

    #region ICloneable

    [Fact]
    public void CloneableKnockOff_Clone_TracksInvocation()
    {
        var knockOff = new CloneableKnockOff();
        var tracking = knockOff.Clone.Return(() => knockOff);
        ICloneable cloneable = knockOff;

        cloneable.Clone();

        tracking.Verify();
    }

    [Fact]
    public void CloneableKnockOff_OnCall_ReturnsCustomObject()
    {
        var knockOff = new CloneableKnockOff();
        var clonedObject = new object();
        knockOff.Clone.Return(() => clonedObject);
        ICloneable cloneable = knockOff;

        var result = cloneable.Clone();

        Assert.Same(clonedObject, result);
    }

    [Fact]
    public void CloneableKnockOff_OnCall_ReturnsSelf()
    {
        var knockOff = new CloneableKnockOff();
        knockOff.Clone.Return(() => knockOff);
        ICloneable cloneable = knockOff;

        var result = cloneable.Clone();

        Assert.Same(knockOff, result);
    }

    #endregion

    #region IServiceProvider

    [Fact]
    public void ServiceProviderKnockOff_GetService_TracksInvocation()
    {
        var knockOff = new ServiceProviderKnockOff();
        var tracking = knockOff.GetService.Return((type) => null);
        IServiceProvider provider = knockOff;

        provider.GetService(typeof(string));

        tracking.Verify();
        Assert.Equal(typeof(string), tracking.LastArgs);
    }

    [Fact]
    public void ServiceProviderKnockOff_OnCall_ReturnsCustomService()
    {
        var knockOff = new ServiceProviderKnockOff();
        var service = new List<string>();
        knockOff.GetService.Return((type) => type == typeof(IList<string>) ? service : null);
        IServiceProvider provider = knockOff;

        var result = provider.GetService(typeof(IList<string>));

        Assert.Same(service, result);
    }

    [Fact]
    public void ServiceProviderKnockOff_OnCall_ServiceLocator()
    {
        var knockOff = new ServiceProviderKnockOff();
        var services = new Dictionary<Type, object>
        {
            { typeof(string), "hello" },
            { typeof(int), 42 },
            { typeof(List<string>), new List<string> { "a", "b" } }
        };
        knockOff.GetService.Return((type) => services.TryGetValue(type, out var service) ? service : null);
        IServiceProvider provider = knockOff;

        Assert.Equal("hello", provider.GetService(typeof(string)));
        Assert.Equal(42, provider.GetService(typeof(int)));
        Assert.NotNull(provider.GetService(typeof(List<string>)));
        Assert.Null(provider.GetService(typeof(double)));
    }

    #endregion

    #region INotifyPropertyChanged

    [Fact]
    public void NotifyPropertyChangedKnockOff_Event_CanSubscribe()
    {
        var knockOff = new NotifyPropertyChangedKnockOff();
        INotifyPropertyChanged notifier = knockOff;
        var eventFired = false;

        notifier.PropertyChanged += (s, e) => eventFired = true;

        // Standalone stubs use Raise method
        knockOff.PropertyChanged.Raise(knockOff, new PropertyChangedEventArgs("TestProperty"));

        Assert.True(eventFired);
    }

    [Fact]
    public void NotifyPropertyChangedKnockOff_Event_TracksSubscription()
    {
        var knockOff = new NotifyPropertyChangedKnockOff();
        INotifyPropertyChanged notifier = knockOff;

        notifier.PropertyChanged += (s, e) => { };
        notifier.PropertyChanged += (s, e) => { };

        // Standalone stubs use PropertyChanged.VerifyAdd
        knockOff.PropertyChanged.VerifyAdd(Called.Exactly(2));
    }

    [Fact]
    public void NotifyPropertyChangedKnockOff_Event_TracksUnsubscription()
    {
        var knockOff = new NotifyPropertyChangedKnockOff();
        INotifyPropertyChanged notifier = knockOff;
        PropertyChangedEventHandler handler = (s, e) => { };

        notifier.PropertyChanged += handler;
        notifier.PropertyChanged -= handler;

        knockOff.PropertyChanged.VerifyAdd(Called.Once);
        knockOff.PropertyChanged.VerifyRemove(Called.Once);
    }

    [Fact]
    public void NotifyPropertyChangedKnockOff_Event_CapturesPropertyName()
    {
        var knockOff = new NotifyPropertyChangedKnockOff();
        INotifyPropertyChanged notifier = knockOff;
        string? capturedPropertyName = null;

        notifier.PropertyChanged += (s, e) => capturedPropertyName = e.PropertyName;
        knockOff.PropertyChanged.Raise(knockOff, new PropertyChangedEventArgs("Name"));

        Assert.Equal("Name", capturedPropertyName);
    }

    [Fact]
    public void NotifyPropertyChangedKnockOff_HasSubscribers_TracksState()
    {
        var knockOff = new NotifyPropertyChangedKnockOff();
        INotifyPropertyChanged notifier = knockOff;
        PropertyChangedEventHandler handler = (s, e) => { };

        Assert.False(knockOff.PropertyChanged.HasSubscribers);

        notifier.PropertyChanged += handler;
        Assert.True(knockOff.PropertyChanged.HasSubscribers);

        notifier.PropertyChanged -= handler;
        Assert.False(knockOff.PropertyChanged.HasSubscribers);
    }

    #endregion

    #region IObserver<T>

    [Fact]
    public void ObserverStringKnockOff_OnNext_TracksInvocation()
    {
        var knockOff = new ObserverStringKnockOff();
        var tracking = knockOff.OnNext.Call((value) => { });
        IObserver<string> observer = knockOff;

        observer.OnNext("value");

        tracking.Verify();
        Assert.Equal("value", tracking.LastArgs);
    }

    [Fact]
    public void ObserverStringKnockOff_OnError_TracksInvocation()
    {
        var knockOff = new ObserverStringKnockOff();
        var tracking = knockOff.OnError.Call((error) => { });
        IObserver<string> observer = knockOff;
        var error = new Exception("test error");

        observer.OnError(error);

        tracking.Verify();
        Assert.Same(error, tracking.LastArgs);
    }

    [Fact]
    public void ObserverStringKnockOff_OnCompleted_TracksInvocation()
    {
        var knockOff = new ObserverStringKnockOff();
        var tracking = knockOff.OnCompleted.Call(() => { });
        IObserver<string> observer = knockOff;

        observer.OnCompleted();

        tracking.Verify();
    }

    [Fact]
    public void ObserverStringKnockOff_FullSequence()
    {
        var knockOff = new ObserverStringKnockOff();
        var receivedValues = new List<string>();
        var onNextTracking = knockOff.OnNext.Call((value) => receivedValues.Add(value));
        var onCompletedTracking = knockOff.OnCompleted.Call(() => { });
        IObserver<string> observer = knockOff;

        observer.OnNext("a");
        observer.OnNext("b");
        observer.OnNext("c");
        observer.OnCompleted();

        onNextTracking.Verify(Called.Exactly(3));
        Assert.Equal(new[] { "a", "b", "c" }, receivedValues);
        onCompletedTracking.Verify();
    }

    #endregion

    #region IProgress<T>

    [Fact]
    public void ProgressIntKnockOff_Report_TracksInvocation()
    {
        var knockOff = new ProgressIntKnockOff();
        var tracking = knockOff.Report.Call((value) => { });
        IProgress<int> progress = knockOff;

        progress.Report(50);

        tracking.Verify();
        Assert.Equal(50, tracking.LastArgs);
    }

    [Fact]
    public void ProgressIntKnockOff_OnCall_CustomBehavior()
    {
        var knockOff = new ProgressIntKnockOff();
        var reportedValues = new List<int>();
        knockOff.Report.Call((value) => reportedValues.Add(value));
        IProgress<int> progress = knockOff;

        progress.Report(25);
        progress.Report(50);
        progress.Report(100);

        Assert.Equal(new[] { 25, 50, 100 }, reportedValues);
    }

    [Fact]
    public void ProgressIntKnockOff_SimulateProgressTracking()
    {
        var knockOff = new ProgressIntKnockOff();
        var lastProgress = 0;
        var progressUpdates = 0;
        var tracking = knockOff.Report.Call((value) =>
        {
            lastProgress = value;
            progressUpdates++;
        });
        IProgress<int> progress = knockOff;

        // Simulate a file download with progress updates
        for (int i = 10; i <= 100; i += 10)
        {
            progress.Report(i);
        }

        Assert.Equal(100, lastProgress);
        Assert.Equal(10, progressUpdates);
        tracking.Verify(Called.Exactly(10));
    }

    #endregion

    #region IEnumerable<T>
    [Fact]
    public void EnumerableStringKnockOff_GenericGetEnumerator_TracksInvocation()
    {
        var knockOff = new EnumerableStringKnockOff();
        var tracking = knockOff.GetEnumerator.Return(() => throw new InvalidOperationException());
        IEnumerable<string> enumerable = knockOff;

        // Call the generic GetEnumerator (will throw since callback throws)
        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public void EnumerableStringKnockOff_NonGenericGetEnumerator_TracksInvocation()
    {
        var knockOff = new EnumerableStringKnockOff();
        var tracking = knockOff.GetEnumerator.Return(() => throw new InvalidOperationException());
        System.Collections.IEnumerable enumerable = knockOff;

        // Call the non-generic GetEnumerator (will throw since callback throws)
        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator());

        // Both generic and non-generic should use the same handler
        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public void EnumerableStringKnockOff_OnCall_ReturnsCustomEnumerator()
    {
        var knockOff = new EnumerableStringKnockOff();
        var items = new List<string> { "a", "b", "c" };
        // Must cast explicitly because List<T>.GetEnumerator() returns a struct
        knockOff.GetEnumerator.Return(() => ((IEnumerable<string>)items).GetEnumerator());
        IEnumerable<string> enumerable = knockOff;

        var result = enumerable.ToList();

        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    #endregion

    #region ICollection<T>
    [Fact]
    public void CollectionStringKnockOff_Add_TracksInvocation()
    {
        var knockOff = new CollectionStringKnockOff();
        var tracking = knockOff.Add.Call((item) => { });
        ICollection<string> collection = knockOff;

        collection.Add("test");

        tracking.Verify();
        Assert.Equal("test", tracking.LastArgs);
    }

    [Fact]
    public void CollectionStringKnockOff_Count_TracksInvocation()
    {
        var knockOff = new CollectionStringKnockOff();
        knockOff.Count.Get(() => 42);
        ICollection<string> collection = knockOff;

        var count = collection.Count;

        Assert.Equal(42, count);
        knockOff.Count.VerifyGet(Called.Once);
    }

    [Fact]
    public void CollectionStringKnockOff_InheritedGetEnumerator_Works()
    {
        var knockOff = new CollectionStringKnockOff();
        var items = new List<string> { "x", "y" };
        // Must cast explicitly because List<T>.GetEnumerator() returns a struct
        var tracking = knockOff.GetEnumerator.Return(() => ((IEnumerable<string>)items).GetEnumerator());
        IEnumerable<string> enumerable = knockOff;

        // Note: Don't use ToList() here because it optimizes for ICollection<T> and calls CopyTo instead of GetEnumerator
        var result = new List<string>();
        foreach (var item in enumerable)
            result.Add(item);

        Assert.Equal(new[] { "x", "y" }, result);
        tracking.Verify();
    }

    #endregion

    #region IList<T>
    [Fact]
    public void ListStringKnockOff_Indexer_TracksInvocation()
    {
        var knockOff = new ListStringKnockOff();
        knockOff.Indexer.Get((index) => $"item_{index}");
        IList<string> list = knockOff;

        var result = list[5];

        Assert.Equal("item_5", result);
        knockOff.Indexer.VerifyGet(Called.Once);
    }

    [Fact]
    public void ListStringKnockOff_IndexOf_TracksInvocation()
    {
        var knockOff = new ListStringKnockOff();
        var tracking = knockOff.IndexOf.Return((item) => item == "found" ? 3 : -1);
        IList<string> list = knockOff;

        var index = list.IndexOf("found");

        Assert.Equal(3, index);
        tracking.Verify();
    }

    [Fact]
    public void ListStringKnockOff_InheritedCount_Works()
    {
        var knockOff = new ListStringKnockOff();
        knockOff.Count.Get(() => 10);
        ICollection<string> collection = knockOff;

        var count = collection.Count;

        Assert.Equal(10, count);
    }

    [Fact]
    public void ListStringKnockOff_InheritedGetEnumerator_Works()
    {
        var knockOff = new ListStringKnockOff();
        var items = new List<string> { "1", "2", "3" };
        // Must cast explicitly because List<T>.GetEnumerator() returns a struct
        knockOff.GetEnumerator.Return(() => ((IEnumerable<string>)items).GetEnumerator());
        System.Collections.IEnumerable enumerable = knockOff;

        var result = new List<object>();
        foreach (var item in enumerable)
            result.Add(item);

        Assert.Equal(new object[] { "1", "2", "3" }, result);
    }

    #endregion

    #region IDictionary<TKey, TValue>
    [Fact]
    public void DictionaryStringIntKnockOff_Indexer_TracksInvocation()
    {
        var knockOff = new DictionaryStringIntKnockOff();
        knockOff.Indexer.Get((key) => key.Length);
        IDictionary<string, int> dict = knockOff;

        var result = dict["hello"];

        Assert.Equal(5, result);
    }

    [Fact]
    public void DictionaryStringIntKnockOff_Keys_TracksInvocation()
    {
        var knockOff = new DictionaryStringIntKnockOff();
        var keys = new List<string> { "a", "b" };
        knockOff.Keys.Get(() => keys);
        IDictionary<string, int> dict = knockOff;

        var result = dict.Keys;

        Assert.Same(keys, result);
    }

    [Fact]
    public void DictionaryStringIntKnockOff_InheritedGetEnumerator_Works()
    {
        var knockOff = new DictionaryStringIntKnockOff();
        var items = new List<KeyValuePair<string, int>>
        {
            new("a", 1),
            new("b", 2)
        };
        // Must cast explicitly because List<T>.GetEnumerator() returns a struct
        var tracking = knockOff.GetEnumerator.Return(() => ((IEnumerable<KeyValuePair<string, int>>)items).GetEnumerator());

        // Note: Don't use ToList() here because it optimizes for ICollection<T> and calls CopyTo instead of GetEnumerator
        // Must cast to interface for foreach because 'GetEnumerator' property conflicts with the method
        var result = new List<KeyValuePair<string, int>>();
        foreach (var item in (IEnumerable<KeyValuePair<string, int>>)knockOff)
            result.Add(item);

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Key);
        tracking.Verify();
    }

    #endregion

    #region ISet<T>
    [Fact]
    public void SetStringKnockOff_Add_TracksInvocation()
    {
        var knockOff = new SetStringKnockOff();
        var tracking = knockOff.Add.Return((item) => true);
        ISet<string> set = knockOff;

        var result = set.Add("test");

        Assert.True(result);
        tracking.Verify();
    }

    [Fact]
    public void SetStringKnockOff_InheritedCollectionAdd_Works()
    {
        var knockOff = new SetStringKnockOff();
        // ICollection<T>.Add is void, ISet<T>.Add returns bool
        // These are different overloads with different signatures, so use the void callback
        var tracking = knockOff.Add.Call((string item) => { });
        ICollection<string> collection = knockOff;

        collection.Add("test");

        tracking.Verify();
        Assert.Equal("test", tracking.LastArgs);
    }

    [Fact]
    public void SetStringKnockOff_UnionWith_TracksInvocation()
    {
        var knockOff = new SetStringKnockOff();
        var tracking = knockOff.UnionWith.Call((other) => { });
        ISet<string> set = knockOff;
        var other = new[] { "a", "b" };

        set.UnionWith(other);

        tracking.Verify();
    }

    #endregion

    #region IReadOnlyList<T>
    [Fact]
    public void ReadOnlyListStringKnockOff_Indexer_TracksInvocation()
    {
        var knockOff = new ReadOnlyListStringKnockOff();
        knockOff.Indexer.Get((index) => $"item_{index}");
        IReadOnlyList<string> list = knockOff;

        var result = list[7];

        Assert.Equal("item_7", result);
    }

    [Fact]
    public void ReadOnlyListStringKnockOff_Count_TracksInvocation()
    {
        var knockOff = new ReadOnlyListStringKnockOff();
        knockOff.Count.Get(() => 100);
        IReadOnlyCollection<string> collection = knockOff;

        var count = collection.Count;

        Assert.Equal(100, count);
    }

    [Fact]
    public void ReadOnlyListStringKnockOff_InheritedGetEnumerator_Works()
    {
        var knockOff = new ReadOnlyListStringKnockOff();
        var items = new List<string> { "read", "only" };
        // Must cast explicitly because List<T>.GetEnumerator() returns a struct
        knockOff.GetEnumerator.Return(() => ((IEnumerable<string>)items).GetEnumerator());
        IEnumerable<string> enumerable = knockOff;

        var result = enumerable.ToList();

        Assert.Equal(new[] { "read", "only" }, result);
    }

    #endregion
}
