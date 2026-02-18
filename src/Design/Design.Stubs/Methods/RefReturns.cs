// -----------------------------------------------------------------------------
// Design.Stubs - Ref Return Behavior
// -----------------------------------------------------------------------------
// This file documents how KnockOff handles ref and ref readonly return types
// on methods, properties, and indexers.
//
// GENERATOR BEHAVIOR: For ref return members, the generator:
// 1. Adds a _refReturnBacking field to the interceptor class
// 2. Generates InvokeRef() (methods) or InvokeRefGet() (properties/indexers)
//    that writes to _refReturnBacking instead of returning directly
// 3. The explicit interface implementation calls InvokeRef/InvokeRefGet then
//    returns ref to the _refReturnBacking field
//
// USER-FACING API: Unchanged. Users still use Return(value), Return(callback),
// Call(callback), sequences, and verification. The difference is entirely
// internal to how the value reaches the caller.
//
// CLASS STUBS: For class stubs (patterns 3, 4, 6, 9), the Impl class override
// uses the IsConfigured-first pattern for virtual members:
//   if (IsConfigured) { InvokeRef; return ref _refReturnBacking; }
//   else { InvokeRef (tracking); return ref base.Member(); }
// Abstract members always use InvokeRef directly.
//
// C# CONSTRAINTS:
// - Ref return properties are always get-only (no setter)
// - Ref return indexers are always get-only (no setter)
// - Async methods cannot return by ref
// - The backing field is mutable even for ref readonly returns (the readonly
//   constraint is on the caller, not the implementation)
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// PATTERN 1: STANDALONE STUB with ref returns
// =============================================================================

[KnockOff]
public partial class RefReturnServiceStub : IRefReturnService
{
}

// =============================================================================
// PATTERN 3: STANDALONE CLASS STUB with ref returns
// =============================================================================

[KnockOffBase<RefReturnBase>]
public partial class RefReturnBaseStub
{
}

// =============================================================================
// INLINE PATTERNS: Patterns 5 (Inline Interface) and 6 (Inline Class)
// =============================================================================

[KnockOff<IRefReturnService>]
[KnockOff<RefReturnBase>]
public partial class RefReturnDemo
{
	// =========================================================================
	// STANDALONE INTERFACE: Ref Return Methods
	// =========================================================================
	// DESIGN DECISION: Return(value) and Return(callback) work identically
	// to non-ref methods. The user never sees the backing field or InvokeRef.

	public void Standalone_RefReturnMethod_ReturnValue()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(42);
		ref int result = ref service.GetValueRef();

