using Design.Domain.Abstractions;
using Design.Domain.Services;
using Design.Stubs.Methods;
using KnockOff;

#pragma warning disable CA1859 // Tests intentionally use interface types to verify ref return through interface dispatch

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for ref return support across all applicable patterns.
/// Exercises standalone interface, standalone class, inline interface,
/// and inline class patterns for ref return methods, properties, and indexers.
/// </summary>
public class RefReturnTests
{
	#region Standalone Interface - Methods

	[Fact]
	public void Standalone_RefReturnMethod_Return_Value()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(42);
		ref int result = ref service.GetValueRef();

		Assert.Equal(42, result);
		stub.GetValueRef.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_RefReturnMethod_Return_Callback()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(() => 100);
		ref int result = ref service.GetValueRef();

		Assert.Equal(100, result);
	}

	[Fact]
	public void Standalone_RefReadonlyReturnMethod_Return_Value()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRefReadonly.Return(55);
		ref readonly int result = ref service.GetValueRefReadonly();

		Assert.Equal(55, result);
	}

	[Fact]
	public void Standalone_RefReturnMethod_WithParam_Return_Callback()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetItemRef.Return((index) => $"item-{index}");
		ref string result = ref service.GetItemRef(3);

		Assert.Equal("item-3", result);
		Assert.Equal(3, stub.GetItemRef.LastArg);
	}

	[Fact]
	public void Standalone_RefReturnMethod_Verification()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(1);
		ref int r1 = ref service.GetValueRef();
		ref int r2 = ref service.GetValueRef();

		stub.GetValueRef.Verify(Called.Exactly(2));
	}

	#endregion

	#region Standalone Interface - Properties

	[Fact]
	public void Standalone_RefReturnProperty_Get_Value()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.RefValue.Get(42);
		ref int result = ref service.RefValue;

		Assert.Equal(42, result);
		stub.RefValue.VerifyGet(Called.Once);
	}

	[Fact]
	public void Standalone_RefReadonlyReturnProperty_Get_Value()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.ReadonlyValue.Get(55);
		ref readonly int result = ref service.ReadonlyValue;

		Assert.Equal(55, result);
		stub.ReadonlyValue.VerifyGet(Called.Once);
	}

	#endregion

	#region Standalone Interface - Indexers

	[Fact]
	public void Standalone_RefReturnIndexer_Get_Callback()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.Indexer.Get((index) => index * 10);
		ref int result = ref service[3];

		Assert.Equal(30, result);
		stub.Indexer.VerifyGet(Called.Once);
	}

	#endregion

	#region Standalone Interface - Mixed Members

	[Fact]
	public void Standalone_MixedMembers_NormalAndRefCoexist()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		// Configure normal method
		stub.GetNormal.Return((x) => x * 2);

		// Configure ref return method
		stub.GetValueRef.Return(42);

		// Configure normal property
		stub.NormalValue.Get(99);

		// Both work correctly
		Assert.Equal(10, service.GetNormal(5));

		ref int refResult = ref service.GetValueRef();
		Assert.Equal(42, refResult);

		Assert.Equal(99, service.NormalValue);
	}

	#endregion

	#region Inline Interface - Methods

	[Fact]
	public void Inline_RefReturnMethod_Return_Value()
	{
		var stub = new RefReturnDemo.Stubs.IRefReturnService();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(42);
		ref int result = ref service.GetValueRef();

		Assert.Equal(42, result);
	}

	[Fact]
	public void Inline_RefReadonlyReturnMethod_Return_Value()
	{
		var stub = new RefReturnDemo.Stubs.IRefReturnService();
		IRefReturnService service = stub;

		stub.GetValueRefReadonly.Return(55);
		ref readonly int result = ref service.GetValueRefReadonly();

		Assert.Equal(55, result);
	}

	#endregion

	#region Standalone Class - Abstract Methods

	[Fact]
	public void StandaloneClass_AbstractRefReturnMethod_Return_Value()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.GetAbstractRef.Return(42);
		ref int result = ref service.GetAbstractRef();

		Assert.Equal(42, result);
		stub.GetAbstractRef.Verify(Called.Once);
	}

	[Fact]
	public void StandaloneClass_AbstractRefReadonlyReturnMethod_Return_Value()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.GetAbstractRefReadonly.Return(55);
		ref readonly int result = ref service.GetAbstractRefReadonly();

		Assert.Equal(55, result);
	}

	#endregion

	#region Standalone Class - Virtual Methods

	[Fact]
	public void StandaloneClass_VirtualRefReturnMethod_FallsBackToBase()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		// Virtual method -- unconfigured should fall back to base
		ref int result = ref service.GetVirtualRef();
		Assert.Equal(42, result);
	}

	[Fact]
	public void StandaloneClass_VirtualRefReturnMethod_ConfiguredOverridesBase()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.GetVirtualRef.Return(100);
		ref int result = ref service.GetVirtualRef();

		Assert.Equal(100, result);
		stub.GetVirtualRef.Verify(Called.Once);
	}

	#endregion

	#region Standalone Class - Properties

	[Fact]
	public void StandaloneClass_AbstractRefReturnProperty()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.AbstractRefProperty.Get(42);
		ref int result = ref service.AbstractRefProperty;

		Assert.Equal(42, result);
		stub.AbstractRefProperty.VerifyGet(Called.Once);
	}

	[Fact]
	public void StandaloneClass_VirtualRefReturnProperty_FallsBackToBase()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		ref int result = ref service.VirtualRefProperty;
		Assert.Equal(77, result);
	}

	#endregion

	#region Standalone Class - Indexers

	[Fact]
	public void StandaloneClass_AbstractRefReturnIndexer()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.Indexer.Get((index) => index * 10);
		ref int result = ref service[3];

		Assert.Equal(30, result);
	}

	#endregion

	#region Standalone Class - Mixed Members

	[Fact]
	public void StandaloneClass_MixedMembers_NormalAndRefCoexist()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.Name.Get("TestService");
		stub.GetAbstractRef.Return(42);

		Assert.Equal("TestService", service.Name);

		ref int refResult = ref service.GetAbstractRef();
		Assert.Equal(42, refResult);
	}

	#endregion

	#region Inline Class - Abstract Methods

	[Fact]
	public void InlineClass_AbstractRefReturnMethod_Return_Value()
	{
		var stub = new RefReturnDemo.Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.GetAbstractRef.Return(42);
		ref int result = ref service.GetAbstractRef();

		Assert.Equal(42, result);
	}

	#endregion

	#region Inline Class - Virtual Methods

	[Fact]
	public void InlineClass_VirtualRefReturnMethod_FallsBackToBase()
	{
		var stub = new RefReturnDemo.Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		ref int result = ref service.GetVirtualRef();
		Assert.Equal(42, result);
	}

	[Fact]
	public void InlineClass_VirtualRefReturnMethod_ConfiguredOverridesBase()
	{
		var stub = new RefReturnDemo.Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.GetVirtualRef.Return(100);
		ref int result = ref service.GetVirtualRef();

		Assert.Equal(100, result);
	}

	#endregion

	#region Inline Class - Properties

	[Fact]
	public void InlineClass_AbstractRefReturnProperty()
	{
		var stub = new RefReturnDemo.Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.AbstractRefProperty.Get(42);
		ref int result = ref service.AbstractRefProperty;

		Assert.Equal(42, result);
	}

	[Fact]
	public void InlineClass_VirtualRefReturnProperty_FallsBackToBase()
	{
		var stub = new RefReturnDemo.Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		ref int result = ref service.VirtualRefProperty;
		Assert.Equal(77, result);
	}

	#endregion
}
