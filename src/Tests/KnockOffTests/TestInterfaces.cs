namespace KnockOff.Tests;

#region Basic Test Types

public interface ISampleService
{
	string Name { get; set; }
	void DoSomething();
	int GetValue(int input);
	void Calculate(string name, int value, bool flag);
	string? GetOptional();
}

[KnockOff]
public partial class SampleKnockOff : ISampleService
{
	protected override int GetValue_(int input) => input * 2;
}

#endregion

#region Multi-Interface Test Types (for inline stubs only)

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or create separate stubs per interface.

public interface ILogger
{
	void Log(string message);
	string Name { get; set; }
}

public interface IAuditor
{
	void Log(string message); // Same signature as ILogger.Log
	void Audit(string action, int userId);
}

public interface INotifier
{
	void Notify(string recipient);
	string Name { get; } // Same name but get-only (vs ILogger which is get/set)
}

// Separate single-interface stubs (the recommended pattern)
[KnockOff]
public partial class LoggerKnockOff : ILogger
{
}

[KnockOff]
public partial class AuditorKnockOff : IAuditor
{
}

[KnockOff]
public partial class NotifierKnockOff : INotifier
{
}

#endregion

#region Async Test Types

public interface IAsyncService
{
	Task DoWorkAsync();
	Task<int> GetValueAsync(int input);
	Task<string?> GetOptionalAsync();
	Task<string> GetRequiredAsync();
	ValueTask DoWorkValueTaskAsync();
	ValueTask<int> GetValueValueTaskAsync(int input);
}

[KnockOff]
public partial class AsyncServiceKnockOff : IAsyncService
{
	protected override Task<int> GetValueAsync_(int input) => Task.FromResult(input * 3);
	protected override ValueTask<int> GetValueValueTaskAsync_(int input) => new(input * 4);
}

#endregion

#region Generic Interface Test Types

public interface IRepository<T> where T : class
{
	T? GetById(int id);
	void Save(T entity);
	Task<T?> GetByIdAsync(int id);
}

[KnockOff]
public partial class UserRepositoryKnockOff : IRepository<User>
{
}

public class User
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

#endregion

#region Interface Inheritance Test Types

public interface IBaseEntity
{
	int Id { get; }
	DateTime CreatedAt { get; }
}

public interface IAuditableEntity : IBaseEntity
{
	DateTime? ModifiedAt { get; set; }
	string ModifiedBy { get; set; }
}

[KnockOff]
public partial class AuditableEntityKnockOff : IAuditableEntity
{
}

#endregion

#region Indexer Test Types

public class PropertyInfo
{
	public string Name { get; set; } = "";
	public string Value { get; set; } = "";
}

public interface IPropertyStore
{
	PropertyInfo? this[string key] { get; }
}

public interface IReadWriteStore
{
	PropertyInfo? this[string key] { get; set; }
}

[KnockOff]
public partial class PropertyStoreKnockOff : IPropertyStore
{
}

[KnockOff]
public partial class ReadWriteStoreKnockOff : IReadWriteStore
{
}

public interface ISimpleIndexer
{
	int this[string key] { get; set; }
}

public interface IMatrixIndexer
{
	double this[int row, int col] { get; set; }
}

[KnockOff]
public partial class SimpleIndexerKnockOff : ISimpleIndexer
{
}

[KnockOff]
public partial class MatrixIndexerKnockOff : IMatrixIndexer
{
}

#endregion

#region Event Test Types

public interface IEventSource
{
	event EventHandler<string> MessageReceived;
	event EventHandler OnCompleted;
	event Action<int> OnProgress;
	event Action<string, int> OnData;
}

[KnockOff]
public partial class EventSourceKnockOff : IEventSource
{
}

#endregion

#region Method Overload Test Types

public interface IOverloadedService
{
	// Method overloads - same name, different signatures
	Task<User?> GetByIdAsync(int id);
	Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

	// More overloads
	void Process(string data);
	void Process(string data, int priority);
	void Process(string data, int priority, bool async);

	// Return type overloads with different params
	int Calculate(int value);
	int Calculate(int a, int b);
}

[KnockOff]
public partial class OverloadedServiceKnockOff : IOverloadedService
{
}

#endregion

#region Out Parameter Test Types

public interface IOutParameterService
{
	bool TryGetValue(string key, out string? value);
	bool TryParse(string input, out int result);
	void GetData(out string name, out int count);
}

[KnockOff]
public partial class OutParameterServiceKnockOff : IOutParameterService
{
}

#endregion

#region Ref Parameter Test Types

public interface IRefParameterService
{
	void Increment(ref int value);
	bool TryUpdate(string key, ref string value);
	void Swap(ref int a, ref int b);
}

[KnockOff]
public partial class RefParameterServiceKnockOff : IRefParameterService
{
}

#endregion

#region Mixed Ref/Out Parameter Test Types

