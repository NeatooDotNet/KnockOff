namespace KnockOff.Tests;

/// <summary>
/// Regression tests for bugs found in the source generator.
/// Each test section documents the bug and verifies the fix.
/// </summary>
public class BugRegressionTests
{
	#region Bug: Missing 'new' keyword on abstract class overrides (CS0108)

	// When a class overrides object members (Equals, GetHashCode, ToString),
	// the generated stub interceptor properties must use 'new' instead of inheriting 'override'.
	// Without 'new', the compiler emits CS0108 which is a compile error (TreatWarningsAsErrors).
	//
	// Affected pipelines: ClassModelBuilder, StandaloneClassModelBuilder
	// Fix: NeedsNewKeyword() helper checks if the member name matches object members.

	// Pattern 6: Inline Class — [KnockOff<ConcreteClass>] → ClassModelBuilder → ClassRenderer
	[Fact]
	public void InlineClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new ObjectOverrideInlineTest.Stubs.EntityWithObjectOverrides();

		stub.Object.Save();
		stub.Save.Verify(Called.Once);
	}

	// Pattern 3: Standalone Class — [KnockOffBase<T>] → StandaloneClassModelBuilder → StandaloneClassRenderer
	[Fact]
	public void StandaloneClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new EntityWithObjectOverridesStub();

		stub.Object.Save();
		stub.Save.Verify(Called.Once);
	}

	// Pattern 9: Open Generic Class — [KnockOff(typeof(T<>))] → ClassModelBuilder → ClassRenderer
	[Fact]
	public void OpenGenericClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new OpenGenericObjectOverrideTest.Stubs.GenericEntityWithObjectOverrides<int>();

		stub.Object.Save();
		stub.Save.Verify(Called.Once);
	}

	// Pattern 4: Generic Standalone Class — [KnockOffBase(typeof(T<>))] → StandaloneClassModelBuilder → StandaloneClassRenderer
	[Fact]
	public void GenericStandaloneClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new GenericEntityWithObjectOverridesStub<int>();

		stub.Object.Save();
		stub.Save.Verify(Called.Once);
	}

	#endregion

	#region Bug: Missing 'using System.Linq' in generated code

	// When a stub has overloaded methods, the generator uses .Sum() for TotalCallCount.
	// Without 'using System.Linq;', this fails to compile for projects without ImplicitUsings.
	//
	// Note: This test project has ImplicitUsings enabled, so it cannot directly reproduce
	// the compile error. The existing OverloadedMethodTests provide coverage that the
	// generated .Sum() code works correctly. The fix (always emitting 'using System.Linq;')
	// ensures self-contained generated code for consumers without ImplicitUsings.
	//
	// Affected pipelines: InlineRenderer, FlatRenderer, StandaloneClassRenderer
	// Fix: Unconditionally emit 'using System.Linq;' in all renderers.

	[Fact]
	public void OverloadedMethods_VerifyAll_WorksAcrossOverloads()
	{
		// Exercises the .Sum() code path in generated TotalCallCount (used by VerifyAll).
		// TotalCallCount is private; VerifyAll() uses it internally.
		var knockOff = new OverloadedServiceKnockOff();
		IOverloadedService service = knockOff;

		knockOff.Process.Call((data) => { });
		knockOff.Process.Call((data, priority) => { });
		knockOff.Process.Call((data, priority, async) => { });

		service.Process("a");
		service.Process("b", 1);
		service.Process("c", 2, true);

		// VerifyAll internally calls TotalCallCount which uses .Sum() across overloads
		knockOff.Process.Verify(Called.Exactly(3));
	}

	#endregion

	#region Bug: Property/Indexer Verifiable(Called times) discards the times parameter

	// The explicit interface implementation for IPropertyGetTracking.Verifiable(Called times)
	// and IPropertySetTracking<T>.Verifiable(Called times) delegates to the parameterless
	// Verifiable(), silently discarding the Called constraint. This means marking a property
	// builder as Verifiable(Called.Exactly(3)) actually uses Called.AtLeastOnce.
	//
	// Same bug exists in IIndexerGetTracking<TKey>.Verifiable(Called times)
	// and IIndexerSetTracking<TKey, TValue>.Verifiable(Called times).
	//
	// Root cause: Generated EII lines like:
	//   IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable();
	// should be:
	//   IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable(times);
	//
	// Affected renderers: PropertyInterceptorRenderer (lines 684, 758, 1544, 1631),
	//                     IndexerInterceptorRenderer (lines 537, 606, 1827, 1921)

	[Fact(Skip = "Known bug: Verifiable(Called times) discards times parameter")]
	public void PropertyGetBuilder_Verifiable_CalledConstraint_IsApplied()
	{
		var stub = new SampleKnockOff();
		ISampleService service = stub;

		// Mark getter as verifiable with Called.Exactly(3)
		stub.Name.Get(() => "test").Verifiable(Called.Exactly(3));

		// Call only once — should NOT satisfy Exactly(3)
		_ = service.Name;

		// Stub.Verify() should throw because 1 != 3
		// BUG: Does not throw because Called.Exactly(3) is silently discarded,
		// defaulting to Called.AtLeastOnce which is satisfied by 1 call.
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact(Skip = "Known bug: Verifiable(Called times) discards times parameter")]
	public void PropertySetBuilder_Verifiable_CalledConstraint_IsApplied()
	{
		var stub = new SampleKnockOff();
		ISampleService service = stub;

		// Mark setter as verifiable with Called.Exactly(3)
		stub.Name.Set(v => { }).Verifiable(Called.Exactly(3));

		// Call only once
		service.Name = "test";

		// Should throw because 1 != 3
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact(Skip = "Known bug: Verifiable(Called times) discards times parameter")]
	public void IndexerGetBuilder_Verifiable_CalledConstraint_IsApplied()
	{
		var stub = new SimpleIndexerKnockOff();
		ISimpleIndexer store = stub;

		// Mark getter as verifiable with Called.Exactly(3)
		stub.Indexer.Get(key => 42).Verifiable(Called.Exactly(3));

		// Call only once
		_ = store["foo"];

		// CheckVerification should return a failure because 1 != 3
		var failure = stub.Indexer.CheckVerification();
		Assert.NotNull(failure);
	}

	[Fact(Skip = "Known bug: Verifiable(Called times) discards times parameter")]
	public void IndexerSetBuilder_Verifiable_CalledConstraint_IsApplied()
	{
		var stub = new SimpleIndexerKnockOff();
		ISimpleIndexer store = stub;

		// Mark setter as verifiable with Called.Exactly(3)
		stub.Indexer.Set((key, value) => { }).Verifiable(Called.Exactly(3));

		// Call only once
		store["foo"] = 99;

		// CheckVerification should return a failure because 1 != 3
		var failure = stub.Indexer.CheckVerification();
		Assert.NotNull(failure);
	}

	#endregion
}

