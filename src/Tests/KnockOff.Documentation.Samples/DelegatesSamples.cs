using KnockOff;

namespace KnockOff.Documentation.Samples.Delegates;

// =============================================================================
// Delegate Types for Samples
// =============================================================================

/// <summary>
/// Void delegate with no parameters.
/// </summary>
public delegate void NotifyCallback();

/// <summary>
/// Delegate with single parameter and return value.
/// </summary>
public delegate string Formatter(string input);

/// <summary>
/// Delegate with multiple parameters and return value.
/// </summary>
public delegate string MessageBuilder(string name, int age);

/// <summary>
/// Generic factory delegate.
/// </summary>
public delegate T Factory<T>();

/// <summary>
/// Generic converter delegate.
/// </summary>
public delegate TResult Converter<TInput, TResult>(TInput input);

/// <summary>
/// Validation rule predicate delegate.
/// </summary>
public delegate bool IsUniqueRule(string value);

/// <summary>
/// Format validation predicate delegate.
/// </summary>
public delegate bool IsValidFormatRule(string value);

/// <summary>
/// Event callback delegate.
/// </summary>
public delegate void EventCallback(DomainEvent evt);

/// <summary>
/// Domain event for event callback samples.
/// </summary>
public class DomainEvent
{
    public string EventType { get; set; } = "";
    public object? Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Product entity for factory samples.
/// </summary>
public class Product
{
    public Product() { }
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

// =============================================================================
// Stub Definitions
// =============================================================================

/// <summary>
/// Define a void delegate with no parameters.
/// </summary>
public delegate void OnComplete();

/// <summary>
/// Apply [KnockOff] to generate a delegate stub in the Stubs class.
/// </summary>
[KnockOff<OnComplete>]
public partial class BasicVoidDelegateTest
{
}

[KnockOff<Formatter>]
[KnockOff<MessageBuilder>]
[KnockOff<NotifyCallback>]
[KnockOff<Factory<string>>]
[KnockOff<Converter<int, string>>]
[KnockOff<IsUniqueRule>]
[KnockOff<IsValidFormatRule>]
[KnockOff<EventCallback>]
public partial class DelegateStubTests
{
}

/// <summary>
/// Separate class for Factory&lt;Product&gt; to avoid naming collision with Factory&lt;string&gt;.
/// </summary>
[KnockOff<Factory<Product>>]
public partial class ProductFactoryTest
{
}

// =============================================================================
// Open Generic Delegate Pattern (using typeof)
// =============================================================================

/// <summary>
/// Generic factory delegate with constraint.
/// </summary>
public delegate T ConstrainedFactory<T>() where T : new();

[KnockOff(typeof(Factory<>))]
[KnockOff(typeof(ConstrainedFactory<>))]
public partial class OpenGenericDelegateTest
{
}

// =============================================================================
// Basic Usage Samples
// =============================================================================

public class BasicUsageTests
{
    #region delegate-stub-basic-void
    [Fact]
    public void BasicVoid_DelegateStub()
    {
        // Create the delegate stub
        var stub = new BasicVoidDelegateTest.Stubs.OnComplete();

        // Convert to delegate and invoke
        OnComplete callback = stub;
        callback();

        // Verify the delegate was called
        stub.Interceptor.Verify();
    }
    #endregion

    #region delegate-stub-with-return
    [Fact]
    public void WithReturn_TracksArgAndReturnsDefault()
    {
        // Create a stub for a delegate with return value
        var stub = new DelegateStubTests.Stubs.Formatter();

        // Invoke through the delegate
        Formatter format = stub;
        var result = format("hello");

        // Default return value is null for reference types
        Assert.Null(result);

        // Track last argument
        Assert.Equal("hello", stub.Interceptor.LastCallArg);
    }
    #endregion

