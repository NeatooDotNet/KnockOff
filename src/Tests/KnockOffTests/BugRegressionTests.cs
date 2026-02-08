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
		stub.Save.Verify(Times.Once);
	}

	// Pattern 3: Standalone Class — [KnockOffBase<T>] → StandaloneClassModelBuilder → StandaloneClassRenderer
	[Fact]
	public void StandaloneClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new EntityWithObjectOverridesStub();

		stub.Object.Save();
		stub.Save.Verify(Times.Once);
	}

	// Pattern 9: Open Generic Class — [KnockOff(typeof(T<>))] → ClassModelBuilder → ClassRenderer
	[Fact]
	public void OpenGenericClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new OpenGenericObjectOverrideTest.Stubs.GenericEntityWithObjectOverrides<int>();

		stub.Object.Save();
		stub.Save.Verify(Times.Once);
	}

	// Pattern 4: Generic Standalone Class — [KnockOffBase(typeof(T<>))] → StandaloneClassModelBuilder → StandaloneClassRenderer
	[Fact]
	public void GenericStandaloneClassStub_WithObjectOverrides_Compiles()
	{
		var stub = new GenericEntityWithObjectOverridesStub<int>();

		stub.Object.Save();
		stub.Save.Verify(Times.Once);
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
		knockOff.Process.Verify(Times.Exactly(3));
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
