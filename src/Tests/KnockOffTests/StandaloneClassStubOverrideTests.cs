using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for stub override overrides on standalone class stubs ([KnockOffBase&lt;T&gt;]).
/// Stub overrides provide default behavior via protected virtual methods with _ suffix
/// in the generated base class. The stub overrides these to define fallback behavior.
/// </summary>
public class StandaloneClassStubOverrideTests
{
	#region Stub Override Fallback - Abstract Methods (Pattern 3)

	[Fact]
	public void AbstractMethod_UserOverride_IsCalledAsFallback()
	{
		// Arrange - Execute is abstract on target class; stub overrides Execute_
		var stub = new SCStubOverrideStub();

		// Act
		stub.Object.Execute("test-command");

		// Assert - stub override was called (Execute_ records the command)
		Assert.Equal("test-command", stub.LastExecutedCommand);
		stub.Execute.Verify(Called.Once);
	}

	[Fact]
	public void AbstractMethod_UserOverride_NonVoid_ReturnsUserValue()
	{
		// Arrange - Process is abstract, returns string; stub override returns "[USER: input]"
		var stub = new SCStubOverrideStub();

		// Act
		var result = stub.Object.Process("hello");

		// Assert - stub override provided the value
		Assert.Equal("[USER: hello]", result);
		stub.Process.Verify(Called.Once);
	}

	[Fact]
	public void AbstractMethod_NoUserOverride_ReturnsDefault()
	{
		// Arrange - GetCount is abstract, no stub override
		var stub = new SCStubOverrideStub();

		// Act
		var result = stub.Object.GetCount();

		// Assert - returns default(int) since no stub override and no OnCall
		Assert.Equal(0, result);
	}

	#endregion

	#region Stub Override Fallback - Virtual Methods (Pattern 3)

	[Fact]
	public void VirtualMethod_UserOverride_ReplacesBaseCall()
	{
		// Arrange - Initialize is virtual on target class; stub overrides Initialize_
		var stub = new SCStubOverrideStub();

		// Act
		stub.Object.Initialize();

		// Assert - stub override was called instead of base.Initialize()
		Assert.True(stub.InitializeCalled);
		stub.Initialize.Verify(Called.Once);
	}

	[Fact]
	public void VirtualMethod_NoUserOverride_DelegatesToBase()
	{
		// Arrange - Shutdown is virtual, no stub override
		var stub = new SCNoStubOverrideStub();

		// Act
		stub.Object.Initialize();

		// Assert - base.Initialize() was called (via unconfigured-count pattern)
		// The fact that it doesn't throw shows base was called
		stub.Initialize.Verify(Called.Once);
	}

	#endregion

	#region OnCall Supersedes Stub Override

	[Fact]
	public void OnCall_SupersedesStubOverride_VoidMethod()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		var onCallInvoked = false;
		stub.Execute.Call((cmd) => onCallInvoked = true);

		// Act
		stub.Object.Execute("test");

