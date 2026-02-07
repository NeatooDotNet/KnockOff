namespace KnockOff.Tests;

/// <summary>
/// Tests that value-based overloads work with all three KnockOff patterns:
/// 1. Standalone: [KnockOff] on a class implementing an interface
/// 2. Inline Interface: [KnockOff&lt;IFoo&gt;] generating a stub class
/// 3. Inline Class: [KnockOff&lt;SomeClass&gt;] generating a stub for virtual members
///
/// Phase 5 of value-based overloads feature.
///
/// Note: Inline class stubs (Pattern 3) use a simpler interceptor pattern that
/// does NOT include value overloads for methods. Only callback syntax is supported.
/// Property value overloads (Get(value)) ARE supported for inline class stubs.
/// </summary>
public partial class ThreePatternValueOverloadTests
{
	#region Pattern 1: Standalone [KnockOff]

	[Fact]
	public void Standalone_MethodOnCallValue_Works()
	{
		var knockOff = new StandaloneServiceKnockOff();
		IThreePatternService service = knockOff;

		knockOff.GetName.Return("standalone result");

		Assert.Equal("standalone result", service.GetName());
	}

	[Fact]
	public void Standalone_PropertyOnGetValue_Works()
	{
		var knockOff = new StandaloneServiceKnockOff();
		IThreePatternService service = knockOff;

		knockOff.Status.Get("standalone status");

		Assert.Equal("standalone status", service.Status);
	}

	[Fact]
	public async Task Standalone_AsyncMethodOnCallValue_Works()
	{
		var knockOff = new StandaloneServiceKnockOff();
		IThreePatternService service = knockOff;

		// Value auto-wrapped in Task
		knockOff.GetNameAsync.Return("async standalone");

		Assert.Equal("async standalone", await service.GetNameAsync());
	}

	[Fact]
	public void Standalone_MethodTracking_WorksWithValue()
	{
		var knockOff = new StandaloneServiceKnockOff();
		IThreePatternService service = knockOff;

		var tracking = knockOff.GetName.Return("tracked value");

		service.GetName();
		service.GetName();

		tracking.Verify(Times.Exactly(2));
	}

	#endregion

	#region Pattern 2: Inline Interface [KnockOff<IFoo>]

	[Fact]
	public void InlineInterface_MethodOnCallValue_Works()
	{
		var stubs = new InlineInterfaceStubs();
		var serviceStub = new InlineInterfaceStubs.Stubs.IThreePatternService();
		IThreePatternService service = serviceStub;

		serviceStub.GetName.Return("inline interface result");

		Assert.Equal("inline interface result", service.GetName());
	}

	[Fact]
	public void InlineInterface_PropertyOnGetValue_Works()
	{
		var serviceStub = new InlineInterfaceStubs.Stubs.IThreePatternService();
		IThreePatternService service = serviceStub;

		serviceStub.Status.Get("inline interface status");

		Assert.Equal("inline interface status", service.Status);
	}

	[Fact]
	public async Task InlineInterface_AsyncMethodOnCallValue_Works()
	{
		var serviceStub = new InlineInterfaceStubs.Stubs.IThreePatternService();
		IThreePatternService service = serviceStub;

		serviceStub.GetNameAsync.Return("async inline interface");

		Assert.Equal("async inline interface", await service.GetNameAsync());
	}

	[Fact]
	public void InlineInterface_MethodTracking_WorksWithValue()
	{
		var serviceStub = new InlineInterfaceStubs.Stubs.IThreePatternService();
		IThreePatternService service = serviceStub;

		var tracking = serviceStub.GetName.Return("tracked value");

		service.GetName();

		tracking.Verify(Times.Once);
	}

	#endregion

	#region Pattern 3: Inline Class [KnockOff<SomeClass>]

	// Note: Inline class stubs use a simpler interceptor that does NOT include
	// OnCall(value) overloads for methods - only OnCall(callback) is available.
	// Property Get(value) IS available.

	[Fact]
	public void InlineClass_VirtualMethodOnCallCallback_Works()
	{
		var classStub = new InlineClassStubs.Stubs.ThreePatternBaseClass();
		ThreePatternBaseClass instance = classStub.Object;

		// Inline class uses callback syntax for methods
		classStub.GetVirtualName.Return(() => "inline class result");

		Assert.Equal("inline class result", instance.GetVirtualName());
	}

	[Fact]
	public void InlineClass_VirtualPropertyOnGetValue_Works()
	{
		var classStub = new InlineClassStubs.Stubs.ThreePatternBaseClass();
		ThreePatternBaseClass instance = classStub.Object;

		// Property Get(value) IS available for inline class stubs
		classStub.VirtualStatus.Get("inline class status");

		Assert.Equal("inline class status", instance.VirtualStatus);
	}

