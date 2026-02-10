using System.Diagnostics.CodeAnalysis;

namespace KnockOff.Tests;

// =============================================================================
// Gap #18: [AllowNull] attribute not preserved on property setters
//
// When a property has [AllowNull], null can be assigned to a non-nullable
// string property. The generated stub must preserve this attribute on the
// setter, otherwise the compiler emits CS8625.
// =============================================================================

public interface IAllowNullProperty
{
	[AllowNull]
	string NewLine { get; set; }
}

public class AllowNullPropertyClass
{
	[AllowNull]
	public virtual string NewLine { get; set; } = "";
}

// Interface stubs (may already work — generator has SetterHasAllowNull)
[KnockOff<IAllowNullProperty>]
public partial class AllowNullInlineTest { }

[KnockOff]
public partial class AllowNullStandaloneKnockOff : IAllowNullProperty { }

// Class stubs (likely broken — ClassModelBuilder has no AllowNull handling)
[KnockOff<AllowNullPropertyClass>]
public partial class AllowNullClassInlineTest { }

[KnockOffBase<AllowNullPropertyClass>]
public partial class AllowNullClassStandaloneStub { }

// =============================================================================
// Gap #19: [DoesNotReturn] attribute not propagated to method overrides
//
// When a base class method has [DoesNotReturn], the generated override must
// also have it, otherwise CS8770. Only applies to class stubs.
// =============================================================================

public class DoesNotReturnClass
{
	[DoesNotReturn]
	public virtual void VoidMethod() => throw new NotSupportedException();

	[DoesNotReturn]
	public virtual int IntMethod() => throw new NotSupportedException();
}

// Class stubs — both inline and standalone
[KnockOff<DoesNotReturnClass>]
public partial class DoesNotReturnInlineTest { }

[KnockOffBase<DoesNotReturnClass>]
public partial class DoesNotReturnStandaloneStub { }

// =============================================================================
// Gap #11: [SetsRequiredMembers] not copied to generated constructors
//
// When a base class has required members and a [SetsRequiredMembers]
// constructor, the derived Impl class constructor must also have
// [SetsRequiredMembers], otherwise CS9039.
// =============================================================================

public class RequiredMembersClass
{
	public required string Name { get; set; }

	[SetsRequiredMembers]
	public RequiredMembersClass()
	{
		Name = "";
	}

	public virtual void DoWork() { }
}

// Class stubs — both inline and standalone
[KnockOff<RequiredMembersClass>]
public partial class RequiredMembersInlineTest { }

[KnockOffBase<RequiredMembersClass>]
public partial class RequiredMembersStandaloneStub { }

// =============================================================================
// Tests
// =============================================================================

public class AttributeGapTests
{
	// =========================================================================
	// Gap #18: [AllowNull] — Interface stubs
	// =========================================================================

	[Fact]
	public void Gap18_Inline_Interface_AllowNull_SetNull()
	{
		var stub = new AllowNullInlineTest.Stubs.IAllowNullProperty();
		IAllowNullProperty svc = stub;

		// [AllowNull] allows null assignment to non-nullable string property
		svc.NewLine = null;

		stub.NewLine.VerifySet(Called.Once);
	}

	[Fact]
	public void Gap18_Standalone_Interface_AllowNull_SetNull()
	{
		var stub = new AllowNullStandaloneKnockOff();
		IAllowNullProperty svc = stub;

		svc.NewLine = null;

		stub.NewLine.VerifySet(Called.Once);
	}

	// =========================================================================
	// Gap #18: [AllowNull] — Class stubs
	// =========================================================================

	[Fact]
	public void Gap18_Inline_Class_AllowNull_SetNull()
	{
		var stub = new AllowNullClassInlineTest.Stubs.AllowNullPropertyClass();
		AllowNullPropertyClass obj = stub.Object;

		obj.NewLine = null;

		stub.NewLine.VerifySet(Called.Once);
	}

	[Fact]
	public void Gap18_Standalone_Class_AllowNull_SetNull()
	{
		var stub = new AllowNullClassStandaloneStub();
		AllowNullPropertyClass obj = stub.Object;

		obj.NewLine = null;

		stub.NewLine.VerifySet(Called.Once);
	}

	// =========================================================================
	// Gap #19: [DoesNotReturn] — Class stubs
	// =========================================================================