		// Assert - OnCall wins, stub override not called
		Assert.True(onCallInvoked);
		Assert.Null(stub.LastExecutedCommand); // Stub override was NOT called
	}

	[Fact]
	public void OnCall_SupersedesStubOverride_NonVoidMethod()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Call(input => "[ONCALL: " + input + "]");

		// Act
		var result = stub.Object.Process("hello");

		// Assert - OnCall wins over stub override
		Assert.Equal("[ONCALL: hello]", result);
	}

	#endregion

	#region Returns Supersedes Stub Override

	[Fact]
	public void Returns_SupersedesStubOverride()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Return("[CONSTANT]");

		// Act
		var result = stub.Object.Process("ignored");

		// Assert - Returns wins over stub override
		Assert.Equal("[CONSTANT]", result);
	}

	#endregion

	#region When Chains with Stub Override Fallback

	[Fact]
	public void When_Match_ReturnsWhenValue()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.When("special").Return("[WHEN MATCHED]");

		// Act
		var result = stub.Object.Process("special");

		// Assert - When chain wins
		Assert.Equal("[WHEN MATCHED]", result);
	}

	[Fact]
	public void When_NoMatch_FallsToStubOverride()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.When("special").Return("[WHEN MATCHED]");

		// Act
		var result = stub.Object.Process("normal");

		// Assert - Falls to stub override
		Assert.Equal("[USER: normal]", result);
	}

	[Fact]
	public void When_ThenWhen_ChainWithStubOverrideFallback()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.When("first").Return("[1]")
			.ThenWhen("second").Return("[2]")
			.ThenNone();

		// Act
		var r1 = stub.Object.Process("first");
		var r2 = stub.Object.Process("second");
		var r3 = stub.Object.Process("third"); // Falls to stub override

		// Assert
		Assert.Equal("[1]", r1);
		Assert.Equal("[2]", r2);
		Assert.Equal("[USER: third]", r3);
	}

	[Fact]
	public void When_VoidMethod_CallsCallback()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		var callbackInvoked = false;
		stub.Execute.When("trigger").Call(cmd => callbackInvoked = true);

		// Act
		stub.Object.Execute("trigger");

		// Assert
		Assert.True(callbackInvoked);
	}

	[Fact]
	public void When_VoidMethod_NoMatch_FallsToStubOverride()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Execute.When("trigger").Call(cmd => { });

		// Act
		stub.Object.Execute("other"); // Falls to stub override

		// Assert - stub override was called
		Assert.Equal("other", stub.LastExecutedCommand);
		stub.Execute.Verify(Called.Once);
	}

	[Fact]
	public void When_HasPriorityOverOnCall()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Call(s => "[ONCALL]");
		stub.Process.When("special").Return("[WHEN]");

		// Act
		var whenResult = stub.Object.Process("special");
		var onCallResult = stub.Object.Process("other");

		// Assert - When has priority, OnCall is fallback before stub override
		Assert.Equal("[WHEN]", whenResult);
		Assert.Equal("[ONCALL]", onCallResult);
	}

	#endregion

	#region Sequences with Stub Override Fallback

	[Fact]
	public void Sequence_ReturnsValuesInOrder()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Call(s => "[FIRST]").ThenReturn("[SECOND]");

		// Act
		var r1 = stub.Object.Process("a");
		var r2 = stub.Object.Process("b");
		var r3 = stub.Object.Process("c"); // Repeats last

		// Assert
		Assert.Equal("[FIRST]", r1);
		Assert.Equal("[SECOND]", r2);
		Assert.Equal("[SECOND]", r3);
	}

	[Fact]
	public void Returns_Sequence_ReturnsValuesInOrder()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Return("[1]", "[2]");

		// Act
		var r1 = stub.Object.Process("a");
		var r2 = stub.Object.Process("b");
		var r3 = stub.Object.Process("c"); // Repeats last

		// Assert
		Assert.Equal("[1]", r1);
		Assert.Equal("[2]", r2);
		Assert.Equal("[2]", r3);
	}

	[Fact]
	public void VoidSequence_CallsInOrder()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		var calls = new System.Collections.Generic.List<string>();
		stub.Execute.Call(cmd => calls.Add("first:" + cmd))
			.ThenCall(cmd => calls.Add("second:" + cmd))
			.ThenCall(cmd => calls.Add("third:" + cmd));

		// Act
		stub.Object.Execute("a");
		stub.Object.Execute("b");
		stub.Object.Execute("c");

		// Assert
		Assert.Equal(new[] { "first:a", "second:b", "third:c" }, calls);
	}

	#endregion

	#region Verifiable on Stub Override Interceptors

	[Fact]
	public void Verifiable_Called_PassesVerification()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Verifiable();

		// Act
		stub.Object.Process("test");

		// Assert - should not throw
		stub.Verify();
	}

	[Fact]
	public void Verifiable_NotCalled_ThrowsVerificationException()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Verifiable();
		// Don't call Process

		// Assert
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void Verifiable_WithTimesExactly_VerifiesCount()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Execute.Verifiable(Called.Exactly(2));

		// Act
		stub.Object.Execute("a");
		stub.Object.Execute("b");

		// Assert - should not throw
		stub.Verify();
	}

	[Fact]
	public void Verifiable_WithTimesExactly_WrongCount_Throws()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Execute.Verifiable(Called.Exactly(2));

		// Act - only call once
		stub.Object.Execute("a");

		// Assert
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void Verifiable_VoidMethod_Called_PassesVerification()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Execute.Verifiable();

		// Act
		stub.Object.Execute("cmd");

		// Assert - should not throw
		stub.Verify();
	}

	[Fact]
	public void Verifiable_VoidMethod_NotCalled_Throws()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Execute.Verifiable();
		// Don't call Execute

		// Assert
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void IndividualVerify_TracksStubOverrideCalls()
	{
		// Arrange
		var stub = new SCStubOverrideStub();

		// Act - call via stub override fallback (no OnCall configured)
		stub.Object.Process("a");
		stub.Object.Process("b");
		stub.Object.Process("c");

		// Assert - tracking works even through stub override fallback
		stub.Process.Verify(Called.Exactly(3));
	}

	[Fact]
	public void LastArg_TracksWithStubOverrideFallback()
	{
		// Arrange
		var stub = new SCStubOverrideStub();

		// Act
		stub.Object.Execute("first");
		stub.Object.Execute("second");

		// Assert
		Assert.Equal("second", stub.Execute.LastArg);
	}

	#endregion

	#region Generic Standalone Class with Stub Overrides (Pattern 4)

	[Fact]
	public void GenericStandaloneClass_StubOverrideFallback_ReturnsGenericType()
	{
		// Arrange - GetById_ returns T? and stub overrides it
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var entity = new ClassStubUser { Id = 42, Name = "Test" };
		stub.DefaultEntity = entity;

		// Act
		var result = stub.Object.GetById(42);

		// Assert - stub override was called
		Assert.Same(entity, result);
	}

	[Fact]
	public void GenericStandaloneClass_StubOverrideFallback_VoidWithGenericParam()
	{
		// Arrange - Save_ takes T param and stub overrides it
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var entity = new ClassStubUser { Id = 1, Name = "Saved" };

		// Act
		stub.Object.Save(entity);

		// Assert - stub override was called
		Assert.Same(entity, stub.LastSavedEntity);
	}

	[Fact]
	public void GenericStandaloneClass_OnCall_SupersedesStubOverride()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var onCallEntity = new ClassStubUser { Id = 99, Name = "OnCall" };
		stub.GetById.Call(id => onCallEntity);

		// Act
		var result = stub.Object.GetById(1);

		// Assert - OnCall wins over stub override
		Assert.Same(onCallEntity, result);
	}

	[Fact]
	public void GenericStandaloneClass_Returns_SupersedesStubOverride()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var returnsEntity = new ClassStubUser { Id = 77, Name = "Returns" };
		stub.GetById.Return(returnsEntity);

		// Act
		var result = stub.Object.GetById(1);

		// Assert - Returns wins over stub override
		Assert.Same(returnsEntity, result);
	}

	[Fact]
	public void GenericStandaloneClass_Verifiable_Works()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		stub.Save.Verifiable();

		// Act
		stub.Object.Save(new ClassStubUser { Id = 1, Name = "Test" });

		// Assert - should not throw
		stub.Verify();
	}

	[Fact]
	public void GenericStandaloneClass_Verifiable_NotCalled_Throws()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		stub.Save.Verifiable();
		// Don't call Save

		// Assert
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	#endregion

	#region Partial Overload Coverage (Pattern 4)

	[Fact]
	public void PartialOverload_OverriddenOverload_UsesStubOverride()
	{
		// Arrange - GetDefault() (no params) has stub override
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var defaultEntity = new ClassStubUser { Id = 0, Name = "Default" };
		stub.DefaultGetDefaultEntity = defaultEntity;

		// Act
		var result = stub.Object.GetDefault();

		// Assert - stub override was called
		Assert.Same(defaultEntity, result);
	}

	[Fact]
	public void PartialOverload_NonOverriddenOverload_DelegatesToBase()
	{
		// Arrange - GetDefault(string) has NO stub override, uses base class fallback
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();

		// Act - GetDefault(string) uses the unconfigured-count + base pattern
		var result = stub.Object.GetDefault("filter");

		// Assert - base.GetDefault(string) returns default (null)
		Assert.Null(result);
	}

	[Fact]
	public void PartialOverload_OnCall_OnNonOverriddenOverload_Works()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var onCallEntity = new ClassStubUser { Id = 5, Name = "Filtered" };
		stub.GetDefault.Call(filter => onCallEntity);

		// Act
		var result = stub.Object.GetDefault("any-filter");

		// Assert - OnCall was used for non-overridden overload
		Assert.Same(onCallEntity, result);
	}

	[Fact]
	public void PartialOverload_EachOverloadIndependent()
	{
		// Arrange
		var stub = new SCGenericStubOverrideStub<ClassStubUser>();
		var userEntity = new ClassStubUser { Id = 0, Name = "UserDefault" };
		var onCallEntity = new ClassStubUser { Id = 1, Name = "FilteredResult" };
		stub.DefaultGetDefaultEntity = userEntity;
		stub.GetDefault.Call(filter => onCallEntity);

		// Act
		var userResult = stub.Object.GetDefault();        // Stub override overload
		var onCallResult = stub.Object.GetDefault("cat"); // OnCall overload

		// Assert - each overload behaves independently
		Assert.Same(userEntity, userResult);
		Assert.Same(onCallEntity, onCallResult);
	}

	#endregion

	#region Tracking and Reset

	[Fact]
	public void Reset_PreservesOnCallConfiguration()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Process.Call(s => "[ONCALL]");

		stub.Object.Process("first");
		stub.Process.Verify(Called.Once);

		// Act
		stub.Process.Reset();

		// Assert - OnCall preserved, tracking cleared
		stub.Process.Verify(Called.Never);
		var result = stub.Object.Process("second");
		Assert.Equal("[ONCALL]", result);
	}

	[Fact]
	public void ResetInterceptors_ClearsAllState()
	{
		// Arrange
		var stub = new SCStubOverrideStub();
		stub.Object.Execute("cmd");
		stub.Object.Process("test");
		stub.Object.Initialize();

		// Act
		stub.ResetInterceptors();

		// Assert
		stub.Execute.Verify(Called.Never);
		stub.Process.Verify(Called.Never);
		stub.Initialize.Verify(Called.Never);
	}

	#endregion

	#region Strict Mode Interaction

	[Fact]
	public void StrictMode_StubOverrideBypassesStrict()
	{
		// Arrange - Stub overrides act as configured behavior, so strict mode doesn't throw
		var stub = new SCStubOverrideStub();
		stub.Strict = true;

		// Act - Execute has stub override, should not throw
		stub.Object.Execute("test");
		var result = stub.Object.Process("hello");

		// Assert
		Assert.Equal("test", stub.LastExecutedCommand);
		Assert.Equal("[USER: hello]", result);
	}

	[Fact]
	public void StrictMode_NoUserOverride_Throws()
	{
		// Arrange - GetCount has no stub override
		var stub = new SCStubOverrideStub();
		stub.Strict = true;

		// Act & Assert - should throw because GetCount has no stub override
		Assert.Throws<StubException>(() => stub.Object.GetCount());
	}

	#endregion
}

