namespace KnockOff.Tests;

/// <summary>
/// Tests for standalone class stubs using [KnockOffBase&lt;T&gt;] attribute.
/// Standalone class stubs follow the composition pattern:
/// - User's partial class is the wrapper (holds interceptors)
/// - Nested Impl class inherits from target and delegates to interceptors
/// - .Object property returns the actual target class instance
/// </summary>
public class StandaloneClassStubTests
{
	#region Basic Existence Tests

	[Fact]
	public void StandaloneClassStub_Exists()
	{
		// Standalone class stub should be created
		var stub = new ServiceBaseStub();
		Assert.NotNull(stub);
	}

	[Fact]
	public void StandaloneClassStub_HasObjectProperty()
	{
		var stub = new ServiceBaseStub();

		// .Object should return the actual ServiceBase instance
		Assert.NotNull(stub.Object);
	}

	[Fact]
	public void StandaloneClassStub_ObjectIsTargetType()
	{
		var stub = new ServiceBaseStub();

		// .Object should be assignable to ServiceBase
		ServiceBase service = stub.Object;
		Assert.NotNull(service);
	}

	[Fact]
	public void StandaloneClassStub_ImplementsIKnockOffStub()
	{
		var stub = new ServiceBaseStub();

		Assert.IsAssignableFrom<IKnockOffStub>(stub);
	}

	#endregion

	#region Constructor Forwarding Tests

	[Fact]
	public void StandaloneClassStub_DefaultConstructor_Works()
	{
		var stub = new ServiceBaseStub();

		Assert.NotNull(stub.Object);
		// Default name should be empty or default
		Assert.Equal("", stub.Object.Name);
	}

	[Fact]
	public void StandaloneClassStub_ParameterizedConstructor_ForwardsArgs()
	{
		var stub = new ServiceBaseStub("TestService");

		Assert.Equal("TestService", stub.Object.Name);
	}

	#endregion

	#region Virtual Property Tests

	[Fact]
	public void StandaloneClassStub_VirtualProperty_TracksGetter()
	{
		var stub = new ServiceBaseStub();
		stub.Name.OnGet(() => "Intercepted");

		var name = stub.Object.Name;

		stub.Name.VerifyGet(Times.Once);
		Assert.Equal("Intercepted", name);
	}

	[Fact]
	public void StandaloneClassStub_VirtualProperty_TracksSetter()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Name = "NewValue";