	[Fact]
	public void Gap19_Inline_DoesNotReturn_VoidMethod_CallsBase()
	{
		var stub = new DoesNotReturnInlineTest.Stubs.DoesNotReturnClass();
		DoesNotReturnClass obj = stub.Object;

		// Unconfigured method should call base which throws NotSupportedException
		Assert.Throws<NotSupportedException>(() => obj.VoidMethod());
	}

	[Fact]
	public void Gap19_Inline_DoesNotReturn_VoidMethod_WithCallback()
	{
		var stub = new DoesNotReturnInlineTest.Stubs.DoesNotReturnClass();
		stub.VoidMethod.Call(() => throw new NotImplementedException());

		DoesNotReturnClass obj = stub.Object;

		Assert.Throws<NotImplementedException>(() => obj.VoidMethod());
	}

	[Fact]
	public void Gap19_Inline_DoesNotReturn_IntMethod_CallsBase()
	{
		var stub = new DoesNotReturnInlineTest.Stubs.DoesNotReturnClass();
		DoesNotReturnClass obj = stub.Object;

		Assert.Throws<NotSupportedException>(() => obj.IntMethod());
	}

	[Fact]
	public void Gap19_Inline_DoesNotReturn_IntMethod_WithReturn()
	{
		var stub = new DoesNotReturnInlineTest.Stubs.DoesNotReturnClass();
		stub.IntMethod.Return(42);

		DoesNotReturnClass obj = stub.Object;

		// Configured to return value — should succeed
		Assert.Equal(42, obj.IntMethod());
	}

	[Fact]
	public void Gap19_Standalone_DoesNotReturn_VoidMethod_CallsBase()
	{
		var stub = new DoesNotReturnStandaloneStub();
		DoesNotReturnClass obj = stub.Object;

		Assert.Throws<NotSupportedException>(() => obj.VoidMethod());
	}

	[Fact]
	public void Gap19_Standalone_DoesNotReturn_VoidMethod_WithCallback()
	{
		var stub = new DoesNotReturnStandaloneStub();
		stub.VoidMethod.Call(() => throw new NotImplementedException());

		DoesNotReturnClass obj = stub.Object;

		Assert.Throws<NotImplementedException>(() => obj.VoidMethod());
	}

	[Fact]
	public void Gap19_Standalone_DoesNotReturn_IntMethod_CallsBase()
	{
		var stub = new DoesNotReturnStandaloneStub();
		DoesNotReturnClass obj = stub.Object;

		Assert.Throws<NotSupportedException>(() => obj.IntMethod());
	}

	[Fact]
	public void Gap19_Standalone_DoesNotReturn_IntMethod_WithReturn()
	{
		var stub = new DoesNotReturnStandaloneStub();
		stub.IntMethod.Return(42);

		DoesNotReturnClass obj = stub.Object;

		Assert.Equal(42, obj.IntMethod());
	}

	// =========================================================================
	// Gap #11: [SetsRequiredMembers] — Class stubs
	// =========================================================================

	[Fact]
	public void Gap11_Inline_RequiredMembers_CanCreate()
	{
		var stub = new RequiredMembersInlineTest.Stubs.RequiredMembersClass();
		RequiredMembersClass obj = stub.Object;

		Assert.NotNull(obj);
	}

	[Fact]
	public void Gap11_Inline_RequiredMembers_MethodWorks()
	{
		var stub = new RequiredMembersInlineTest.Stubs.RequiredMembersClass();
		stub.DoWork.Call(() => { });

		RequiredMembersClass obj = stub.Object;
		obj.DoWork();

		stub.DoWork.Verify(Called.Once);
	}

	[Fact]
	public void Gap11_Standalone_RequiredMembers_CanCreate()
	{
		var stub = new RequiredMembersStandaloneStub();
		RequiredMembersClass obj = stub.Object;

		Assert.NotNull(obj);
	}

	[Fact]
	public void Gap11_Standalone_RequiredMembers_MethodWorks()
	{
		var stub = new RequiredMembersStandaloneStub();
		stub.DoWork.Call(() => { });

		RequiredMembersClass obj = stub.Object;
		obj.DoWork();

		stub.DoWork.Verify(Called.Once);
	}
}