#region Pattern 3 Target Classes and Stubs

/// <summary>
/// Abstract target class with abstract and virtual methods for testing
/// standalone class stub override overrides (pattern 3).
/// </summary>
public abstract class SCServiceBase
{
	/// <summary>Abstract void method with parameter.</summary>
	public abstract void Execute(string command);

	/// <summary>Abstract non-void method with parameter.</summary>
	public abstract string Process(string input);

	/// <summary>Abstract non-void method without stub override.</summary>
	public abstract int GetCount();

	/// <summary>Virtual void method with default implementation.</summary>
	public virtual void Initialize() { }
}

/// <summary>
/// Standalone class stub with stub override overrides for abstract and virtual methods.
/// </summary>
[KnockOffBase<SCServiceBase>]
public partial class SCStubOverrideStub
{
	// State for verifying stub override was called
	public string? LastExecutedCommand { get; set; }
	public bool InitializeCalled { get; set; }

	protected override void Execute_(string command)
	{
		LastExecutedCommand = command;
	}

	protected override string Process_(string input)
	{
		return $"[USER: {input}]";
	}

	protected override void Initialize_()
	{
		InitializeCalled = true;
	}

	// NOTE: GetCount_ is intentionally NOT overridden to test
	// behavior without stub override
}

/// <summary>
/// Standalone class stub with NO stub override overrides.
/// Tests that the standard unconfigured-count + base call pattern still works.
/// </summary>
[KnockOffBase<SCServiceBase>]
public partial class SCNoStubOverrideStub
{
}

