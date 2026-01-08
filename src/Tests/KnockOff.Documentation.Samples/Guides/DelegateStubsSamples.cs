/// <summary>
/// Code samples for docs/guides/delegates.md
///
/// Snippets in this file:
/// - docs:delegates:basic-delegate
/// - docs:delegates:basic-usage
/// - docs:delegates:void-delegate
/// - docs:delegates:multi-param-delegate
/// - docs:delegates:implicit-conversion
/// - docs:delegates:interceptor-api
/// - docs:delegates:closed-generics
/// - docs:delegates:validation-pattern
///
/// Corresponding tests: DelegateStubsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides.Delegates;

// ============================================================================
// Domain Types for Delegate Samples
// ============================================================================

public class DelUser
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Delegate Types for Samples
// ============================================================================

#region docs:delegates:basic-delegate
// Named delegate types can be stubbed with [KnockOff<TDelegate>]
public delegate bool IsUniqueRule(string value);

public delegate DelUser UserFactory(int id);

public delegate void LogAction(string message);
#endregion

// Multi-parameter delegate (not currently used in docs - ref/out params not supported for delegate stubs)
public delegate string Formatter(string template, object[] args);

public delegate bool Validator(string field, object value, out string error);

// ============================================================================
// Basic Delegate Stub Example
// ============================================================================

#region docs:delegates:basic-usage
[KnockOff<IsUniqueRule>]
[KnockOff<UserFactory>]
public partial class DelegateTests
{
	// The [KnockOff<TDelegate>] attribute generates:
	// - Stubs.IsUniqueRule with Interceptor property
	// - Stubs.UserFactory with Interceptor property
	// Each has implicit conversion to the delegate type
}

// Usage in test:
// var uniqueStub = new DelegateTests.Stubs.IsUniqueRule();
// uniqueStub.Interceptor.OnCall = (ko, value) => value != "duplicate";
//
// // Implicit conversion to delegate
// IsUniqueRule rule = uniqueStub;
// Assert.True(rule("unique"));
// Assert.False(rule("duplicate"));
//
// // Verify calls
// Assert.Equal(2, uniqueStub.Interceptor.CallCount);
// Assert.Equal("duplicate", uniqueStub.Interceptor.LastCallArg);
#endregion

// ============================================================================
// Void Delegate Example
// ============================================================================

#region docs:delegates:void-delegate
[KnockOff<LogAction>]
public partial class VoidDelegateTests
{
}

// Usage:
// var logStub = new VoidDelegateTests.Stubs.LogAction();
// var messages = new List<string>();
// logStub.Interceptor.OnCall = (ko, msg) => messages.Add(msg);
//
// LogAction logger = logStub;
// logger("Hello");
// logger("World");
//
// Assert.Equal(2, logStub.Interceptor.CallCount);
// Assert.Equal(["Hello", "World"], messages);
#endregion

// ============================================================================
// Implicit Conversion
// ============================================================================

#region docs:delegates:implicit-conversion
// Stub classes have implicit conversion operators to the delegate type.
// This enables seamless passing to methods that expect the delegate:

// public class Validator
// {
//     private readonly IsUniqueRule _rule;
//     public Validator(IsUniqueRule rule) => _rule = rule;
//     public bool Validate(string value) => _rule(value);
// }

// var stub = new DelegateTests.Stubs.IsUniqueRule();
// stub.Interceptor.OnCall = (ko, v) => v.Length > 3;
//
// // Implicit conversion - no cast needed
// var validator = new Validator(stub);
// Assert.True(validator.Validate("test"));
#endregion

// ============================================================================
// Interceptor API Reference
// ============================================================================

#region docs:delegates:interceptor-api
// Delegate stub interceptor properties:
// stub.Interceptor.CallCount      // int - number of invocations
// stub.Interceptor.WasCalled      // bool - invoked at least once
// stub.Interceptor.LastCallArg    // T? - last argument (single param delegates)
// stub.Interceptor.LastCallArgs   // (T1, T2, ...)? - tuple (multi-param delegates)
// stub.Interceptor.OnCall         // Func/Action - callback for custom behavior
// stub.Interceptor.Reset()        // Clear all tracking and callback
#endregion

// ============================================================================
// Closed Generic Delegates
// ============================================================================

public delegate T Factory<T>();
public delegate TResult Converter<TInput, TResult>(TInput input);

#region docs:delegates:closed-generics
[KnockOff<Factory<DelUser>>]
[KnockOff<Converter<int, string>>]
public partial class GenericDelegateTests
{
	// Closed generic delegates generate stubs with simple names:
	// - Stubs.Factory (for Factory<DelUser>)
	// - Stubs.Converter (for Converter<int, string>)
}

// Usage:
// var factoryStub = new GenericDelegateTests.Stubs.Factory();
// factoryStub.Interceptor.OnCall = (ko) => new DelUser { Id = 1, Name = "Created" };
//
// Factory<DelUser> factory = factoryStub;
// var user = factory();
// Assert.Equal("Created", user.Name);
//
// var converterStub = new GenericDelegateTests.Stubs.Converter();
// converterStub.Interceptor.OnCall = (ko, num) => $"Number: {num}";
//
// Converter<int, string> converter = converterStub;
// Assert.Equal("Number: 42", converter(42));
#endregion

// ============================================================================
// Validation Pattern
// ============================================================================

#region docs:delegates:validation-pattern
[KnockOff<IsUniqueRule>]
public partial class ValidationPatternTests
{
}

// Use delegates for validation rules in domain entities:
// var uniqueCheck = new ValidationPatternTests.Stubs.IsUniqueRule();
//
// // Default: always valid
// uniqueCheck.Interceptor.OnCall = (ko, value) => true;
//
// var entity = new Entity(uniqueCheck);
// entity.Name = "anything";  // Passes validation
//
// // Test invalid scenario
// uniqueCheck.Interceptor.Reset();
// uniqueCheck.Interceptor.OnCall = (ko, value) => false;
//
// entity.Name = "duplicate";  // Triggers validation error
// Assert.True(entity.HasErrors);
#endregion
