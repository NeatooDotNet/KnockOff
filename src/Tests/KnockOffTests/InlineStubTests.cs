namespace KnockOff.Tests;

/// <summary>
/// Tests for inline stubs pattern using [KnockOff&lt;T&gt;] attribute.
/// </summary>
public class InlineStubTests
{
	[Fact]
	public void GenericAttribute_GeneratesStubsClass()
	{
		// Verify that the Stubs class exists and contains the interface stub
		var stub = new InlineTestClass.Stubs.ISimpleService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void InlineStub_Property_TracksGetter()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();
		stub.Name.Value = "Test";

		ISimpleService service = stub;
		var name = service.Name;

		Assert.Equal(1, stub.Name.GetCount);
		Assert.Equal("Test", name);
	}

	[Fact]
	public void InlineStub_Property_TracksSetter()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();

		ISimpleService service = stub;
		service.Name = "NewValue";

		Assert.Equal(1, stub.Name.SetCount);
		Assert.Equal("NewValue", stub.Name.LastSetValue);
	}

	[Fact]
	public void InlineStub_Method_TracksCallCount()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();

		ISimpleService service = stub;
		service.DoSomething();
		service.DoSomething();

		Assert.Equal(2, stub.DoSomething.CallCount);
		Assert.True(stub.DoSomething.WasCalled);
	}

	[Fact]
	public void InlineStub_Method_TracksLastArg()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();

		ISimpleService service = stub;
		service.GetValue(42);

		Assert.Equal(42, stub.GetValue.LastCallArg);
	}

	[Fact]
	public void InlineStub_OnCall_ReturnsCustomValue()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();
		stub.GetValue.OnCall = (ko, x) => x * 10;

		ISimpleService service = stub;
		var result = service.GetValue(5);

		Assert.Equal(50, result);
	}

	[Fact]
	public void InlineStub_OnGet_OverridesValue()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();
		stub.Name.Value = "Default";
		stub.Name.OnGet = (ko) => "Override";

		ISimpleService service = stub;
		var name = service.Name;

		Assert.Equal("Override", name);
	}

	[Fact]
	public void InlineStub_Reset_ClearsState()
	{
		var stub = new InlineTestClass.Stubs.ISimpleService();
		stub.GetValue.OnCall = (ko, x) => 100;

		ISimpleService service = stub;
		service.GetValue(1);
		service.GetValue(2);

		stub.GetValue.Reset();

		Assert.Equal(0, stub.GetValue.CallCount);
		Assert.False(stub.GetValue.WasCalled);
		Assert.Null(stub.GetValue.OnCall);
		Assert.Null(stub.GetValue.LastCallArg);
	}

	[Fact]
	public void MultipleInterfaces_GeneratesSeparateStubs()
	{
		var serviceStub = new MultiInterfaceInlineTest.Stubs.ISimpleService();
		var loggerStub = new MultiInterfaceInlineTest.Stubs.ISimpleLogger();

		Assert.NotNull(serviceStub);
		Assert.NotNull(loggerStub);

		ISimpleService service = serviceStub;
		ISimpleLogger logger = loggerStub;

		service.DoSomething();
		logger.Log("test");

		Assert.True(serviceStub.DoSomething.WasCalled);
		Assert.True(loggerStub.Log.WasCalled);
		Assert.Equal("test", loggerStub.Log.LastCallArg);
	}

#if NET9_0_OR_GREATER
	[Fact]
	public void PartialProperty_AutoInstantiated()
	{
		var test = new PartialPropertyTest();

		// Access the auto-instantiated partial property
		var stub = test.Service;
		Assert.NotNull(stub);

		// Verify it's a working stub
		ISimpleService service = stub;
		service.DoSomething();
		Assert.True(stub.DoSomething.WasCalled);
	}
#endif
}

#region Delegate Stub Tests

/// <summary>
/// Test delegate with void return, no parameters.
/// </summary>
public delegate void VoidNoParamDelegate();

/// <summary>
/// Test delegate with void return, one parameter.
/// </summary>
public delegate void VoidOneParamDelegate(string message);

/// <summary>
/// Test delegate with return value and one parameter.
/// </summary>
public delegate int ReturnOneParamDelegate(int input);