public interface IMixedRefOutService
{
	bool Process(string input, out string output, ref int counter);
	int Transform(string input, out string transformed, out bool wasModified);
}

[KnockOff]
public partial class MixedRefOutServiceKnockOff : IMixedRefOutService
{
}

#endregion

#region Conflicting Signature Test Types

/// <summary>
/// Interface with GetData returning string based on int id.
/// </summary>
public interface IDataProvider
{
	string GetData(int id);
	int Count { get; }
}

/// <summary>
/// Interface with GetData returning int based on string key.
/// Different signature and return type than IDataProvider.GetData.
/// </summary>
public interface IKeyLookup
{
	int GetData(string key);
	int Count { get; set; }
}

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use separate single-interface stubs instead.

[KnockOff]
public partial class DataProviderKnockOff : IDataProvider
{
	protected override string GetData_(int id) => $"Data-{id}";
}

[KnockOff]
public partial class KeyLookupKnockOff : IKeyLookup
{
	protected override int GetData_(string key) => key.Length;
}

#endregion

#region KO Property Collision Test Types

/// <summary>
/// Interface with a property named the same as the interface.
/// Tests KO property naming collision detection.
/// </summary>
public interface ICollision
{
	string ICollision { get; set; }
	void DoWork();
}

/// <summary>
/// The generated KO property "ICollision" would collide with the member "ICollision".
/// Generator should detect this and use a different name (e.g., "ICollision_").
/// </summary>
[KnockOff]
public partial class CollisionKnockOff : ICollision
{
}

#endregion

#region Smart Defaults Test Types

/// <summary>
/// Simple class with parameterless constructor for testing NewInstance strategy.
/// </summary>
public class TestEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

/// <summary>
/// Interface testing smart default return values for various types.
/// </summary>
public interface ISmartDefaultsService
{
	// Value types - should return default (0, false, etc.)
	int GetInt();
	bool GetBool();
	DateTime GetDateTime();

	// Nullable reference types - should return default (null)
	string? GetNullableString();
	TestEntity? GetNullableEntity();

	// Non-nullable with parameterless constructor - should return new T()
	List<string> GetList();
	Dictionary<string, int> GetDictionary();
	TestEntity GetEntity();
	IList<string> GetIList();

	// Non-nullable without parameterless constructor - should throw
	string GetString();

	// Interface return type - should throw (can't instantiate)
	IDisposable GetDisposable();

	// Task<T> variants
	Task<int> GetIntAsync();
	Task<List<string>> GetListAsync();
	Task<string> GetStringAsync();

	// Properties with various types
	int Count { get; }
	List<string> Items { get; }
}

[KnockOff]
public partial class SmartDefaultsKnockOff : ISmartDefaultsService
{
}

#endregion

#region Custom Collection Interface Test Types (inherits from IList)

/// <summary>
/// Test interface that inherits from IList&lt;T&gt;.
/// Used to test that .Source(List&lt;T&gt;) properly delegates GetEnumerator
/// even when C# overload resolution picks IList&lt;T&gt; over IEnumerable&lt;T&gt;.
/// </summary>
public interface ICustomStepList : IList<string>
{
	/// <summary>
	/// Custom method beyond IList members.
	/// </summary>
	void AddRange(IEnumerable<string> items);
}

[KnockOff]
public partial class CustomStepListKnockOff : ICustomStepList
{
}

/// <summary>
/// Inline stub test class for ICustomStepList.
/// This tests the InlineModelBuilder path specifically.
/// </summary>
[KnockOff<ICustomStepList>]
public partial class CustomStepListInlineStubTests
{
}

#endregion

#region Generic Method Test Types

/// <summary>
/// Interface with generic methods for testing Of&lt;T&gt;() pattern.
/// </summary>
public interface IGenericMethodService
{
	// Basic generic return type
	T Create<T>() where T : new();

	// Generic parameter
	void Process<T>(T value);

	// Generic return with non-generic parameter
	T Deserialize<T>(string json);

	// Multiple type parameters
	TOut Convert<TIn, TOut>(TIn input);

	// Generic with nullable return
	T? Find<T>(int id) where T : class;

	// Void with multiple generic params
	void Transfer<TSource, TDest>(TSource source, TDest destination);
}

/// <summary>
/// Entity interface for constraint testing.
/// </summary>
public interface IEntity
{
	int Id { get; set; }
}

/// <summary>
/// Test entity that implements IEntity and has parameterless constructor.
/// </summary>
public class TestEntityWithInterface : IEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = "";
}

/// <summary>
/// Interface with constrained generic methods.
/// </summary>
public interface IConstrainedGenericService
{
	T CreateEntity<T>() where T : class, IEntity, new();
	void SaveEntity<T>(T entity) where T : IEntity;
}

[KnockOff]
public partial class GenericMethodServiceKnockOff : IGenericMethodService
{
}

