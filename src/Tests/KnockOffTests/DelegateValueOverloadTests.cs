using KnockOff;

namespace KnockOff.Tests;

/// <summary>
/// Tests for delegate stub OnCall(value) overloads.
/// Phase 4 of value-based overloads feature.
/// </summary>
public class DelegateValueOverloadTests
{
	#region Basic Value Return Tests

	[Fact]
	public void OnCall_WithValue_ReturnsConfiguredString()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		stub.Interceptor.OnCall("configured value");

		var result = factory();

		Assert.Equal("configured value", result);
		stub.Interceptor.Verify(Times.Once);
	}

	[Fact]
	public void OnCall_WithValue_ReturnsConfiguredInt()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.IntFactory();
		IntFactory factory = stub;

		stub.Interceptor.OnCall(42);

		var result = factory();

		Assert.Equal(42, result);
		stub.Interceptor.Verify(Times.Once);
	}

	[Fact]
	public void OnCall_WithValue_ReturnsConfiguredBool()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.BoolFactory();
		BoolFactory factory = stub;

		stub.Interceptor.OnCall(true);

		var result = factory();

		Assert.True(result);
	}

	[Fact]
	public void OnCall_WithValue_ReturnsNullWhenDelegateReturnsNullable()
	{
		// Test with NullableStringFactory which returns string?
		var stub = new DelegateValueOverloadTestClass.Stubs.NullableStringFactory();
		NullableStringFactory factory = stub;

		stub.Interceptor.OnCall((string?)null);

		var result = factory();

		Assert.Null(result);
	}

	[Fact]
	public void OnCall_WithValue_RepeatsIndefinitely()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		stub.Interceptor.OnCall("repeated");

		Assert.Equal("repeated", factory());
		Assert.Equal("repeated", factory());
		Assert.Equal("repeated", factory());

		stub.Interceptor.Verify(Times.Exactly(3));
	}

	#endregion

	#region Single Parameter Tests

	[Fact]
	public void OnCall_WithValue_SingleParam_IgnoresArgument()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringTransformer();
		StringTransformer transformer = stub;

		// Value overload ignores the argument and always returns configured value
		stub.Interceptor.OnCall("constant");

		var result1 = transformer("input1");
		var result2 = transformer("input2");

		Assert.Equal("constant", result1);
		Assert.Equal("constant", result2);
	}

	[Fact]
	public void OnCall_WithValue_SingleParam_TracksLastCallArg()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringTransformer();
		StringTransformer transformer = stub;

		stub.Interceptor.OnCall("result");

		transformer("first");
		transformer("second");

		// LastCallArg still tracks even with value overload
		Assert.Equal("second", stub.Interceptor.LastCallArg);
	}

	#endregion

	#region Multiple Parameter Tests

	[Fact]
	public void OnCall_WithValue_MultiParam_IgnoresAllArguments()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.MessageBuilder();
		MessageBuilder builder = stub;

		stub.Interceptor.OnCall("constant message");

		var result = builder("Alice", 30);

		Assert.Equal("constant message", result);
	}

	[Fact]
	public void OnCall_WithValue_MultiParam_TracksLastCallArgs()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.MessageBuilder();
		MessageBuilder builder = stub;

		stub.Interceptor.OnCall("result");

		builder("Alice", 30);
		builder("Bob", 25);

		// LastCallArgs still tracks even with value overload
		Assert.NotNull(stub.Interceptor.LastCallArgs);
		Assert.Equal("Bob", stub.Interceptor.LastCallArgs.Value.name);
		Assert.Equal(25, stub.Interceptor.LastCallArgs.Value.age);
	}

	#endregion

	#region Complex Object Tests

	[Fact]
	public void OnCall_WithValue_ReturnsComplexObject()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.PersonFactory();
		PersonFactory factory = stub;

		var testPerson = new Person { Name = "Test", Age = 42 };
		stub.Interceptor.OnCall(testPerson);

		var result = factory();

		Assert.Same(testPerson, result);
	}

	[Fact]
	public void OnCall_WithValue_ReturnsList()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.ListFactory();
		ListFactory factory = stub;

		var testList = new List<string> { "a", "b", "c" };
		stub.Interceptor.OnCall(testList);

		var result = factory();

		Assert.Same(testList, result);
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public void OnCall_Callback_ClearsValueConfiguration()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		// First configure with value
		stub.Interceptor.OnCall("value");
		Assert.Equal("value", factory());

		// Then configure with callback - should override
		stub.Interceptor.OnCall(() => "callback");
		Assert.Equal("callback", factory());
	}

	[Fact]
	public void OnCall_Value_ClearsCallbackConfiguration()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		// First configure with callback
		stub.Interceptor.OnCall(() => "callback");
		Assert.Equal("callback", factory());

		// Then configure with value - should override
		stub.Interceptor.OnCall("value");
		Assert.Equal("value", factory());
	}

	#endregion

	#region Void Delegate Verification Tests

	// Note: Void delegates (Action, Action<T>) should NOT have OnCall(value) overload
	// This is verified at compile time - attempting to use OnCall(value) would be a compile error

	[Fact]
	public void VoidDelegate_OnCallCallback_StillWorks()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.VoidNotify();
		VoidNotify notify = stub;

		var wasNotified = false;
		stub.Interceptor.OnCall(() => wasNotified = true);

		notify();

		Assert.True(wasNotified);
		stub.Interceptor.Verify(Times.Once);
	}

	[Fact]
	public void VoidDelegateWithParam_OnCallCallback_StillWorks()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.VoidLogger();
		VoidLogger logger = stub;

		string? loggedMessage = null;
		stub.Interceptor.OnCall((msg) => loggedMessage = msg);

		logger("Hello");

		Assert.Equal("Hello", loggedMessage);
		Assert.Equal("Hello", stub.Interceptor.LastCallArg);
	}

	#endregion

	#region Verification Tests

	[Fact]
	public void OnCall_WithValue_Verify_Works()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		stub.Interceptor.OnCall("test");

		factory();

		stub.Interceptor.Verify();
	}

	[Fact]
	public void OnCall_WithValue_VerifyWithTimes_Works()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		stub.Interceptor.OnCall("test");

		factory();
		factory();
		factory();

		stub.Interceptor.Verify(Times.Exactly(3));
	}

	#endregion

	#region Reset Tests

	[Fact]
	public void Reset_PreservesValueConfiguration()
	{
		var stub = new DelegateValueOverloadTestClass.Stubs.StringFactory();
		StringFactory factory = stub;

		stub.Interceptor.OnCall("configured");
		factory();
		factory();

		stub.Interceptor.Verify(Times.Exactly(2));

		// Reset clears tracking but preserves configuration
		stub.Interceptor.Reset();

		stub.Interceptor.Verify(Times.Never);

		// Configuration still works
		var result = factory();
		Assert.Equal("configured", result);
	}

	#endregion
}

#region Test Delegate Definitions

// Simple delegates for testing
public delegate string StringFactory();
public delegate string? NullableStringFactory();
public delegate int IntFactory();
public delegate bool BoolFactory();
public delegate string StringTransformer(string input);
public delegate string MessageBuilder(string name, int age);
public delegate Person PersonFactory();
public delegate List<string> ListFactory();

// Void delegates (should NOT have value overload - verified at compile time)
public delegate void VoidNotify();
public delegate void VoidLogger(string message);

public class Person
{
	public string Name { get; set; } = "";
	public int Age { get; set; }
}

#endregion

#region Test Stub Definition

[KnockOff<StringFactory>]
[KnockOff<NullableStringFactory>]
[KnockOff<IntFactory>]
[KnockOff<BoolFactory>]
[KnockOff<StringTransformer>]
[KnockOff<MessageBuilder>]
[KnockOff<PersonFactory>]
[KnockOff<ListFactory>]
[KnockOff<VoidNotify>]
[KnockOff<VoidLogger>]
public partial class DelegateValueOverloadTestClass
{
}

#endregion