/// <summary>
/// Test delegate with multiple parameters.
/// </summary>
public delegate string MultiParamDelegate(string name, int age);

/// <summary>
/// Generic delegate for testing closed generic delegates.
/// </summary>
public delegate T Factory<T>();

/// <summary>
/// Generic delegate with parameter for testing.
/// </summary>
public delegate TResult Converter<TInput, TResult>(TInput input);

/// <summary>
/// Test class with delegate [KnockOff<T>] attributes.
/// </summary>
[KnockOff<VoidNoParamDelegate>]
[KnockOff<VoidOneParamDelegate>]
[KnockOff<ReturnOneParamDelegate>]
[KnockOff<MultiParamDelegate>]
public partial class DelegateInlineTest
{
}

/// <summary>
/// Test class with closed generic delegate attributes.
/// </summary>
[KnockOff<Factory<string>>]
[KnockOff<Converter<int, string>>]
public partial class GenericDelegateInlineTest
{
}

#endregion

#region Delegate Tests

public class DelegateStubTests
{
	[Fact]
	public void DelegateStub_VoidNoParam_Exists()
	{
		var stub = new DelegateInlineTest.Stubs.VoidNoParamDelegate();
		Assert.NotNull(stub);
		Assert.NotNull(stub.Interceptor);
	}

	[Fact]
	public void DelegateStub_VoidNoParam_TracksCallCount()
	{
		var stub = new DelegateInlineTest.Stubs.VoidNoParamDelegate();
		VoidNoParamDelegate del = stub;

		del();
		del();

		Assert.Equal(2, stub.Interceptor.CallCount);
		Assert.True(stub.Interceptor.WasCalled);
	}

	[Fact]
	public void DelegateStub_VoidOneParam_TracksLastCallArg()
	{
		var stub = new DelegateInlineTest.Stubs.VoidOneParamDelegate();
		VoidOneParamDelegate del = stub;

		del("hello");
		del("world");

		Assert.Equal(2, stub.Interceptor.CallCount);
		Assert.Equal("world", stub.Interceptor.LastCallArg);
	}

	[Fact]
	public void DelegateStub_VoidOneParam_OnCall()
	{
		var stub = new DelegateInlineTest.Stubs.VoidOneParamDelegate();
		string? captured = null;
		stub.Interceptor.OnCall = (ko, msg) => captured = msg;

		VoidOneParamDelegate del = stub;
		del("test message");

		Assert.Equal("test message", captured);
	}

	[Fact]
	public void DelegateStub_ReturnOneParam_ReturnsDefault()
	{
		var stub = new DelegateInlineTest.Stubs.ReturnOneParamDelegate();
		ReturnOneParamDelegate del = stub;

		var result = del(42);

		Assert.Equal(0, result); // default(int)
		Assert.Equal(42, stub.Interceptor.LastCallArg);
	}

	[Fact]
	public void DelegateStub_ReturnOneParam_OnCall()
	{
		var stub = new DelegateInlineTest.Stubs.ReturnOneParamDelegate();
		stub.Interceptor.OnCall = (ko, x) => x * 10;

		ReturnOneParamDelegate del = stub;
		var result = del(5);

		Assert.Equal(50, result);
	}

	[Fact]
	public void DelegateStub_MultiParam_TracksLastCallArgs()
	{
		var stub = new DelegateInlineTest.Stubs.MultiParamDelegate();
		MultiParamDelegate del = stub;

		del("Alice", 30);

		Assert.True(stub.Interceptor.WasCalled);
		Assert.NotNull(stub.Interceptor.LastCallArgs);
		Assert.Equal("Alice", stub.Interceptor.LastCallArgs.Value.name);
		Assert.Equal(30, stub.Interceptor.LastCallArgs.Value.age);
	}

	[Fact]
	public void DelegateStub_MultiParam_OnCall()
	{
		var stub = new DelegateInlineTest.Stubs.MultiParamDelegate();
		stub.Interceptor.OnCall = (ko, name, age) => $"{name} is {age} years old";

		MultiParamDelegate del = stub;
		var result = del("Bob", 25);

		Assert.Equal("Bob is 25 years old", result);
	}