[KnockOff]
public partial class ConstrainedGenericServiceKnockOff : IConstrainedGenericService
{
}

#endregion

#region Array Parameter Overload Test Types

/// <summary>
/// Interface with method overloads that use array parameters.
/// Tests that GetTypeSuffix correctly handles array brackets in type names,
/// producing valid identifiers like "StringArray" instead of "string[]".
/// </summary>
public interface IArrayParamOverloadService
{
	IReadOnlyList<string> GetItems();
	IReadOnlyList<string> GetItems(string[] filters);
	IReadOnlyList<string> GetItems(string[] filters, int maxCount);
}

[KnockOff]
public partial class ArrayParamOverloadKnockOff : IArrayParamOverloadService
{
}

/// <summary>
/// Inline stub test class for array parameter overloads.
/// Tests the InlineModelBuilder path.
/// </summary>
[KnockOff<IArrayParamOverloadService>]
public partial class ArrayParamOverloadInlineTests
{
}

#endregion

#region Parameter Type Suffix Bug Fix Test Types

/// <summary>
/// Interface with method overloads that exercise GetTypeSuffix edge cases.
/// Each overload uses a parameter type that previously produced invalid identifiers.
/// </summary>
public interface IParamTypeSuffixService
{
	// Bug 1: Nullable inside generics (embedded ? produces invalid identifier)
	void Process(List<string?> data);

	// Bug 2: Tuples (parentheses produce invalid identifier)
	void Process((int, string) pair);

	// Bug 3: Multidimensional arrays ([,] not handled)
	void Process(int[,] matrix);

	// Bug 4: Array of nullable elements (? survives to keyword switch)
	void Process(string?[] items);

	// Bug 5: nint/nuint (missing from keyword map)
	void Process(nint value);
	void Process(nuint value);

	// Non-overloaded method for basic verification
	string GetName();
}

[KnockOff]
public partial class ParamTypeSuffixKnockOff : IParamTypeSuffixService
{
}

/// <summary>
/// Inline stub test class for parameter type suffix bug fixes.
/// </summary>
[KnockOff<IParamTypeSuffixService>]
public partial class ParamTypeSuffixInlineTests
{
}

#endregion

#region Ref Return Test Types

/// <summary>
/// Interface with ref return methods for testing ref return support.
/// </summary>
public interface IRefReturnMethodService
{
	ref int GetValueRef();
	ref readonly int GetValueRefReadonly();
	ref string GetItemRef(int index);
	ref readonly string GetItemRefReadonly(int index);
}

/// <summary>
/// Interface with ref return properties (get-only, C# constraint).
/// </summary>
public interface IRefReturnPropertyService
{
	ref int Value { get; }
	ref readonly int ReadonlyValue { get; }
}

/// <summary>
/// Interface with ref return indexers.
/// </summary>
public interface IRefReturnIndexerService
{
	ref int this[int index] { get; }
	ref readonly int this[string key] { get; }
}

/// <summary>
/// Mix of normal + ref return members.
/// Tests whether ref return members break generation of adjacent normal members.
/// </summary>
public interface IMixedRefReturnService
{
	int NormalValue { get; set; }
	int GetNormal(int input);
	ref int GetValueRef();
	ref readonly int ReadonlyValue { get; }
}

[KnockOff]
public partial class RefReturnMethodServiceKnockOff : IRefReturnMethodService
{
}

[KnockOff]
public partial class RefReturnPropertyServiceKnockOff : IRefReturnPropertyService
{
}

[KnockOff]
public partial class RefReturnIndexerServiceKnockOff : IRefReturnIndexerService
{
}

[KnockOff]
public partial class MixedRefReturnServiceKnockOff : IMixedRefReturnService
{
}

/// <summary>
/// Abstract base class with ref return members for testing class stub patterns.
/// Covers abstract and virtual methods, properties, and indexers with ref/ref readonly returns.
/// </summary>
public abstract class RefReturnServiceBase
{
	// Abstract ref return method
	public abstract ref int GetValueRef();

	// Abstract ref readonly return method
	public abstract ref readonly int GetValueRefReadonly();

	// Virtual ref return method with base implementation
	private int _virtualBacking = 99;
	public virtual ref int GetVirtualValueRef() => ref _virtualBacking;

	// Abstract ref return property
	public abstract ref int RefProperty { get; }

	// Abstract ref readonly return property
	public abstract ref readonly int RefReadonlyProperty { get; }

	// Virtual ref return property with base implementation
	private int _virtualPropBacking = 77;
	public virtual ref int VirtualRefProperty => ref _virtualPropBacking;

	// Abstract ref return indexer
	public abstract ref int this[int index] { get; }

	// Non-ref member to verify mixed generation works
	public abstract string Name { get; }
}

[KnockOffBase<RefReturnServiceBase>]
public partial class RefReturnServiceBaseKnockOff
{
}

#endregion
