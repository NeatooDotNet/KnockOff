namespace KnockOff.Tests;

/// <summary>
/// Tests for Bug 2: Generic interface inheritance type mismatch.
///
/// When an interface inherits from a generic interface which also has a non-generic base
/// (e.g., IRule{T} : IRule), KnockOff should generate separate interceptors for each
/// to handle the different parameter types correctly.
///
/// Example: IConsultationHistoryRule : IRule{IConsultationHistory}
/// Where IRule{T} : IRule and both have RunRule methods with different parameter types.
///
/// These tests verify the fix: Execute1 (typed) and Execute2 (base) should be separate
/// interceptors with correct parameter types.
/// </summary>
public class GenericInheritanceTypeMismatchBugTests
{
	[Fact]
	public void GenericInheritance_DerivedMethod_CanBeCalled()
	{
		var stub = new SampleValidationRuleKnockOff();
		ISampleRule<ISampleTarget> rule = stub;

		var target = new SampleTarget { Value = "test" };

		// Set up callback for Execute1 (typed version)
		var expectedResult = new SampleResult { Success = true };
		stub.Execute1.OnCall = (ko, t, ct) => Task.FromResult<ISampleResult>(expectedResult);

		// Call the generic version (ISampleRule<T>.Execute)
		var result = rule.Execute(target, CancellationToken.None);

		// Should be tracked on Execute1 (the typed interceptor)
		Assert.True(stub.Execute1.WasCalled);
		Assert.False(stub.Execute2.WasCalled);
	}

	[Fact]
	public void GenericInheritance_BaseMethod_CanBeCalled()
	{
		var stub = new SampleValidationRuleKnockOff();
		ISampleRule rule = stub; // Cast to base interface

		var target = new SampleTarget { Value = "base-call" };

		// Set up callback for Execute2 (base interface version)
		var expectedResult = new SampleResult { Success = true };
		stub.Execute2.OnCall = (ko, t, ct) => Task.FromResult<ISampleResult>(expectedResult);

		// Call the non-generic version (ISampleRule.Execute)
		var result = rule.Execute(target, CancellationToken.None);

		Assert.NotNull(result);
		Assert.True(stub.Execute2.WasCalled);
		Assert.False(stub.Execute1.WasCalled);
	}

	[Fact]
	public void GenericInheritance_BothMethods_TrackSeparately()
	{
		var stub = new SampleValidationRuleKnockOff();

		// Set up callbacks for both interceptors
		var result1 = new SampleResult { Success = true };
		var result2 = new SampleResult { Success = false };
		stub.Execute1.OnCall = (ko, t, ct) => Task.FromResult<ISampleResult>(result1);
		stub.Execute2.OnCall = (ko, t, ct) => Task.FromResult<ISampleResult>(result2);

		// Call via derived interface (Execute1)
		ISampleRule<ISampleTarget> typedRule = stub;
		var target1 = new SampleTarget { Value = "typed" };
		typedRule.Execute(target1, CancellationToken.None);

		// Call via base interface (Execute2)
		ISampleRule baseRule = stub;
		var target2 = new SampleTarget { Value = "base" };
		baseRule.Execute(target2, CancellationToken.None);

		// Each method tracked separately
		Assert.Equal(1, stub.Execute1.CallCount);
		Assert.Equal(1, stub.Execute2.CallCount);
	}

	[Fact]
	public async Task GenericInheritance_OnCallCallback_WorksForTypedMethod()
	{
		var stub = new SampleValidationRuleKnockOff();
		ISampleRule<ISampleTarget> rule = stub;

		var expectedResult = new SampleResult { Success = true };
		stub.Execute1.OnCall = (ko, target, ct) => Task.FromResult<ISampleResult>(expectedResult);

		var target = new SampleTarget { Value = "callback" };
		var result = await rule.Execute(target, CancellationToken.None);

		Assert.True(result.Success);
	}
}

#region Bug 2 Test Types - Generic Interface Inheritance

/// <summary>
/// Base interface for validation rule targets.
/// Analogous to IValidateBase in Neatoo.
/// </summary>
public interface ISampleRuleTarget
{
	string Value { get; }
}

/// <summary>
/// Concrete target type.
/// Analogous to IConsultationHistory in Neatoo.
/// </summary>
public interface ISampleTarget : ISampleRuleTarget
{
	// Inherits Value from ISampleRuleTarget
}

public class SampleTarget : ISampleTarget
{
	public string Value { get; set; } = "";
}

/// <summary>
/// Result of a validation rule.
/// </summary>
public interface ISampleResult
{
	bool Success { get; }
}

public class SampleResult : ISampleResult
{
	public bool Success { get; set; }
}

/// <summary>
/// Non-generic base rule interface.
/// Analogous to IRule in Neatoo.
/// </summary>
public interface ISampleRule
{
	/// <summary>
	/// Execute with base type parameter.
	/// </summary>
	Task<ISampleResult> Execute(ISampleRuleTarget target, CancellationToken? token);
}

/// <summary>
/// Generic rule interface that inherits from non-generic base.
/// Analogous to IRule{T} : IRule in Neatoo.
/// </summary>
public interface ISampleRule<T> : ISampleRule where T : ISampleRuleTarget
{
	/// <summary>
	/// Execute with typed parameter.
	/// This method has the SAME NAME but DIFFERENT parameter type than base.
	/// </summary>
	Task<ISampleResult> Execute(T target, CancellationToken? token);
}

/// <summary>
/// Concrete rule interface.
/// Analogous to IConsultationHistoryRule : IRule{IConsultationHistory} in Neatoo.
/// </summary>
public interface ISampleValidationRule : ISampleRule<ISampleTarget>
{
	// Inherits:
	// - Task<ISampleResult> Execute(ISampleTarget target, CancellationToken? token) from ISampleRule<T>
	// - Task<ISampleResult> Execute(ISampleRuleTarget target, CancellationToken? token) from ISampleRule
}

/// <summary>
/// Stub for ISampleValidationRule.
///
/// Expected: Separate interceptors or proper delegation for:
/// - ISampleRule.Execute(ISampleRuleTarget, ...)
/// - ISampleRule{T}.Execute(T, ...)
///
/// Bug: One interceptor typed for ISampleTarget, causing CS1503 when
/// the ISampleRule.Execute explicit implementation tries to pass ISampleRuleTarget.
/// </summary>
[KnockOff]
public partial class SampleValidationRuleKnockOff : ISampleValidationRule
{
}

#endregion