	[Fact]
	public async Task InlineClass_VirtualAsyncMethodOnCallCallback_Works()
	{
		var classStub = new InlineClassStubs.Stubs.ThreePatternBaseClass();
		ThreePatternBaseClass instance = classStub.Object;

		// Inline class uses callback syntax for async methods
		classStub.GetVirtualNameAsync.Return(() => Task.FromResult<string?>("async inline class"));

		Assert.Equal("async inline class", await instance.GetVirtualNameAsync());
	}

	[Fact]
	public void InlineClass_NonVirtualMethod_NotIntercepted()
	{
		var classStub = new InlineClassStubs.Stubs.ThreePatternBaseClass();
		ThreePatternBaseClass instance = classStub.Object;

		// Non-virtual methods are not intercepted - they call the base implementation
		Assert.Equal("non-virtual", instance.GetNonVirtualName());
	}

	#endregion

	#region Cross-Pattern Consistency Tests

	[Fact]
	public void AllPatterns_NullValueSupport()
	{
		// Standalone - supports OnCall(null)
		var standalone = new StandaloneServiceKnockOff();
		standalone.GetNullable.Return((string?)null);
		Assert.Null(((IThreePatternService)standalone).GetNullable());

		// Inline Interface - supports OnCall(null)
		var inlineInterface = new InlineInterfaceStubs.Stubs.IThreePatternService();
		inlineInterface.GetNullable.Return((string?)null);
		Assert.Null(((IThreePatternService)inlineInterface).GetNullable());
	}

	[Fact]
	public void AllPatterns_ValueTypesWork()
	{
		// Standalone - supports OnCall(int)
		var standalone = new StandaloneServiceKnockOff();
		standalone.GetCount.Return(42);
		Assert.Equal(42, ((IThreePatternService)standalone).GetCount());

		// Inline Interface - supports OnCall(int)
		var inlineInterface = new InlineInterfaceStubs.Stubs.IThreePatternService();
		inlineInterface.GetCount.Return(99);
		Assert.Equal(99, ((IThreePatternService)inlineInterface).GetCount());
	}

	[Fact]
	public void AllPatterns_ReconfigurationOverridesPrevious()
	{
		// Standalone
		var standalone = new StandaloneServiceKnockOff();
		standalone.GetName.Return("first");
		standalone.GetName.Return("second");
		Assert.Equal("second", ((IThreePatternService)standalone).GetName());

		// Inline Interface
		var inlineInterface = new InlineInterfaceStubs.Stubs.IThreePatternService();
		inlineInterface.GetName.Return("third");
		inlineInterface.GetName.Return("fourth");
		Assert.Equal("fourth", ((IThreePatternService)inlineInterface).GetName());
	}

	[Fact]
	public void PropertyOnGetValue_WorksForAllPatterns()
	{
		// Standalone
		var standalone = new StandaloneServiceKnockOff();
		standalone.Status.Get("standalone");
		Assert.Equal("standalone", ((IThreePatternService)standalone).Status);

		// Inline Interface
		var inlineInterface = new InlineInterfaceStubs.Stubs.IThreePatternService();
		inlineInterface.Status.Get("inline interface");
		Assert.Equal("inline interface", ((IThreePatternService)inlineInterface).Status);

		// Inline Class
		var inlineClass = new InlineClassStubs.Stubs.ThreePatternBaseClass();
		inlineClass.VirtualStatus.Get("inline class");
		Assert.Equal("inline class", inlineClass.Object.VirtualStatus);
	}

	#endregion

	#region Test Interfaces and Classes

	/// <summary>Common interface for all three patterns.</summary>
	public interface IThreePatternService
	{
		string? GetName();
		string? GetNullable();
		int GetCount();
		Task<string?> GetNameAsync();
		string? Status { get; set; }
	}

	/// <summary>Base class for inline class pattern testing.</summary>
	public class ThreePatternBaseClass
	{
		public virtual string? GetVirtualName() => "base virtual";
		public virtual string? VirtualStatus { get; set; } = "base status";
		public virtual Task<string?> GetVirtualNameAsync() => Task.FromResult<string?>("base async");
		public string GetNonVirtualName() => "non-virtual";
	}

	#endregion

	#region Pattern 1: Standalone Stub

	[KnockOff]
	public partial class StandaloneServiceKnockOff : IThreePatternService
	{
	}

	#endregion

	#region Pattern 2: Inline Interface Stubs

	[KnockOff<IThreePatternService>]
	public partial class InlineInterfaceStubs
	{
	}

	#endregion

	#region Pattern 3: Inline Class Stubs

	[KnockOff<ThreePatternBaseClass>]
	public partial class InlineClassStubs
	{
	}

	#endregion
}