		stub.Name.VerifySet(Times.Once);
		Assert.Equal("NewValue", stub.Name.LastSetValue);
	}

	[Fact]
	public void StandaloneClassStub_VirtualProperty_DelegatesToBase_WhenNoCallback()
	{
		var stub = new ServiceBaseStub("InitialName");

		// Get should read from base when not configured
		var name = stub.Object.Name;

		Assert.Equal("InitialName", name);
		stub.Name.VerifyGet(Times.Once);
	}

	[Fact]
	public void StandaloneClassStub_VirtualProperty_SetThenGet_UsesBase()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Name = "SetValue";
		var name = stub.Object.Name;

		Assert.Equal("SetValue", name);
	}

	#endregion

	#region Virtual Method Tests

	[Fact]
	public void StandaloneClassStub_VirtualMethod_TracksCallCount()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Initialize();
		stub.Object.Initialize();

		stub.Initialize.Verify(Times.Exactly(2));
	}

	[Fact]
	public void StandaloneClassStub_VirtualMethod_DelegatesToBase_WhenNoCallback()
	{
		var stub = new ServiceBaseStub();

		// Initialize is virtual with a base implementation
		stub.Object.Initialize();

		// Should still track the call
		stub.Initialize.Verify(Times.Once);
	}

	[Fact]
	public void StandaloneClassStub_VirtualMethod_OnCall_OverridesBase()
	{
		var stub = new ServiceBaseStub();
		var wasCalled = false;
		stub.Initialize.Call(() => wasCalled = true);

		stub.Object.Initialize();

		Assert.True(wasCalled);
		stub.Initialize.Verify(Times.Once);
	}

	#endregion

	#region Abstract Method Tests

	[Fact]
	public void StandaloneClassStub_AbstractMethod_TracksCallCount()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Execute("command1");
		stub.Object.Execute("command2");

		stub.Execute.Verify(Times.Exactly(2));
	}

	[Fact]
	public void StandaloneClassStub_AbstractMethod_TracksLastArg()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Execute("test-command");

		Assert.Equal("test-command", stub.Execute.LastArg);
	}

	[Fact]
	public void StandaloneClassStub_AbstractMethod_OnCall_ReturnsCustomValue()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => cmd.Length);

		var result = stub.Object.Execute("hello");

		Assert.Equal(5, result);
	}

	[Fact]
	public void StandaloneClassStub_AbstractMethod_NoCallback_ReturnsDefault()
	{
		var stub = new ServiceBaseStub();

		var result = stub.Object.Execute("anything");

		Assert.Equal(0, result); // default(int)
	}

	#endregion

	#region Abstract Property Tests

	[Fact]
	public void StandaloneClassStub_AbstractProperty_ReturnsDefault_WhenNoCallback()
	{
		var stub = new AbstractServiceStub();

		var connectionString = stub.Object.ConnectionString;

		Assert.Null(connectionString);
	}

	[Fact]
	public void StandaloneClassStub_AbstractProperty_ReturnsCallback_WhenSet()
	{
		var stub = new AbstractServiceStub();
		stub.ConnectionString.OnGet(() => "Server=test");

		var connectionString = stub.Object.ConnectionString;

		Assert.Equal("Server=test", connectionString);
	}

	#endregion

	#region Reset Tests

	[Fact]
	public void StandaloneClassStub_Reset_ClearsInterceptorState()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 100);

		stub.Object.Execute("test");
		stub.Object.Execute("test");

		stub.Execute.Reset();

		stub.Execute.Verify(Times.Never);
		Assert.Null(stub.Execute.LastArg);
	}

	[Fact]
	public void StandaloneClassStub_ResetInterceptors_ClearsAllState()
	{
		var stub = new ServiceBaseStub();

		stub.Object.Execute("cmd");
		stub.Object.Initialize();
		stub.Object.Name = "Test";

		stub.ResetInterceptors();

		stub.Execute.Verify(Times.Never);
		stub.Initialize.Verify(Times.Never);
		stub.Name.VerifySet(Times.Never);
	}

	#endregion

	#region Substitutability Tests

	[Fact]
	public void StandaloneClassStub_Substitutability_PassToMethod()
	{
		var stub = new ServiceBaseStub("TestService");
		stub.Execute.Return((cmd) => cmd.Length * 10);

		// Pass stub.Object to a method expecting ServiceBase
		var result = ProcessService(stub.Object);

		Assert.Equal("TestService: 50", result);
	}

	private static string ProcessService(ServiceBase service)
	{
		var calculated = service.Execute("hello");
		return $"{service.Name}: {calculated}";
	}

	#endregion

	#region Strict Mode Tests

	[Fact]
	public void StandaloneClassStub_Strict_ThrowsOnUnconfiguredMethod()
	{
		var stub = new ServiceBaseStub();
		stub.Strict = true;

		Assert.Throws<StubException>(() => stub.Object.Execute("test"));
	}

	[Fact]
	public void StandaloneClassStub_Strict_DefaultIsFalse()
	{
		var stub = new ServiceBaseStub();

		Assert.False(stub.Strict);
	}

	#endregion

	#region Custom User Methods Tests

	[Fact]
	public void StandaloneClassStub_UserCanAddCustomMethods()
	{
		var stub = new ServiceBaseStub();

		stub.MarkUsed();

		Assert.True(stub.WasUsed);
	}

	#endregion

	#region Stub-Level Verify Tests

	[Fact]
	public void StandaloneClassStub_Verify_PassesWhenVerifiablesCalled()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 42).Verifiable();

		stub.Object.Execute("test");

		stub.Verify(); // Should not throw
	}

	[Fact]
	public void StandaloneClassStub_Verify_ThrowsWhenVerifiableNotCalled()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 42).Verifiable();
		// Don't call Execute

		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void StandaloneClassStub_VerifyAll_PassesWhenAllCalled()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 42);

		stub.Object.Execute("test");

		stub.VerifyAll(); // Should not throw - all configured interceptors called
	}

	#endregion

	#region Sequence Tests

	[Fact]
	public void StandaloneClassStub_Sequence_ReturnsValuesInOrder()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 1).ThenReturn((cmd) => 2).ThenReturn((cmd) => 3);

		Assert.Equal(1, stub.Object.Execute("a"));
		Assert.Equal(2, stub.Object.Execute("b"));
		Assert.Equal(3, stub.Object.Execute("c"));
	}

	[Fact]
	public void StandaloneClassStub_Sequence_RepeatsLastValue()
	{
		var stub = new ServiceBaseStub();
		stub.Execute.Return((cmd) => 1).ThenReturn((cmd) => 99);

		Assert.Equal(1, stub.Object.Execute("a"));
		Assert.Equal(99, stub.Object.Execute("b"));
		Assert.Equal(99, stub.Object.Execute("c")); // Repeats last
	}

	#endregion

	#region Verifiable Marking Tests

	[Fact]
	public void StandaloneClassStub_Verifiable_MarksForVerification()
	{
		var stub = new ServiceBaseStub();
		stub.Name.OnGet(() => "test").Verifiable();
		stub.Execute.Return((cmd) => 42).Verifiable();

		_ = stub.Object.Name;
		stub.Object.Execute("cmd");

		stub.Verify(); // Both verifiables satisfied
	}

	#endregion
}

#region Abstract Service Tests

public class AbstractStandaloneClassStubTests
{
	[Fact]
	public void AbstractStub_Exists()
	{
		var stub = new AbstractServiceStub();
		Assert.NotNull(stub);

		// Verify substitutability via .Object
		AbstractServiceBase service = stub.Object;
		Assert.NotNull(service);
	}