    #region delegate-stub-multi-param
    [Fact]
    public void MultiParam_TracksLastCallArgs()
    {
        // Create a stub for a multi-parameter delegate
        var stub = new DelegateStubTests.Stubs.MessageBuilder();

        // Invoke through the delegate
        MessageBuilder builder = stub;
        builder("Alice", 30);

        // Access arguments via named tuple
        Assert.NotNull(stub.Interceptor.LastCallArgs);
        Assert.Equal("Alice", stub.Interceptor.LastCallArgs.Value.name);
        Assert.Equal(30, stub.Interceptor.LastCallArgs.Value.age);
    }
    #endregion
}

// =============================================================================
// OnCall Configuration Samples
// =============================================================================

public class OnCallConfigurationTests
{
    #region delegate-stub-oncall-void
    [Fact]
    public void OnCallVoid_ExecutesCustomLogic()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();

        // Configure side effects for void delegate
        var notified = false;
        stub.Interceptor.OnCall(() => notified = true);

        // Invoke through the delegate
        NotifyCallback callback = stub;
        callback();

        // Verify side effect occurred
        Assert.True(notified);
    }
    #endregion

    #region delegate-stub-oncall-value
    [Fact]
    public void OnCallValue_ReturnsFixedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        // VALUE OVERLOAD: Pass the return value directly (simpler syntax)
        stub.Interceptor.OnCall("FORMATTED");

        // Invoke through the delegate
        Formatter format = stub;
        var result = format("any input");

        // Returns the fixed value regardless of input
        Assert.Equal("FORMATTED", result);
    }
    #endregion

    #region delegate-stub-oncall-return
    [Fact]
    public void OnCallReturn_ReturnsComputedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        // CALLBACK: Configure to return computed value based on input
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

        // Invoke through the delegate
        Formatter format = stub;
        var result = format("hello");

        // Verify computed return value
        Assert.Equal("HELLO", result);
    }
    #endregion

    #region delegate-stub-oncall-multi-param
    [Fact]
    public void OnCallMultiParam_ComputesFromAllParams()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();

        // Configure with multiple parameters
        stub.Interceptor.OnCall((name, age) => $"{name} is {age} years old");

        // Invoke through the delegate
        MessageBuilder builder = stub;
        var result = builder("Bob", 25);

        // Verify computed result
        Assert.Equal("Bob is 25 years old", result);
    }
    #endregion
}

// =============================================================================
// Verification Samples
// =============================================================================

public class VerificationTests
{
    #region delegate-stub-verification-basic
    [Fact]
    public void Verify_ThrowsIfNeverCalled()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });

        // Invoke through the delegate
        NotifyCallback callback = stub;
        callback();

        // Verify() passes - delegate was called at least once
        stub.Interceptor.Verify();
    }
    #endregion

    #region delegate-stub-verification-times
    [Fact]
    public void Verify_WithTimesConstraints()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });

        NotifyCallback callback = stub;

        // Call exactly 3 times
        callback();
        callback();
        callback();

        // Verify with Times constraints
        stub.Interceptor.Verify(Times.Exactly(3));
        stub.Interceptor.Verify(Times.AtLeast(2));
        stub.Interceptor.Verify(Times.AtMost(5));
    }
    #endregion

    #region delegate-stub-verifiable
    [Fact]
    public void Verifiable_VerifyAfterOnCall()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        // Configure the delegate behavior
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

        Formatter format = stub;
        format("test");

        // Verify delegate was called
        // Note: Delegate interceptors use Verify() directly (no Verifiable() chaining)
        stub.Interceptor.Verify();
    }
    #endregion
}

// =============================================================================
// Tracking Samples
// =============================================================================

public class TrackingTests
{
    #region delegate-stub-lastcallarg
    [Fact]
    public void LastCallArg_SingleParameter()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input);

        Formatter format = stub;
        format("first");
        format("second");

        // LastCallArg captures the most recent argument
        Assert.Equal("second", stub.Interceptor.LastCallArg);
    }
    #endregion

    #region delegate-stub-lastcallargs
    [Fact]
    public void LastCallArgs_MultipleParameters()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();
        stub.Interceptor.OnCall((name, age) => $"{name}: {age}");

        MessageBuilder builder = stub;
        builder("Alice", 30);
        builder("Bob", 25);

        // LastCallArgs provides named tuple access
        var args = stub.Interceptor.LastCallArgs;
        Assert.NotNull(args);
        Assert.Equal("Bob", args.Value.name);
        Assert.Equal(25, args.Value.age);
    }
    #endregion

    #region delegate-stub-callcount
    [Fact]
    public void CallCount_VerifyWithTimes()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });

        NotifyCallback callback = stub;
        callback();
        callback();
        callback();

        // Verify invocation count using Times constraints
        // Note: Use Verify(Times.Exactly(n)) instead of CallCount property
        stub.Interceptor.Verify(Times.Exactly(3));
    }
    #endregion
}