	[Fact]
	public void DelegateStub_Reset_ClearsState()
	{
		var stub = new DelegateInlineTest.Stubs.VoidOneParamDelegate();
		stub.Interceptor.OnCall = (ko, msg) => { };

		VoidOneParamDelegate del = stub;
		del("test");

		stub.Interceptor.Reset();

		Assert.Equal(0, stub.Interceptor.CallCount);
		Assert.False(stub.Interceptor.WasCalled);
		Assert.Null(stub.Interceptor.OnCall);
		Assert.Null(stub.Interceptor.LastCallArg);
	}

	[Fact]
	public void DelegateStub_ImplicitConversion()
	{
		var stub = new DelegateInlineTest.Stubs.ReturnOneParamDelegate();
		stub.Interceptor.OnCall = (ko, x) => x + 1;

		// Implicit conversion
		ReturnOneParamDelegate del = stub;

		// Verify it's actually callable
		Assert.Equal(6, del(5));
	}

	[Fact]
	public void DelegateStub_ClosedGeneric_NoParam_Works()
	{
		var stub = new GenericDelegateInlineTest.Stubs.Factory();
		stub.Interceptor.OnCall = (ko) => "generated value";

		Factory<string> del = stub;
		var result = del();

		Assert.Equal("generated value", result);
		Assert.True(stub.Interceptor.WasCalled);
		Assert.Equal(1, stub.Interceptor.CallCount);
	}

	[Fact]
	public void DelegateStub_ClosedGeneric_WithParam_Works()
	{
		var stub = new GenericDelegateInlineTest.Stubs.Converter();
		stub.Interceptor.OnCall = (ko, input) => $"Value: {input}";

		Converter<int, string> del = stub;
		var result = del(42);

		Assert.Equal("Value: 42", result);
		Assert.Equal(42, stub.Interceptor.LastCallArg);
	}

	[Fact]
	public void DelegateStub_ClosedGeneric_Reset_Works()
	{
		var stub = new GenericDelegateInlineTest.Stubs.Converter();
		stub.Interceptor.OnCall = (ko, x) => "test";

		Converter<int, string> del = stub;
		del(1);
		del(2);

		stub.Interceptor.Reset();

		Assert.Equal(0, stub.Interceptor.CallCount);
		Assert.False(stub.Interceptor.WasCalled);
		Assert.Null(stub.Interceptor.OnCall);
		Assert.Null(stub.Interceptor.LastCallArg);
	}
}

#endregion

#region Test Interfaces

/// <summary>
/// Simple interface for testing inline stubs.
/// </summary>
public interface ISimpleService
{
	string Name { get; set; }
	void DoSomething();
	int GetValue(int input);
}

/// <summary>
/// Simple logger interface for testing multiple interfaces.
/// </summary>
public interface ISimpleLogger
{
	void Log(string message);
}

#endregion

#region Inline Stub Test Classes

/// <summary>
/// Test class with [KnockOff&lt;T&gt;] attribute.
/// </summary>
[KnockOff<ISimpleService>]
public partial class InlineTestClass
{
}

/// <summary>
/// Test class with multiple [KnockOff&lt;T&gt;] attributes.
/// </summary>
[KnockOff<ISimpleService>]
[KnockOff<ISimpleLogger>]
public partial class MultiInterfaceInlineTest
{
}

#if NET9_0_OR_GREATER
/// <summary>
/// Test class with partial property auto-instantiation.
/// </summary>
[KnockOff<ISimpleService>]
public partial class PartialPropertyTest
{
	public partial Stubs.ISimpleService Service { get; }
}
#endif

#endregion

#region Class Stub Test Classes

/// <summary>
/// Simple class for testing class stubbing via inheritance.
/// </summary>
public class SimpleService
{
	public SimpleService() { }
	public SimpleService(string name) { Name = name; }

	public virtual string Name { get; set; } = "";
	public virtual int Value { get; set; }

	public virtual void DoWork() { }
	public virtual int Calculate(int x) => x * 2;
	public virtual string Format(string input, int count) => $"{input}: {count}";
}

/// <summary>
/// Abstract class for testing abstract member stubbing.
/// </summary>
public abstract class AbstractRepository
{
	protected AbstractRepository() { }