#endregion

#region Pattern 4 Target Classes and Stubs

/// <summary>
/// Generic abstract target class for testing generic standalone class stub overrides (pattern 4).
/// </summary>
public abstract class SCRepositoryBase<T> where T : class
{
	/// <summary>Virtual method returning generic type T?.</summary>
	public virtual T? GetById(int id) => default;

	/// <summary>Virtual void method with generic type T parameter.</summary>
	public virtual void Save(T entity) { }

	/// <summary>Overloaded method - no params.</summary>
	public virtual T? GetDefault() => default;

	/// <summary>Overloaded method - with filter.</summary>
	public virtual T? GetDefault(string filter) => default;
}

/// <summary>
/// Generic standalone class stub with stub override overrides.
/// Tests partial overload coverage: GetDefault_() overridden, GetDefault_(string) not.
/// </summary>
[KnockOffBase(typeof(SCRepositoryBase<>))]
public partial class SCGenericStubOverrideStub<T> where T : class
{
	// State for verifying stub override was called
	public T? DefaultEntity { get; set; }
	public T? LastSavedEntity { get; set; }
	public T? DefaultGetDefaultEntity { get; set; }

	protected override T? GetById_(int id)
	{
		return DefaultEntity;
	}

	protected override void Save_(T entity)
	{
		LastSavedEntity = entity;
	}

	// Only override parameterless GetDefault -- NOT GetDefault(string)
	protected override T? GetDefault_()
	{
		return DefaultGetDefaultEntity;
	}

	// NOTE: GetDefault_(string filter) is intentionally NOT overridden.
	// This tests partial overload coverage.
}

#endregion
