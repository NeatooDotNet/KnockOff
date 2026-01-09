using System.ComponentModel;

namespace KnockOff.Tests;

/// <summary>
/// Standalone KnockOff stubs for key BCL interfaces.
/// These implement interfaces directly (vs inline stubs that nest inside test classes).
/// Both simple interfaces (IDisposable, ICloneable) and collection interfaces with
/// inheritance chains (IList, ICollection, IDictionary, ISet) are fully supported.
/// </summary>

#region IDisposable

[KnockOff]
public partial class DisposableKnockOff : IDisposable
{
}

#endregion

#region IAsyncDisposable

[KnockOff]
public partial class AsyncDisposableKnockOff : IAsyncDisposable
{
}

#endregion

#region IComparable<T>

[KnockOff]
public partial class ComparableStringKnockOff : IComparable<string>
{
}

#endregion

#region IComparer<T>

[KnockOff]
public partial class ComparerStringKnockOff : IComparer<string>
{
}

#endregion

#region ICloneable

[KnockOff]
public partial class CloneableKnockOff : ICloneable
{
}

#endregion

#region IServiceProvider

[KnockOff]
public partial class ServiceProviderKnockOff : IServiceProvider
{
}

#endregion

#region INotifyPropertyChanged

[KnockOff]
public partial class NotifyPropertyChangedKnockOff : INotifyPropertyChanged
{
}

#endregion

#region IObserver<T>

[KnockOff]
public partial class ObserverStringKnockOff : IObserver<string>
{
}

#endregion

#region IProgress<T>

[KnockOff]
public partial class ProgressIntKnockOff : IProgress<int>
{
}

#endregion

// Collection interfaces with inheritance chains
// These demonstrate that standalone stubs properly implement all inherited interface members

#region IEnumerable<T>
[KnockOff]
public partial class EnumerableStringKnockOff : IEnumerable<string>
{
}

#endregion

#region ICollection<T>
[KnockOff]
public partial class CollectionStringKnockOff : ICollection<string>
{
}

#endregion

#region IList<T>
[KnockOff]
public partial class ListStringKnockOff : IList<string>
{
}

#endregion

#region IDictionary<TKey, TValue>
[KnockOff]
public partial class DictionaryStringIntKnockOff : IDictionary<string, int>
{
}

#endregion

#region ISet<T>
[KnockOff]
public partial class SetStringKnockOff : ISet<string>
{
}

#endregion

#region IReadOnlyList<T>
[KnockOff]
public partial class ReadOnlyListStringKnockOff : IReadOnlyList<string>
{
}

#endregion