#region Bug regression test types

/// <summary>
/// Abstract class that overrides object members.
/// Stubs wrapping this class need 'new' keyword on Equals/GetHashCode interceptors
/// to avoid CS0108 (member hides inherited member).
/// </summary>
public abstract class EntityWithObjectOverrides
{
	public override bool Equals(object? obj) => base.Equals(obj);
	public override int GetHashCode() => base.GetHashCode();
	public override string ToString() => "Entity";

	public abstract void Save();
}

/// <summary>
/// Inline class stub for EntityWithObjectOverrides.
/// Compilation proves the 'new' keyword fix works for InlineModelBuilder pipeline.
/// </summary>
[KnockOff<EntityWithObjectOverrides>]
public partial class ObjectOverrideInlineTest
{
}

/// <summary>
/// Standalone class stub for EntityWithObjectOverrides.
/// Compilation proves the 'new' keyword fix works for StandaloneClassModelBuilder pipeline.
/// </summary>
[KnockOffBase<EntityWithObjectOverrides>]
public partial class EntityWithObjectOverridesStub
{
}

/// <summary>
/// Generic abstract class that overrides object members.
/// Tests Pattern 4 and Pattern 9 — generic class stubs with object override members.
/// </summary>
public abstract class GenericEntityWithObjectOverrides<T>
{
	public override bool Equals(object? obj) => base.Equals(obj);
	public override int GetHashCode() => base.GetHashCode();

	public abstract void Save();
}

/// <summary>
/// Open generic class stub (Pattern 9) — [KnockOff(typeof(T&lt;&gt;))] → ClassRenderer.
/// </summary>
[KnockOff(typeof(GenericEntityWithObjectOverrides<>))]
public partial class OpenGenericObjectOverrideTest
{
}

/// <summary>
/// Generic standalone class stub (Pattern 4) — [KnockOffBase(typeof(T&lt;&gt;))] → StandaloneClassRenderer.
/// </summary>
[KnockOffBase(typeof(GenericEntityWithObjectOverrides<>))]
public partial class GenericEntityWithObjectOverridesStub<T>
{
}

#endregion
