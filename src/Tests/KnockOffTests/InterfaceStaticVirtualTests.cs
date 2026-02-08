using KnockOff;

namespace KnockOff.Tests;

/// <summary>
/// Reproduction tests for static virtual/abstract interface members.
/// The generator should skip static members and only stub instance members.
/// </summary>
public class InterfaceStaticVirtualTests
{
	[Fact]
	public void InlineStub_InstanceMethod_Works()
	{
		var stub = new StaticVirtualInlineTest.Stubs.IHaveStaticVirtuals();
		IHaveStaticVirtuals service = stub;

		var result = service.InstanceLift();

		stub.InstanceLift.Verify(Called.Once);
	}

	[Fact]
	public void InlineStub_InstanceProperty_Works()
	{
		var stub = new StaticVirtualInlineTest.Stubs.IHaveStaticVirtuals();
		stub.InstancePush.Get("TestValue");

		IHaveStaticVirtuals service = stub;
		var result = service.InstancePush;

		Assert.Equal("TestValue", result);
		stub.InstancePush.VerifyGet(Called.Once);
	}

	[Fact]
	public void InlineStub_InstancePropertySetter_Works()
	{
		var stub = new StaticVirtualInlineTest.Stubs.IHaveStaticVirtuals();

		IHaveStaticVirtuals service = stub;
		service.InstancePush = "NewValue";

		stub.InstancePush.VerifySet(Called.Once);
		Assert.Equal("NewValue", stub.InstancePush.LastSetValue);
	}

	[Fact]
	public void StandaloneStub_InstanceMethod_Works()
	{
		var stub = new StaticVirtualStandaloneStub();
		stub.InstanceLift.Return("Lifted");

		IHaveStaticVirtuals service = stub;
		var result = service.InstanceLift();

		Assert.Equal("Lifted", result);
		stub.InstanceLift.Verify(Called.Once);
	}

	[Fact]
	public void StandaloneStub_InstanceProperty_Works()
	{
		var stub = new StaticVirtualStandaloneStub();
		stub.InstancePush.Get("StandaloneValue");

		IHaveStaticVirtuals service = stub;
		var result = service.InstancePush;

		Assert.Equal("StandaloneValue", result);
		stub.InstancePush.VerifyGet(Called.Once);
	}
}

#region Test Interfaces

/// <summary>
/// Interface with both instance and static virtual members.
/// The generator must skip the static members and only stub instance members.
/// </summary>
public interface IHaveStaticVirtuals
{
	string InstanceLift();
	string? InstancePush { get; set; }

	static virtual string StaticLift() => "Lift";
	static virtual string? StaticPush { get; set; }
}

/// <summary>
/// Interface with only static virtual members and no instance members.
/// The generator should produce a stub with no interceptors.
/// </summary>
public interface IOnlyStaticVirtuals
{
	static virtual string StaticOnly() => "Static";
	static virtual int StaticProp { get; set; }
}

#endregion

#region Stub Definitions

[KnockOff<IHaveStaticVirtuals>]
public partial class StaticVirtualInlineTest
{
}

[KnockOff<IOnlyStaticVirtuals>]
public partial class OnlyStaticVirtualInlineTest
{
}

[KnockOff]
public partial class StaticVirtualStandaloneStub : IHaveStaticVirtuals
{
}

#endregion