// =============================================================================
// Generic Delegate Samples
// =============================================================================

public class GenericDelegateTests
{
    #region delegate-stub-closed-generic
    [Fact]
    public void ClosedGeneric_FullySpecifiedTypeArgs()
    {
        // Closed generic: type arguments specified at stub definition
        var stub = new DelegateStubTests.Stubs.Factory();
        stub.Interceptor.OnCall(() => "generated value");

        // Use as Factory<string>
        Factory<string> factory = stub;
        var result = factory();

        Assert.Equal("generated value", result);
        stub.Interceptor.Verify();
    }
    #endregion

    #region delegate-stub-open-generic
    [Fact]
    public void OpenGeneric_ReuseWithAnyTypeArg()
    {
        // Open generic: create stub with any type argument
        var stringFactory = new OpenGenericDelegateTest.Stubs.Factory<string>();
        stringFactory.Interceptor.OnCall(() => "hello");

        var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
        intFactory.Interceptor.OnCall(() => 42);

        // Each stub instance is independent
        Factory<string> sf = stringFactory;
        Factory<int> intf = intFactory;

        Assert.Equal("hello", sf());
        Assert.Equal(42, intf());
    }
    #endregion

    #region delegate-stub-generic-constraints
    [Fact]
    public void GenericConstraints_PreservedAtCompileTime()
    {
        // ConstrainedFactory<T> requires T : new()
        // Compiler enforces this when creating the stub
        var productFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<Product>();
        productFactory.Interceptor.OnCall(() => new Product { Id = 1, Name = "Widget" });

        ConstrainedFactory<Product> factory = productFactory;
        var product = factory();

        Assert.Equal("Widget", product.Name);

        // This would NOT compile because string has no parameterless constructor:
        // var invalidFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<string>();
    }
    #endregion
}

// =============================================================================
// Reset Sample
// =============================================================================

public class ResetTests
{
    #region delegate-stub-reset
    [Fact]
    public void Reset_ClearsTrackingPreservesConfiguration()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

        Formatter format = stub;
        format("hello");
        format("world");

        // Before reset: verify calls were tracked
        stub.Interceptor.Verify(Times.Exactly(2));
        Assert.Equal("world", stub.Interceptor.LastCallArg);

        // Reset clears tracking state
        stub.Interceptor.Reset();

        // After reset: tracking cleared
        stub.Interceptor.Verify(Times.Never);
        Assert.Null(stub.Interceptor.LastCallArg);

        // Configuration preserved: OnCall still works
        var result = format("test");
        Assert.Equal("TEST", result);
    }
    #endregion
}

// =============================================================================
// Implicit Conversion Samples
// =============================================================================

public class ImplicitConversionTests
{
    #region delegate-stub-implicit-conversion
    [Fact]
    public void ImplicitConversion_DirectAssignment()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

        // Implicit conversion - no cast required
        Formatter format = stub;

        // Use the delegate
        var result = format("hello");
        Assert.Equal("HELLO", result);
    }
    #endregion

    #region delegate-stub-method-parameter
    [Fact]
    public void MethodParameter_SeamlessSubstitution()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => $"[{input}]");

        // Pass stub directly to method expecting Formatter
        var result = ProcessWithFormatter(stub);

        Assert.Equal("[test]", result);
        stub.Interceptor.Verify();
    }

    private static string ProcessWithFormatter(Formatter formatter)
    {
        return formatter("test");
    }
    #endregion
}

// =============================================================================
// Real-World Example Samples
// =============================================================================