	public abstract string ConnectionString { get; }
	public abstract void Connect();
	public abstract int Execute(string command);
}

/// <summary>
/// Class with mix of virtual and non-virtual members.
/// </summary>
public class MixedService
{
	public MixedService() { }

	public string NonVirtualProperty { get; set; } = "";
	public virtual string VirtualProperty { get; set; } = "";

	public void NonVirtualMethod() { }
	public virtual void VirtualMethod() { }
}

/// <summary>
/// Test class with [KnockOff&lt;T&gt;] for class stubbing.
/// </summary>
[KnockOff<SimpleService>]
public partial class ClassStubTestClass
{
}

/// <summary>
/// Test class for abstract class stubbing.
/// </summary>
[KnockOff<AbstractRepository>]
public partial class AbstractStubTestClass
{
}

/// <summary>
/// Test class for mixed virtual/non-virtual class stubbing.
/// </summary>
[KnockOff<MixedService>]
public partial class MixedStubTestClass
{
}

#endregion

#region Class Stub Tests

public class ClassStubTests
{
	[Fact]
	public void ClassStub_Exists()
	{
		// The Stubs.SimpleService class should be generated (wrapper with composition)
		var stub = new ClassStubTestClass.Stubs.SimpleService();
		Assert.NotNull(stub);

		// Verify .Object is substitutable for SimpleService
		SimpleService service = stub.Object;
		Assert.NotNull(service);
	}

	[Fact]
	public void ClassStub_Constructor_ChainsToBase()
	{
		// Test constructor with parameters
		var stub = new ClassStubTestClass.Stubs.SimpleService("TestName");

		// The name should be set via base constructor
		Assert.Equal("TestName", stub.Object.Name);
	}

	[Fact]
	public void ClassStub_Property_TracksGetter()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();
		stub.Name.OnGet = (ko) => "Intercepted";

		var name = stub.Object.Name;

		Assert.Equal(1, stub.Name.GetCount);
		Assert.Equal("Intercepted", name);
	}

	[Fact]
	public void ClassStub_Property_TracksSetter()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		stub.Object.Name = "NewValue";

		Assert.Equal(1, stub.Name.SetCount);
		Assert.Equal("NewValue", stub.Name.LastSetValue);
	}

	[Fact]
	public void ClassStub_Property_DelegatesToBase_WhenNoCallback()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		// Set through stub
		stub.Object.Name = "BaseValue";

		// Get should read from base
		var name = stub.Object.Name;

		Assert.Equal("BaseValue", name);
	}

	[Fact]
	public void ClassStub_VoidMethod_TracksCallCount()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		stub.Object.DoWork();
		stub.Object.DoWork();

		Assert.Equal(2, stub.DoWork.CallCount);
		Assert.True(stub.DoWork.WasCalled);
	}

	[Fact]
	public void ClassStub_Method_TracksLastArg()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		stub.Object.Calculate(42);

		Assert.Equal(42, stub.Calculate.LastCallArg);
	}

	[Fact]
	public void ClassStub_Method_OnCall_ReturnsCustomValue()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();
		stub.Calculate.OnCall = (ko, x) => x * 10;

		var result = stub.Object.Calculate(5);

		Assert.Equal(50, result);
	}

	[Fact]
	public void ClassStub_Method_DelegatesToBase_WhenNoCallback()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		// Base implementation: x * 2
		var result = stub.Object.Calculate(5);

		Assert.Equal(10, result);
	}

	[Fact]
	public void ClassStub_MultiParamMethod_TracksLastCallArgs()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		stub.Object.Format("Test", 123);

		Assert.NotNull(stub.Format.LastCallArgs);
		Assert.Equal("Test", stub.Format.LastCallArgs.Value.input);
		Assert.Equal(123, stub.Format.LastCallArgs.Value.count);
	}

	[Fact]
	public void ClassStub_Reset_ClearsState()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();
		stub.Calculate.OnCall = (ko, x) => 100;

		stub.Object.Calculate(1);
		stub.Object.Calculate(2);

		stub.Calculate.Reset();

		Assert.Equal(0, stub.Calculate.CallCount);
		Assert.False(stub.Calculate.WasCalled);
		Assert.Null(stub.Calculate.OnCall);
		Assert.Null(stub.Calculate.LastCallArg);
	}

	[Fact]
	public void ClassStub_ResetInterceptors_ClearsAllState()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService();

		stub.Object.Calculate(1);
		stub.Object.DoWork();
		stub.Object.Name = "Test";

		stub.ResetInterceptors();

		Assert.Equal(0, stub.Calculate.CallCount);
		Assert.Equal(0, stub.DoWork.CallCount);
		Assert.Equal(0, stub.Name.SetCount);
	}

	[Fact]
	public void ClassStub_Substitutability_PassToMethod()
	{
		var stub = new ClassStubTestClass.Stubs.SimpleService("SubstitutedName");
		stub.Calculate.OnCall = (ko, x) => x * 100;

		// Pass the stub.Object to a method expecting SimpleService
		var result = ProcessService(stub.Object);

		Assert.Equal("SubstitutedName: 500", result);
	}

	private static string ProcessService(SimpleService service)
	{
		var calculated = service.Calculate(5);
		return $"{service.Name}: {calculated}";
	}
}

