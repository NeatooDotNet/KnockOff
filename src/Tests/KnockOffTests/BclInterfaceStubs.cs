using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace KnockOff.Tests;

/// <summary>
/// KnockOff stubs for the top 30 most commonly used .NET interfaces.
/// Based on BigQuery analysis of GitHub C# repositories.
///
/// KNOWN ISSUES DISCOVERED:
///
/// 1. IEnumerator&lt;T&gt; - Duplicate Current property from IEnumerator inheritance
///    Bug: Generator creates duplicate IEnumerator_CurrentInterceptor
///
/// 2. Methods hiding inherited members (CS0108 treated as error):
///    - IEquatable&lt;T&gt;.Equals hides object.Equals
///    - IStructuralEquatable.Equals/GetHashCode hides object.*
///    - IFormattable.ToString hides object.ToString
///    - IEqualityComparer.Equals/GetHashCode hides object.*
///    - IConvertible.ToString hides object.ToString
///    Fix needed: Generator should emit 'new' keyword for these
///
/// 3. Nullability mismatch (CS8769):
///    - IDbConnection.ConnectionString setter expects non-nullable
///    - IDbCommand.CommandText setter expects non-nullable
///    Fix needed: Generator should match interface nullability exactly
/// </summary>

#region 1. IDisposable / IAsyncDisposable - WORKING

[KnockOff<IDisposable>]
public partial class DisposableStubTests
{
}

[KnockOff<IAsyncDisposable>]
public partial class AsyncDisposableStubTests
{
}

#endregion

#region 2. IEnumerable<T> / IEnumerable - WORKING

[KnockOff<IEnumerable>]
public partial class EnumerableStubTests
{
}

[KnockOff<IEnumerable<string>>]
public partial class EnumerableStringStubTests
{
}

[KnockOff<IEnumerable<int>>]
public partial class EnumerableIntStubTests
{
}

[KnockOff<IEnumerable<User>>]
public partial class EnumerableUserStubTests
{
}

#endregion

#region 3. IEnumerator<T> / IEnumerator - WORKING (fixed: deduplicated Current interceptor)

[KnockOff<IEnumerator>]
public partial class EnumeratorStubTests
{
}

[KnockOff<IEnumerator<string>>]
public partial class EnumeratorStringStubTests
{
}

[KnockOff<IEnumerator<int>>]
public partial class EnumeratorIntStubTests
{
}

#endregion

#region 4. IList<T> / IList - WORKING

[KnockOff<IList>]
public partial class ListStubTests
{
}

[KnockOff<IList<string>>]
public partial class ListStringStubTests
{
}

[KnockOff<IList<int>>]
public partial class ListIntStubTests
{
}

[KnockOff<IList<User>>]
public partial class ListUserStubTests
{
}

#endregion

#region 5. ICollection<T> / ICollection - WORKING

[KnockOff<ICollection>]
public partial class CollectionStubTests
{
}

[KnockOff<ICollection<string>>]
public partial class CollectionStringStubTests
{
}

[KnockOff<ICollection<int>>]
public partial class CollectionIntStubTests
{
}

#endregion

#region 6. IDictionary<TKey, TValue> / IDictionary - WORKING

[KnockOff<IDictionary>]
public partial class DictionaryStubTests
{
}

[KnockOff<IDictionary<string, int>>]
public partial class DictionaryStringIntStubTests
{
}

[KnockOff<IDictionary<int, User>>]
public partial class DictionaryIntUserStubTests
{
}

[KnockOff<IDictionary<string, string>>]
public partial class DictionaryStringStringStubTests
{
}

#endregion

#region 7. IReadOnlyList<T> / IReadOnlyCollection<T> / IReadOnlyDictionary<TKey, TValue> - WORKING

[KnockOff<IReadOnlyList<string>>]
public partial class ReadOnlyListStringStubTests
{
}

[KnockOff<IReadOnlyList<int>>]
public partial class ReadOnlyListIntStubTests
{
}

[KnockOff<IReadOnlyCollection<string>>]
public partial class ReadOnlyCollectionStringStubTests
{
}

[KnockOff<IReadOnlyDictionary<string, int>>]
public partial class ReadOnlyDictionaryStringIntStubTests
{
}

#endregion

#region 8. ISet<T> - WORKING

[KnockOff<ISet<string>>]
public partial class SetStringStubTests
{
}

[KnockOff<ISet<int>>]
public partial class SetIntStubTests
{
}

#endregion

#region 9. IEquatable<T> - WORKING (fixed: 'new' keyword added for Equals interceptor)

[KnockOff<IEquatable<string>>]
public partial class EquatableStringStubTests
{
}

[KnockOff<IEquatable<int>>]
public partial class EquatableIntStubTests
{
}

[KnockOff<IEquatable<User>>]
public partial class EquatableUserStubTests
{
}

#endregion

#region 10. IComparable<T> / IComparable - WORKING

[KnockOff<IComparable>]
public partial class ComparableStubTests
{
}

[KnockOff<IComparable<string>>]
public partial class ComparableStringStubTests
{
}

[KnockOff<IComparable<int>>]
public partial class ComparableIntStubTests
{
}

#endregion

#region 11. IComparer<T> / IComparer - WORKING

[KnockOff<IComparer>]
public partial class ComparerStubTests
{
}

[KnockOff<IComparer<string>>]
public partial class ComparerStringStubTests
{
}

[KnockOff<IComparer<int>>]
public partial class ComparerIntStubTests
{
}

#endregion

