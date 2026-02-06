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
    [Fact]
    public void BasicVoid_DelegateStub()
    {
        #region delegate-stub-basic-void
        // Create stub, convert to delegate, invoke, and verify
        var stub = new BasicVoidDelegateTest.Stubs.OnComplete();
        OnComplete callback = stub;
        callback();
        stub.Interceptor.Verify();
        #endregion
    }

    [Fact]
    public void WithReturn_TracksArgAndReturnsDefault()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        Formatter format = stub;
        var result = format("hello");

        #region delegate-stub-with-return
        // Default return value is null; LastArg tracks the argument
        Assert.Null(result);
        Assert.Equal("hello", stub.Interceptor.LastArg);
        #endregion
    }

    [Fact]
    public void MultiParam_TracksLastArgs()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();
        MessageBuilder builder = stub;
        builder("Alice", 30);

        #region delegate-stub-multi-param
        // Access arguments via named tuple
        Assert.Equal("Alice", stub.Interceptor.LastArgs!.Value.name);
        Assert.Equal(30, stub.Interceptor.LastArgs!.Value.age);
        #endregion
    }
}

// =============================================================================
// OnCall Configuration Samples
// =============================================================================

public class OnCallConfigurationTests
{
    [Fact]
    public void OnCallVoid_ExecutesCustomLogic()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        var notified = false;

        #region delegate-stub-oncall-void
        // Configure side effects for void delegate
        stub.Interceptor.OnCall(() => notified = true);
        #endregion

        NotifyCallback callback = stub;
        callback();

        Assert.True(notified);
    }

    [Fact]
    public void OnCallValue_ReturnsFixedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-stub-oncall-value
        // Returns() - pass the return value directly (simpler syntax)
        stub.Interceptor.Returns("FORMATTED");
        #endregion

        Formatter format = stub;
        var result = format("any input");

        Assert.Equal("FORMATTED", result);
    }

    [Fact]
    public void OnCallReturn_ReturnsComputedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-stub-oncall-return
        // OnCall() - compute return value based on input
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());
        #endregion

        Formatter format = stub;
        var result = format("hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void OnCallMultiParam_ComputesFromAllParams()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();

        #region delegate-stub-oncall-multi-param
        // Configure with multiple parameters
        stub.Interceptor.OnCall((name, age) => $"{name} is {age} years old");
        #endregion

        MessageBuilder builder = stub;
        var result = builder("Bob", 25);

        Assert.Equal("Bob is 25 years old", result);
    }
}

// =============================================================================
// Verification Samples
// =============================================================================

public class VerificationTests
{
    [Fact]
    public void Verify_ThrowsIfNeverCalled()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });
        NotifyCallback callback = stub;
        callback();

        #region delegate-stub-verification-basic
        // Verify() passes - delegate was called at least once
        stub.Interceptor.Verify();
        #endregion
    }

    [Fact]
    public void Verify_WithTimesConstraints()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });
        NotifyCallback callback = stub;
        callback();
        callback();
        callback();

        #region delegate-stub-verification-times
        // Verify with Times constraints
        stub.Interceptor.Verify(Times.Exactly(3));
        stub.Interceptor.Verify(Times.AtLeast(2));
        stub.Interceptor.Verify(Times.AtMost(5));
        #endregion
    }

    [Fact]
    public void Verifiable_VerifyAfterOnCall()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());
        Formatter format = stub;
        format("test");

        #region delegate-stub-verifiable
        // Delegate interceptors use Verify() directly (no Verifiable() chaining)
        stub.Interceptor.Verify();
        #endregion
    }
}

// =============================================================================
// Tracking Samples
// =============================================================================

public class TrackingTests
{
    [Fact]
    public void LastArg_SingleParameter()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input);
        Formatter format = stub;
        format("first");
        format("second");

        #region delegate-stub-lastcallarg
        // LastArg captures the most recent argument
        Assert.Equal("second", stub.Interceptor.LastArg);
        #endregion
    }

    [Fact]
    public void LastArgs_MultipleParameters()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();
        stub.Interceptor.OnCall((name, age) => $"{name}: {age}");
        MessageBuilder builder = stub;
        builder("Alice", 30);
        builder("Bob", 25);

        #region delegate-stub-lastcallargs
        // LastArgs provides named tuple access
        Assert.Equal("Bob", stub.Interceptor.LastArgs!.Value.name);
        Assert.Equal(25, stub.Interceptor.LastArgs!.Value.age);
        #endregion
    }

    [Fact]
    public void CallCount_VerifyWithTimes()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        stub.Interceptor.OnCall(() => { });
        NotifyCallback callback = stub;
        callback();
        callback();
        callback();

        #region delegate-stub-callcount
        // Verify invocation count using Times constraints
        stub.Interceptor.Verify(Times.Exactly(3));
        #endregion
    }
}

// =============================================================================
// Generic Delegate Samples
// =============================================================================

public class GenericDelegateTests
{
    [Fact]
    public void ClosedGeneric_FullySpecifiedTypeArgs()
    {
        #region delegate-stub-closed-generic
        // Closed generic: type arguments specified at stub definition
        var stub = new DelegateStubTests.Stubs.Factory();
        stub.Interceptor.OnCall(() => "generated value");
        Factory<string> factory = stub;
        #endregion

        var result = factory();
        Assert.Equal("generated value", result);
        stub.Interceptor.Verify();
    }