public class AbstractClassStubTests
{
	[Fact]
	public void AbstractStub_Exists()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();
		Assert.NotNull(stub);

		// Verify substitutability via .Object
		AbstractRepository repo = stub.Object;
		Assert.NotNull(repo);
	}

	[Fact]
	public void AbstractStub_Property_ReturnsDefault_WhenNoCallback()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();

		// Abstract property should return default when no callback
		var connectionString = stub.Object.ConnectionString;

		Assert.Null(connectionString);
	}

	[Fact]
	public void AbstractStub_Property_ReturnsCallback_WhenSet()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();
		stub.ConnectionString.OnGet = (ko) => "Server=test";

		var connectionString = stub.Object.ConnectionString;

		Assert.Equal("Server=test", connectionString);
	}

	[Fact]
	public void AbstractStub_VoidMethod_DoesNotThrow_WhenNoCallback()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();

		// Should not throw - abstract void methods just record the call
		stub.Object.Connect();

		Assert.True(stub.Connect.WasCalled);
	}

	[Fact]
	public void AbstractStub_ReturningMethod_ReturnsDefault_WhenNoCallback()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();

		var result = stub.Object.Execute("SELECT 1");

		Assert.Equal(0, result); // default(int)
		Assert.Equal("SELECT 1", stub.Execute.LastCallArg);
	}

	[Fact]
	public void AbstractStub_ReturningMethod_ReturnsCallback_WhenSet()
	{
		var stub = new AbstractStubTestClass.Stubs.AbstractRepository();
		stub.Execute.OnCall = (ko, cmd) => cmd.Length;

		var result = stub.Object.Execute("SELECT 1");

		Assert.Equal(8, result);
	}
}

public class MixedClassStubTests
{
	[Fact]
	public void MixedStub_OnlyVirtualMembers_AreIntercepted()
	{
		var stub = new MixedStubTestClass.Stubs.MixedService();

		// Virtual property should have interceptor on wrapper
		Assert.NotNull(stub.VirtualProperty);

		// Virtual method should have interceptor on wrapper
		Assert.NotNull(stub.VirtualMethod);

		// Non-virtual members don't have interceptors - access through .Object
	}

	[Fact]
	public void MixedStub_VirtualProperty_IsTracked()
	{
		var stub = new MixedStubTestClass.Stubs.MixedService();

		stub.Object.VirtualProperty = "Test";

		Assert.Equal(1, stub.VirtualProperty.SetCount);
	}

	[Fact]
	public void MixedStub_VirtualMethod_IsTracked()
	{
		var stub = new MixedStubTestClass.Stubs.MixedService();

		stub.Object.VirtualMethod();

		Assert.True(stub.VirtualMethod.WasCalled);
	}

	[Fact]
	public void MixedStub_NonVirtualMembers_WorkThroughObject()
	{
		var stub = new MixedStubTestClass.Stubs.MixedService();

		// Non-virtual property works through .Object
		stub.Object.NonVirtualProperty = "BaseValue";
		Assert.Equal("BaseValue", stub.Object.NonVirtualProperty);

		// Non-virtual method works through .Object
		stub.Object.NonVirtualMethod(); // Should not throw
	}
}

#endregion
