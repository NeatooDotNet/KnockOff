namespace KnockOff.Tests;

#region Inline Stub Declarations

[KnockOff<IRefReturnMethodService>]
public partial class RefReturnMethodInlineTest
{
}

[KnockOff<IRefReturnPropertyService>]
public partial class RefReturnPropertyInlineTest
{
}

[KnockOff<IRefReturnIndexerService>]
public partial class RefReturnIndexerInlineTest
{
}

[KnockOff<IMixedRefReturnService>]
public partial class MixedRefReturnInlineTest
{
}

#endregion

/// <summary>
/// Exploratory tests for ref return support in KnockOff.
/// Tests both standalone ([KnockOff]) and inline ([KnockOff&lt;T&gt;]) patterns.
/// Expected: compilation failures because the generator doesn't preserve ref return modifiers.
/// </summary>
public class RefReturnTests
{
	#region Standalone - Ref Return Methods

	[Fact]
	public void Standalone_RefReturnMethod_Compiles()
	{
		var knockOff = new RefReturnMethodServiceKnockOff();
		IRefReturnMethodService service = knockOff;

		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Standalone_RefReturnMethod_GetValueRef_CanBeCalledAndReturnsRef()
	{
		var knockOff = new RefReturnMethodServiceKnockOff();
		IRefReturnMethodService service = knockOff;

		ref int result = ref service.GetValueRef();
		knockOff.GetValueRef.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_RefReturnMethod_GetValueRefReadonly_CanBeCalled()
	{
		var knockOff = new RefReturnMethodServiceKnockOff();
		IRefReturnMethodService service = knockOff;

		ref readonly int result = ref service.GetValueRefReadonly();
		knockOff.GetValueRefReadonly.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_RefReturnMethod_WithParam_GetItemRef()
	{
		var knockOff = new RefReturnMethodServiceKnockOff();
		IRefReturnMethodService service = knockOff;

		ref string result = ref service.GetItemRef(0);

		knockOff.GetItemRef.Verify(Called.Once);
		Assert.Equal(0, knockOff.GetItemRef.LastArg);
	}

	[Fact]
	public void Standalone_RefReturnMethod_WithParam_GetItemRefReadonly()
	{
		var knockOff = new RefReturnMethodServiceKnockOff();
		IRefReturnMethodService service = knockOff;

		ref readonly string result = ref service.GetItemRefReadonly(3);

		knockOff.GetItemRefReadonly.Verify(Called.Once);
		Assert.Equal(3, knockOff.GetItemRefReadonly.LastArg);
	}

	#endregion

	#region Standalone - Ref Return Properties

	[Fact]
	public void Standalone_RefReturnProperty_Compiles()
	{
		var knockOff = new RefReturnPropertyServiceKnockOff();
		IRefReturnPropertyService service = knockOff;

		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Standalone_RefReturnProperty_Value_ReturnsRef()
	{
		var knockOff = new RefReturnPropertyServiceKnockOff();
		IRefReturnPropertyService service = knockOff;

		ref int value = ref service.Value;
		knockOff.Value.VerifyGet(Called.Once);
	}

	[Fact]
	public void Standalone_RefReturnProperty_ReadonlyValue_ReturnsRefReadonly()
	{
		var knockOff = new RefReturnPropertyServiceKnockOff();
		IRefReturnPropertyService service = knockOff;

		ref readonly int value = ref service.ReadonlyValue;
		knockOff.ReadonlyValue.VerifyGet(Called.Once);
	}

	#endregion

	#region Standalone - Ref Return Indexers

	[Fact]
	public void Standalone_RefReturnIndexer_Compiles()
	{
		var knockOff = new RefReturnIndexerServiceKnockOff();
		IRefReturnIndexerService service = knockOff;

		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Standalone_RefReturnIndexer_IntKey_ReturnsRef()
	{
		var knockOff = new RefReturnIndexerServiceKnockOff();
		IRefReturnIndexerService service = knockOff;

		ref int value = ref service[0];
		knockOff.Indexer.VerifyGet(Called.Once);
	}

	[Fact]
	public void Standalone_RefReturnIndexer_StringKey_ReturnsRefReadonly()
	{
		var knockOff = new RefReturnIndexerServiceKnockOff();
		IRefReturnIndexerService service = knockOff;

		ref readonly int value = ref service["key"];
		knockOff.IndexerString.VerifyGet(Called.Once);
	}

	#endregion

	#region Standalone - Mixed Interface (normal + ref return members)

	[Fact]
	public void Standalone_Mixed_Compiles()
	{
		var knockOff = new MixedRefReturnServiceKnockOff();
		IMixedRefReturnService service = knockOff;

		Assert.NotNull(knockOff);
	}

	[Fact]
	public void Standalone_Mixed_NormalProperty_StillWorks()
	{
		var knockOff = new MixedRefReturnServiceKnockOff();
		IMixedRefReturnService service = knockOff;

		knockOff.NormalValue.Get(42);
		var result = service.NormalValue;

		Assert.Equal(42, result);
		knockOff.NormalValue.VerifyGet(Called.Once);
	}

	[Fact]
	public void Standalone_Mixed_NormalMethod_StillWorks()
	{
		var knockOff = new MixedRefReturnServiceKnockOff();
		IMixedRefReturnService service = knockOff;

		knockOff.GetNormal.Return((x) => x * 2);
		var result = service.GetNormal(5);

		Assert.Equal(10, result);
		knockOff.GetNormal.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_Mixed_RefReturnMethod_Works()
	{
		var knockOff = new MixedRefReturnServiceKnockOff();
		IMixedRefReturnService service = knockOff;

		ref int result = ref service.GetValueRef();
		knockOff.GetValueRef.Verify(Called.Once);
	}

	[Fact]
	public void Standalone_Mixed_RefReadonlyProperty_Works()
	{
		var knockOff = new MixedRefReturnServiceKnockOff();
		IMixedRefReturnService service = knockOff;

		ref readonly int value = ref service.ReadonlyValue;
		knockOff.ReadonlyValue.VerifyGet(Called.Once);
	}

	#endregion

	#region Inline - Ref Return Methods

	[Fact]
	public void Inline_RefReturnMethod_Compiles()
	{
		var stub = new RefReturnMethodInlineTest.Stubs.IRefReturnMethodService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void Inline_RefReturnMethod_GetValueRef_CanBeCalled()
	{
		var stub = new RefReturnMethodInlineTest.Stubs.IRefReturnMethodService();
		IRefReturnMethodService service = stub;

		ref int result = ref service.GetValueRef();
		stub.GetValueRef.Verify(Called.Once);
	}

	[Fact]
	public void Inline_RefReturnMethod_GetValueRefReadonly_CanBeCalled()
	{
		var stub = new RefReturnMethodInlineTest.Stubs.IRefReturnMethodService();
		IRefReturnMethodService service = stub;

		ref readonly int result = ref service.GetValueRefReadonly();
		stub.GetValueRefReadonly.Verify(Called.Once);
	}

	[Fact]
	public void Inline_RefReturnMethod_WithParam_GetItemRef()
	{
		var stub = new RefReturnMethodInlineTest.Stubs.IRefReturnMethodService();
		IRefReturnMethodService service = stub;

		ref string result = ref service.GetItemRef(0);

		stub.GetItemRef.Verify(Called.Once);
		Assert.Equal(0, stub.GetItemRef.LastArg);
	}

	[Fact]
	public void Inline_RefReturnMethod_WithParam_GetItemRefReadonly()
	{
		var stub = new RefReturnMethodInlineTest.Stubs.IRefReturnMethodService();
		IRefReturnMethodService service = stub;

		ref readonly string result = ref service.GetItemRefReadonly(3);

		stub.GetItemRefReadonly.Verify(Called.Once);
		Assert.Equal(3, stub.GetItemRefReadonly.LastArg);
	}

	#endregion

	#region Inline - Ref Return Properties

	[Fact]
	public void Inline_RefReturnProperty_Compiles()
	{
		var stub = new RefReturnPropertyInlineTest.Stubs.IRefReturnPropertyService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void Inline_RefReturnProperty_Value_ReturnsRef()
	{
		var stub = new RefReturnPropertyInlineTest.Stubs.IRefReturnPropertyService();
		IRefReturnPropertyService service = stub;

		ref int value = ref service.Value;
		stub.Value.VerifyGet(Called.Once);
	}

	[Fact]
	public void Inline_RefReturnProperty_ReadonlyValue_ReturnsRefReadonly()
	{
		var stub = new RefReturnPropertyInlineTest.Stubs.IRefReturnPropertyService();
		IRefReturnPropertyService service = stub;

		ref readonly int value = ref service.ReadonlyValue;
		stub.ReadonlyValue.VerifyGet(Called.Once);
	}

	#endregion

	#region Inline - Ref Return Indexers

	[Fact]
	public void Inline_RefReturnIndexer_Compiles()
	{
		var stub = new RefReturnIndexerInlineTest.Stubs.IRefReturnIndexerService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void Inline_RefReturnIndexer_IntKey_ReturnsRef()
	{
		var stub = new RefReturnIndexerInlineTest.Stubs.IRefReturnIndexerService();
		IRefReturnIndexerService service = stub;

		ref int value = ref service[0];
		stub.Indexer.VerifyGet(Called.Once);
	}

	[Fact]
	public void Inline_RefReturnIndexer_StringKey_ReturnsRefReadonly()
	{
		var stub = new RefReturnIndexerInlineTest.Stubs.IRefReturnIndexerService();
		IRefReturnIndexerService service = stub;

		ref readonly int value = ref service["key"];
		stub.IndexerString.VerifyGet(Called.Once);
	}

	#endregion

	#region Inline - Mixed Interface

	[Fact]
	public void Inline_Mixed_Compiles()
	{
		var stub = new MixedRefReturnInlineTest.Stubs.IMixedRefReturnService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void Inline_Mixed_NormalProperty_StillWorks()
	{
		var stub = new MixedRefReturnInlineTest.Stubs.IMixedRefReturnService();
		IMixedRefReturnService service = stub;

		stub.NormalValue.Get(42);
		var result = service.NormalValue;

		Assert.Equal(42, result);
		stub.NormalValue.VerifyGet(Called.Once);
	}

	[Fact]
	public void Inline_Mixed_NormalMethod_StillWorks()
	{
		var stub = new MixedRefReturnInlineTest.Stubs.IMixedRefReturnService();
		IMixedRefReturnService service = stub;

		stub.GetNormal.Return((x) => x * 2);
		var result = service.GetNormal(5);

		Assert.Equal(10, result);
		stub.GetNormal.Verify(Called.Once);
	}

	[Fact]
	public void Inline_Mixed_RefReturnMethod_Works()
	{
		var stub = new MixedRefReturnInlineTest.Stubs.IMixedRefReturnService();
		IMixedRefReturnService service = stub;

		ref int result = ref service.GetValueRef();
		stub.GetValueRef.Verify(Called.Once);
	}

	[Fact]
	public void Inline_Mixed_RefReadonlyProperty_Works()
	{
		var stub = new MixedRefReturnInlineTest.Stubs.IMixedRefReturnService();
		IMixedRefReturnService service = stub;

		ref readonly int value = ref service.ReadonlyValue;
		stub.ReadonlyValue.VerifyGet(Called.Once);
	}

	#endregion
}