    [Fact]
    public void OpenGeneric_ReuseWithAnyTypeArg()
    {
        #region delegate-stub-open-generic
        // Open generic: create stub with any type argument
        var stringFactory = new OpenGenericDelegateTest.Stubs.Factory<string>();
        stringFactory.Interceptor.OnCall(() => "hello");

        var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
        intFactory.Interceptor.OnCall(() => 42);
        #endregion

        Factory<string> sf = stringFactory;
        Factory<int> intf = intFactory;
        Assert.Equal("hello", sf());
        Assert.Equal(42, intf());
    }

    [Fact]
    public void GenericConstraints_PreservedAtCompileTime()
    {
        #region delegate-stub-generic-constraints
        // ConstrainedFactory<T> requires T : new() - compiler enforces this
        var productFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<Product>();
        productFactory.Interceptor.OnCall(() => new Product { Id = 1, Name = "Widget" });
        #endregion

        ConstrainedFactory<Product> factory = productFactory;
        var product = factory();
        Assert.Equal("Widget", product.Name);
    }
}

// =============================================================================
// Reset Sample
// =============================================================================

public class ResetTests
{
    [Fact]
    public void Reset_ClearsTrackingPreservesConfiguration()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());
        Formatter format = stub;
        format("hello");
        format("world");

        stub.Interceptor.Verify(Times.Exactly(2));
        Assert.Equal("world", stub.Interceptor.LastArg);

        #region delegate-stub-reset
        // Reset clears tracking state but preserves configuration
        stub.Interceptor.Reset();

        stub.Interceptor.Verify(Times.Never);
        Assert.Null(stub.Interceptor.LastArg);
        Assert.Equal("TEST", format("test")); // OnCall still works
        #endregion
    }
}

// =============================================================================
// Implicit Conversion Samples
// =============================================================================

public class ImplicitConversionTests
{
    [Fact]
    public void ImplicitConversion_DirectAssignment()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

        #region delegate-stub-implicit-conversion
        // Implicit conversion - no cast required
        Formatter format = stub;
        var result = format("hello");
        #endregion

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void MethodParameter_SeamlessSubstitution()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.OnCall((input) => $"[{input}]");

        #region delegate-stub-method-parameter
        // Pass stub directly to method expecting Formatter
        var result = ProcessWithFormatter(stub);
        #endregion

        Assert.Equal("[test]", result);
        stub.Interceptor.Verify();
    }

    private static string ProcessWithFormatter(Formatter formatter)
    {
        return formatter("test");
    }
}

// =============================================================================
// Real-World Example Samples
// =============================================================================

public class RealWorldExampleTests
{
    [Fact]
    public void ValidationRule_StubValidationPredicate()
    {
        var stub = new DelegateStubTests.Stubs.IsUniqueRule();

        #region delegate-stub-validation-rule
        // Configure validation: "admin" is taken, others are available
        stub.Interceptor.OnCall((value) => value != "admin");
        #endregion

        IsUniqueRule isUnique = stub;
        Assert.False(isUnique("admin"));
        Assert.True(isUnique("newuser"));
        stub.Interceptor.Verify(Times.Exactly(2));
    }

    [Fact]
    public void Factory_StubObjectCreation()
    {
        var stub = new ProductFactoryTest.Stubs.Factory();
        var testProduct = new Product { Id = 42, Name = "Test Widget", Price = 9.99m };

        #region delegate-stub-factory
        // Configure factory to return test instance
        stub.Interceptor.OnCall(() => testProduct);
        Factory<Product> factory = stub;
        #endregion

        var product = factory();
        Assert.Same(testProduct, product);
        Assert.Equal("Test Widget", product.Name);
        stub.Interceptor.Verify();
    }

    [Fact]
    public void EventCallback_VerifyEventRaised()
    {
        var stub = new DelegateStubTests.Stubs.EventCallback();
        DomainEvent? receivedEvent = null;

        #region delegate-stub-event-callback
        // Track received events
        stub.Interceptor.OnCall((evt) => receivedEvent = evt);
        #endregion

        EventCallback handler = stub;
        var testEvent = new DomainEvent
        {
            EventType = "UserCreated",
            Payload = new { UserId = 1, Name = "Alice" }
        };
        handler(testEvent);

        stub.Interceptor.Verify();
        Assert.NotNull(receivedEvent);
        Assert.Equal("UserCreated", receivedEvent.EventType);
    }
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
    [Fact]
    public void CompleteExample_ValidationWithMultipleRules()
    {
        var uniqueStub = new DelegateStubTests.Stubs.IsUniqueRule();
        var formatStub = new DelegateStubTests.Stubs.IsValidFormatRule();

        #region delegate-stub-complete-example
        // Configure format rule: must be at least 3 characters
        formatStub.Interceptor.OnCall((value) => value.Length >= 3);

        // Configure uniqueness rule: "admin" and "root" are taken
        uniqueStub.Interceptor.OnCall((value) => value != "admin" && value != "root");

        // Create validator with stubbed rules
        var validator = new UsernameValidator(uniqueStub, formatStub);
        #endregion

        var (valid1, error1) = validator.Validate("ab");
        Assert.False(valid1);
        Assert.Equal("Invalid format", error1);

        var (valid2, error2) = validator.Validate("admin");
        Assert.False(valid2);
        Assert.Equal("Username already taken", error2);

        var (valid3, error3) = validator.Validate("alice");
        Assert.True(valid3);
        Assert.Null(error3);

        formatStub.Interceptor.Verify(Times.Exactly(3));
        uniqueStub.Interceptor.Verify(Times.Exactly(2));
    }
}