#region 12. IEqualityComparer<T> / IEqualityComparer - WORKING (fixed: 'new' keyword added)

[KnockOff<IEqualityComparer>]
public partial class EqualityComparerStubTests
{
}

[KnockOff<IEqualityComparer<string>>]
public partial class EqualityComparerStringStubTests
{
}

[KnockOff<IEqualityComparer<int>>]
public partial class EqualityComparerIntStubTests
{
}

#endregion

#region 13. ICloneable - WORKING

[KnockOff<ICloneable>]
public partial class CloneableStubTests
{
}

#endregion

#region 14. IFormattable - WORKING (fixed: 'new' keyword added for ToString interceptor)

[KnockOff<IFormattable>]
public partial class FormattableStubTests
{
}

#endregion

#region 15. IServiceProvider - WORKING

[KnockOff<IServiceProvider>]
public partial class ServiceProviderStubTests
{
}

#endregion

#region 16. IObservable<T> / IObserver<T> - WORKING

[KnockOff<IObservable<string>>]
public partial class ObservableStringStubTests
{
}

[KnockOff<IObservable<int>>]
public partial class ObservableIntStubTests
{
}

[KnockOff<IObserver<string>>]
public partial class ObserverStringStubTests
{
}

[KnockOff<IObserver<int>>]
public partial class ObserverIntStubTests
{
}

#endregion

#region 17. INotifyPropertyChanged / INotifyPropertyChanging - WORKING

[KnockOff<INotifyPropertyChanged>]
public partial class NotifyPropertyChangedStubTests
{
}

[KnockOff<INotifyPropertyChanging>]
public partial class NotifyPropertyChangingStubTests
{
}

#endregion

#region 18. IQueryable<T> / IQueryable - WORKING

[KnockOff<IQueryable>]
public partial class QueryableStubTests
{
}

[KnockOff<IQueryable<string>>]
public partial class QueryableStringStubTests
{
}

[KnockOff<IQueryable<User>>]
public partial class QueryableUserStubTests
{
}

#endregion

#region 19. Data Access: IDataReader / IDbConnection / IDbCommand / IDbTransaction - WORKING

// IDataReader - WORKING
[KnockOff<IDataReader>]
public partial class DataReaderStubTests
{
}

// BUG 3 FIXED: Asymmetric nullability - getter returns string?, setter expects string
// IDbConnection.ConnectionString: getter can return null, setter requires non-null
// Fix: Generator now captures SetterParameterType separately and uses it for explicit implementation

[KnockOff<IDbConnection>]
public partial class DbConnectionStubTests
{
}

[KnockOff<IDbCommand>]
public partial class DbCommandStubTests
{
}

[KnockOff<IDbTransaction>]
public partial class DbTransactionStubTests
{
}

[KnockOff<IDataRecord>]
public partial class DataRecordStubTests
{
}

#endregion

#region 20. IAsyncResult (Legacy) - WORKING

[KnockOff<IAsyncResult>]
public partial class AsyncResultStubTests
{
}

#endregion

#region 21. ISerializable / IXmlSerializable - WORKING

[KnockOff<ISerializable>]
public partial class SerializableStubTests
{
}

[KnockOff<IXmlSerializable>]
public partial class XmlSerializableStubTests
{
}

#endregion

#region 22. IStructuralEquatable / IStructuralComparable - WORKING (fixed: 'new' keyword added)

[KnockOff<IStructuralEquatable>]
public partial class StructuralEquatableStubTests
{
}

[KnockOff<IStructuralComparable>]
public partial class StructuralComparableStubTests
{
}

#endregion

#region 23. IConvertible - WORKING (fixed: 'new' keyword added for ToString interceptor)

[KnockOff<IConvertible>]
public partial class ConvertibleStubTests
{
}

#endregion

#region 24. IFormatProvider - WORKING

[KnockOff<IFormatProvider>]
public partial class FormatProviderStubTests
{
}

#endregion

#region 25. IAsyncEnumerable<T> / IAsyncEnumerator<T> - WORKING

[KnockOff<IAsyncEnumerable<string>>]
public partial class AsyncEnumerableStringStubTests
{
}

[KnockOff<IAsyncEnumerable<int>>]
public partial class AsyncEnumerableIntStubTests
{
}

[KnockOff<IAsyncEnumerator<string>>]
public partial class AsyncEnumeratorStringStubTests
{
}

#endregion

#region 26. IProgress<T> - WORKING

[KnockOff<IProgress<int>>]
public partial class ProgressIntStubTests
{
}

[KnockOff<IProgress<string>>]
public partial class ProgressStringStubTests
{
}

#endregion

#region 27. ICustomFormatter - WORKING

[KnockOff<ICustomFormatter>]
public partial class CustomFormatterStubTests
{
}

#endregion

#region 28. IDictionaryEnumerator - WORKING

[KnockOff<IDictionaryEnumerator>]
public partial class DictionaryEnumeratorStubTests
{
}

#endregion

#region 29. IGrouping<TKey, TElement> - WORKING

[KnockOff<IGrouping<string, int>>]
public partial class GroupingStringIntStubTests
{
}

[KnockOff<IGrouping<int, User>>]
public partial class GroupingIntUserStubTests
{
}

#endregion

#region 30. IOrderedEnumerable<T> / IOrderedQueryable<T> - WORKING

[KnockOff<IOrderedEnumerable<string>>]
public partial class OrderedEnumerableStringStubTests
{
}

[KnockOff<IOrderedQueryable<User>>]
public partial class OrderedQueryableUserStubTests
{
}

#endregion