public class RealWorldExampleTests
{
    #region delegate-stub-validation-rule
    [Fact]
    public void ValidationRule_StubValidationPredicate()
    {
        var stub = new DelegateStubTests.Stubs.IsUniqueRule();

        // Configure validation: "admin" is taken, others are available
        stub.Interceptor.OnCall((value) => value != "admin");

        IsUniqueRule isUnique = stub;

        // Test validation logic
        Assert.False(isUnique("admin"));  // taken
        Assert.True(isUnique("newuser")); // available

        // Verify both checks were performed
        stub.Interceptor.Verify(Times.Exactly(2));
    }
    #endregion

    #region delegate-stub-factory
    [Fact]
    public void Factory_StubObjectCreation()
    {
        // Use separate test class to avoid naming collision with Factory<string>
        var stub = new ProductFactoryTest.Stubs.Factory();
        var testProduct = new Product { Id = 42, Name = "Test Widget", Price = 9.99m };

        // Configure factory to return test instance
        stub.Interceptor.OnCall(() => testProduct);

        Factory<Product> factory = stub;

        // Test code that uses the factory
        var product = factory();

        Assert.Same(testProduct, product);
        Assert.Equal("Test Widget", product.Name);
        stub.Interceptor.Verify();
    }
    #endregion

    #region delegate-stub-event-callback
    [Fact]
    public void EventCallback_VerifyEventRaised()
    {
        var stub = new DelegateStubTests.Stubs.EventCallback();

        // Track received events
        DomainEvent? receivedEvent = null;
        stub.Interceptor.OnCall((evt) => receivedEvent = evt);

        EventCallback handler = stub;

        // Simulate event being raised
        var testEvent = new DomainEvent
        {
            EventType = "UserCreated",
            Payload = new { UserId = 1, Name = "Alice" }
        };
        handler(testEvent);

        // Verify callback received correct event
        stub.Interceptor.Verify();
        Assert.NotNull(receivedEvent);
        Assert.Equal("UserCreated", receivedEvent.EventType);
    }
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

/// <summary>
/// Validator that uses delegate rules for validation.
/// </summary>
public class UsernameValidator
{
    private readonly IsUniqueRule _isUnique;
    private readonly IsValidFormatRule _isValidFormat;

    public UsernameValidator(IsUniqueRule isUnique, IsValidFormatRule isValidFormat)
    {
        _isUnique = isUnique;
        _isValidFormat = isValidFormat;
    }

    public (bool IsValid, string? Error) Validate(string username)
    {
        if (!_isValidFormat(username))
            return (false, "Invalid format");

        if (!_isUnique(username))
            return (false, "Username already taken");

        return (true, null);
    }
}

public class CompleteExampleTests
{
    #region delegate-stub-complete-example
    [Fact]
    public void CompleteExample_ValidationWithMultipleRules()
    {
        // Arrange - create delegate stubs
        var uniqueStub = new DelegateStubTests.Stubs.IsUniqueRule();
        var formatStub = new DelegateStubTests.Stubs.IsValidFormatRule();

        // Configure rules:
        // - Format: must be at least 3 characters
        // - Unique: "admin" and "root" are taken
        formatStub.Interceptor.OnCall((value) => value.Length >= 3);
        uniqueStub.Interceptor.OnCall((value) => value != "admin" && value != "root");

        // Create validator with stubbed rules
        var validator = new UsernameValidator(uniqueStub, formatStub);

        // Act & Assert - test various scenarios
        var (valid1, error1) = validator.Validate("ab");
        Assert.False(valid1);
        Assert.Equal("Invalid format", error1);

        var (valid2, error2) = validator.Validate("admin");
        Assert.False(valid2);
        Assert.Equal("Username already taken", error2);

        var (valid3, error3) = validator.Validate("alice");
        Assert.True(valid3);
        Assert.Null(error3);

        // Verify rules were invoked
        formatStub.Interceptor.Verify(Times.Exactly(3)); // All 3 usernames checked
        uniqueStub.Interceptor.Verify(Times.Exactly(2)); // Only valid formats check uniqueness
    }
    #endregion
}