		// result points to the interceptor's _refReturnBacking field
		// which holds the value 42
	}

	public void Standalone_RefReturnMethod_ReturnCallback()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Call(() => 100);
		ref int result = ref service.GetValueRef();
		// result == 100
	}

	public void Standalone_RefReadonlyReturnMethod()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRefReadonly.Return(55);
		ref readonly int result = ref service.GetValueRefReadonly();
		// result == 55, but caller cannot write through the ref
	}

	public void Standalone_RefReturnMethod_WithParam()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetItemRef.Call((index) => $"item-{index}");
		ref string result = ref service.GetItemRef(3);
		// result == "item-3"
	}

	// =========================================================================
	// STANDALONE INTERFACE: Ref Return Properties
	// =========================================================================

	public void Standalone_RefReturnProperty()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.RefValue.Get(42);
		ref int result = ref service.RefValue;
		// result == 42
	}

	public void Standalone_RefReadonlyReturnProperty()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.ReadonlyValue.Get(55);
		ref readonly int result = ref service.ReadonlyValue;
		// result == 55
	}

	// =========================================================================
	// STANDALONE INTERFACE: Ref Return Indexers
	// =========================================================================

	public void Standalone_RefReturnIndexer()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.Indexer.Get((index) => index * 10);
		ref int result = ref service[3];
		// result == 30
	}

	// =========================================================================
	// STANDALONE INTERFACE: Mixed Members
	// =========================================================================
	// Normal members coexist with ref return members on the same interface.

	public void Standalone_MixedMembers()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		// Normal method works as usual
		stub.GetNormal.Call((x) => x * 2);
		var normalResult = service.GetNormal(5);
		// normalResult == 10

		// Normal property works as usual
		stub.NormalValue.Get(99);
		var normalProp = service.NormalValue;
		// normalProp == 99

		// Ref return method works alongside
		stub.GetValueRef.Return(42);
		ref int refResult = ref service.GetValueRef();
		// refResult == 42
	}

	// =========================================================================
	// STANDALONE INTERFACE: Verification
	// =========================================================================

	public void Standalone_RefReturn_Verification()
	{
		var stub = new RefReturnServiceStub();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(1);
		ref int r1 = ref service.GetValueRef();
		ref int r2 = ref service.GetValueRef();

		stub.GetValueRef.Verify(Called.Exactly(2));
	}

	// =========================================================================
	// INLINE INTERFACE: Ref Return Methods (Pattern 5)
	// =========================================================================

	public void Inline_RefReturnMethod()
	{
		var stub = new Stubs.IRefReturnService();
		IRefReturnService service = stub;

		stub.GetValueRef.Return(42);
		ref int result = ref service.GetValueRef();
	}

	public void Inline_RefReadonlyReturnMethod()
	{
		var stub = new Stubs.IRefReturnService();
		IRefReturnService service = stub;

		stub.GetValueRefReadonly.Return(55);
		ref readonly int result = ref service.GetValueRefReadonly();
	}

	// =========================================================================
	// STANDALONE CLASS STUB: Ref Return (Pattern 3)
	// =========================================================================
	// DESIGN DECISION: Class stubs use the same InvokeRef + backing field
	// pattern in the Impl class overrides. Virtual members use IsConfigured-first
	// pattern (check IsConfigured before calling InvokeRef), which is consistent
	// with how virtual property overrides already work.

	public void StandaloneClass_AbstractRefReturnMethod()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.GetAbstractRef.Return(42);
		ref int result = ref service.GetAbstractRef();
		// result == 42
	}

	public void StandaloneClass_VirtualRefReturnMethod_Unconfigured()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		// Virtual: unconfigured falls back to base implementation
		ref int result = ref service.GetVirtualRef();
		// result == 42 (base implementation's _virtualMethodBacking)
	}

	public void StandaloneClass_VirtualRefReturnMethod_Configured()
	{
		var stub = new RefReturnBaseStub();
		RefReturnBase service = stub.Object;

		stub.GetVirtualRef.Return(100);
		ref int result = ref service.GetVirtualRef();
		// result == 100 (configured value overrides base)
	}

	// =========================================================================
	// INLINE CLASS STUB: Ref Return (Pattern 6)
	// =========================================================================

	public void InlineClass_AbstractRefReturnMethod()
	{
		var stub = new Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.GetAbstractRef.Return(42);
		ref int result = ref service.GetAbstractRef();
		// result == 42
	}

	public void InlineClass_VirtualRefReturnMethod_FallsBackToBase()
	{
		var stub = new Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		ref int result = ref service.GetVirtualRef();
		// result == 42 (base implementation)
	}

	public void InlineClass_VirtualRefReturnMethod_ConfiguredOverridesBase()
	{
		var stub = new Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.GetVirtualRef.Return(100);
		ref int result = ref service.GetVirtualRef();
		// result == 100
	}

	public void InlineClass_RefReturnProperty()
	{
		var stub = new Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		stub.AbstractRefProperty.Get(42);
		ref int result = ref service.AbstractRefProperty;
		// result == 42
	}

	public void InlineClass_VirtualRefReturnProperty_FallsBackToBase()
	{
		var stub = new Stubs.RefReturnBase();
		RefReturnBase service = stub.Object;

		ref int result = ref service.VirtualRefProperty;
		// result == 77 (base implementation)
	}
}