	[Fact]
	public void AbstractStub_VoidMethod_DoesNotThrow_WhenNoCallback()
	{
		var stub = new AbstractServiceStub();

		// Should not throw - abstract void methods just record the call
		stub.Object.Connect();

		stub.Connect.Verify();
	}

	[Fact]
	public void AbstractStub_ReturningMethod_ReturnsDefault_WhenNoCallback()
	{
		var stub = new AbstractServiceStub();

		var result = stub.Object.ExecuteQuery("SELECT 1");

		Assert.Equal(0, result); // default(int)
		Assert.Equal("SELECT 1", stub.ExecuteQuery.LastArg);
	}

	[Fact]
	public void AbstractStub_ReturningMethod_ReturnsCallback_WhenSet()
	{
		var stub = new AbstractServiceStub();
		stub.ExecuteQuery.Return((query) => query.Length);

		var result = stub.Object.ExecuteQuery("SELECT 1");

		Assert.Equal(8, result);
	}
}

#endregion

#region Event Tests

public class StandaloneClassEventTests
{
	[Fact]
	public void StandaloneClassStub_Event_CanSubscribe()
	{
		var stub = new EventServiceStub();
		var eventFired = false;

		stub.Object.StatusChanged += (s, e) => eventFired = true;

		// Raise the event via interceptor
		stub.StatusChanged.Raise(stub.Object, EventArgs.Empty);

		Assert.True(eventFired);
	}

	[Fact]
	public void StandaloneClassStub_Event_TracksSubscription()
	{
		var stub = new EventServiceStub();

		stub.Object.StatusChanged += (s, e) => { };
		stub.Object.StatusChanged += (s, e) => { };

		stub.StatusChanged.VerifyAdd(Times.Exactly(2));
	}

	[Fact]
	public void StandaloneClassStub_Event_TracksUnsubscription()
	{
		var stub = new EventServiceStub();
		EventHandler handler = (s, e) => { };

		stub.Object.StatusChanged += handler;
		stub.Object.StatusChanged -= handler;

		stub.StatusChanged.VerifyAdd(Times.Once);
		stub.StatusChanged.VerifyRemove(Times.Once);
	}
}

#endregion

#region Indexer Tests

public class StandaloneClassIndexerTests
{
	[Fact]
	public void StandaloneClassStub_Indexer_TracksGetter()
	{
		var stub = new IndexerServiceStub();
		stub.Indexer.OnGet((key) => $"value_{key}");

		var result = stub.Object["test"];

		Assert.Equal("value_test", result);
		stub.Indexer.VerifyGet(Times.Once);
	}

	[Fact]
	public void StandaloneClassStub_Indexer_TracksSetter()
	{
		var stub = new IndexerServiceStub();

		stub.Object["key1"] = "new_value";

		stub.Indexer.VerifySet(Times.Once);
	}

	[Fact]
	public void StandaloneClassStub_Indexer_DelegatesToBase_WhenNoCallback()
	{
		var stub = new IndexerServiceStub();

		stub.Object["key1"] = "stored";
		var result = stub.Object["key1"];

		Assert.Equal("stored", result);
	}
}

#endregion

#region Test Classes

/// <summary>
/// Target class with virtual and abstract members for testing standalone class stubs.
/// </summary>
public abstract class ServiceBase
{
	public ServiceBase() { }
	public ServiceBase(string name) { Name = name; }

	public virtual string Name { get; set; } = "";

	public abstract int Execute(string command);

	public virtual void Initialize() { /* base implementation */ }
}

/// <summary>
/// Standalone class stub for ServiceBase.
/// </summary>
[KnockOffBase<ServiceBase>]
public partial class ServiceBaseStub
{
	// User can add custom methods or state to standalone stubs
	public bool WasUsed { get; set; }

	public void MarkUsed()
	{
		WasUsed = true;
	}
}

/// <summary>
/// Abstract class for testing abstract member stubbing.
/// </summary>
public abstract class AbstractServiceBase
{
	protected AbstractServiceBase() { }

	public abstract string? ConnectionString { get; }
	public abstract void Connect();
	public abstract int ExecuteQuery(string query);
}

/// <summary>
/// Standalone class stub for AbstractServiceBase.
/// </summary>
[KnockOffBase<AbstractServiceBase>]
public partial class AbstractServiceStub
{
}

/// <summary>
/// Class with virtual event for testing event stubbing.
/// </summary>
public abstract class EventServiceBase
{
	public abstract event EventHandler? StatusChanged;

	public virtual void DoWork() { }
}

/// <summary>
/// Standalone class stub for EventServiceBase.
/// </summary>
[KnockOffBase<EventServiceBase>]
public partial class EventServiceStub
{
}

/// <summary>
/// Class with virtual indexer for testing indexer stubbing.
/// </summary>
public abstract class IndexerServiceBase
{
	private readonly Dictionary<string, string> _data = new();

	public virtual string this[string key]
	{
		get => _data.TryGetValue(key, out var value) ? value : "";
		set => _data[key] = value;
	}
}

/// <summary>
/// Standalone class stub for IndexerServiceBase.
/// </summary>
[KnockOffBase<IndexerServiceBase>]
public partial class IndexerServiceStub
{
}

#endregion
