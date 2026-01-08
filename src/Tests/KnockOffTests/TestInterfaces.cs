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
	protected int GetValue(int input) => input * 2;
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
	protected Task<int> GetValueAsync(int input) => Task.FromResult(input * 3);
	protected ValueTask<int> GetValueValueTaskAsync(int input) => new(input * 4);
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
}

[KnockOff]
public partial class RefParameterServiceKnockOff : IRefParameterService
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
	protected string GetData(int id) => $"Data-{id}";
}

[KnockOff]
public partial class KeyLookupKnockOff : IKeyLookup
{
	protected int GetData(string key) => key.Length;
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
