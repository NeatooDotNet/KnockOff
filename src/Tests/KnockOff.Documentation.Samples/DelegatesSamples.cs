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
/// Two-parameter returning delegate for sequences, When chains, strict mode samples.
/// </summary>
public delegate int Calculate(int a, int b);

/// <summary>
/// Two-parameter void delegate for void When chain samples.
/// </summary>
public delegate void ProcessValues(int a, int b);

/// <summary>
/// Single-parameter returning delegate for sequence callbacks.
/// </summary>
public delegate int Transform(int x);

/// <summary>
/// Async delegate for auto-wrapping samples.
/// </summary>
public delegate Task<int> AsyncTransform(int x);

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
// Named Delegate Example (for Func/Action limitation)
// =============================================================================

#region delegate-func-action-not-supported
// Does NOT work:
// [KnockOff<Func<int, int, int>>]  // Not supported

// Define a named delegate instead:
public delegate int NamedCalculation(int a, int b);

[KnockOff<NamedCalculation>]  // Works!
public partial class NamedDelegateExample { }
#endregion

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
[KnockOff<Calculate>]
[KnockOff<ProcessValues>]
[KnockOff<Transform>]
[KnockOff<AsyncTransform>]
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
// Return Configuration Samples
// =============================================================================

public class ReturnConfigurationTests
{
    [Fact]
    public void CallVoid_ExecutesCustomLogic()
    {
        var stub = new DelegateStubTests.Stubs.NotifyCallback();
        var notified = false;

        #region delegate-stub-oncall-void
        // Configure side effects for void delegate
        stub.Interceptor.Call(() => notified = true);
        #endregion

        NotifyCallback callback = stub;
        callback();

        Assert.True(notified);
    }

    [Fact]
    public void ReturnValue_ReturnsFixedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-stub-oncall-value
        // Return() - pass the return value directly (simpler syntax)
        stub.Interceptor.Return("FORMATTED");
        #endregion

        Formatter format = stub;
        var result = format("any input");

        Assert.Equal("FORMATTED", result);
    }

    [Fact]
    public void ReturnReturn_ReturnsComputedValue()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-stub-oncall-return
        // Return() - compute return value based on input
        stub.Interceptor.Call((input) => input.ToUpperInvariant());
        #endregion

        Formatter format = stub;
        var result = format("hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void ReturnMultiParam_ComputesFromAllParams()
    {
        var stub = new DelegateStubTests.Stubs.MessageBuilder();

        #region delegate-stub-oncall-multi-param
        // Configure with multiple parameters
        stub.Interceptor.Call((name, age) => $"{name} is {age} years old");
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
        stub.Interceptor.Call(() => { });
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
        stub.Interceptor.Call(() => { });
        NotifyCallback callback = stub;
        callback();
        callback();
        callback();

        #region delegate-stub-verification-times
        // Verify with Times constraints
        stub.Interceptor.Verify(Called.Exactly(3));
        stub.Interceptor.Verify(Called.AtLeast(2));
        stub.Interceptor.Verify(Called.AtMost(5));
        #endregion
    }

    [Fact]
    public void Verifiable_VerifyAfterReturn()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();
        stub.Interceptor.Call((input) => input.ToUpperInvariant());
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
        stub.Interceptor.Call((input) => input);
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
        stub.Interceptor.Call((name, age) => $"{name}: {age}");
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
        stub.Interceptor.Call(() => { });
        NotifyCallback callback = stub;
        callback();
        callback();
        callback();

        #region delegate-stub-callcount
        // Verify invocation count using Times constraints
        stub.Interceptor.Verify(Called.Exactly(3));
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
        stub.Interceptor.Call(() => "generated value");
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
        stringFactory.Interceptor.Call(() => "hello");

        var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
        intFactory.Interceptor.Call(() => 42);
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
        productFactory.Interceptor.Call(() => new Product { Id = 1, Name = "Widget" });
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
        stub.Interceptor.Call((input) => input.ToUpperInvariant());
        Formatter format = stub;
        format("hello");
        format("world");

        stub.Interceptor.Verify(Called.Exactly(2));
        Assert.Equal("world", stub.Interceptor.LastArg);

        #region delegate-stub-reset
        // Reset clears tracking state but preserves configuration
        stub.Interceptor.Reset();

        stub.Interceptor.Verify(Called.Never);
        Assert.Null(stub.Interceptor.LastArg);
        Assert.Equal("TEST", format("test")); // Return still works
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
        stub.Interceptor.Call((input) => input.ToUpperInvariant());

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
        stub.Interceptor.Call((input) => $"[{input}]");

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
        stub.Interceptor.Call((value) => value != "admin");
        #endregion

        IsUniqueRule isUnique = stub;
        Assert.False(isUnique("admin"));
        Assert.True(isUnique("newuser"));
        stub.Interceptor.Verify(Called.Exactly(2));
    }

    [Fact]
    public void Factory_StubObjectCreation()
    {
        var stub = new ProductFactoryTest.Stubs.Factory();
        var testProduct = new Product { Id = 42, Name = "Test Widget", Price = 9.99m };

        #region delegate-stub-factory
        // Configure factory to return test instance
        stub.Interceptor.Call(() => testProduct);
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
        stub.Interceptor.Call((evt) => receivedEvent = evt);
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
        formatStub.Interceptor.Call((value) => value.Length >= 3);

        // Configure uniqueness rule: "admin" and "root" are taken
        uniqueStub.Interceptor.Call((value) => value != "admin" && value != "root");

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

        formatStub.Interceptor.Verify(Called.Exactly(3));
        uniqueStub.Interceptor.Verify(Called.Exactly(2));
    }
}

// =============================================================================
// Verifiable Pattern Sample
// =============================================================================

public class VerifiablePatternTests
{
    [Fact]
    public void Verifiable_ChainingOnDelegateInterceptor()
    {
        var stub = new DelegateStubTests.Stubs.Transform();

        #region delegate-verifiable-pattern
        // Mark for verification with Verifiable() chaining
        stub.Interceptor.Call((x) => x * 2).Verifiable();
        stub.Interceptor.Verify(Called.Never); // Not called yet

        Transform transform = stub;
        var result = transform(21);

        // Verify the delegate was called
        stub.Interceptor.Verify(Called.Once);
        #endregion

        Assert.Equal(42, result);
    }
}

// =============================================================================
// Sequence Samples
// =============================================================================

public class SequenceTests
{
    [Fact]
    public void Sequences_ReturnsMultipleValues()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-sequences
        // Return different values on successive calls
        stub.Interceptor.Return(10, 20, 30);
        // Call 1: 10, Call 2: 20, Call 3+: 30 (repeats last)
        #endregion

        Calculate calc = stub;
        Assert.Equal(10, calc(0, 0));
        Assert.Equal(20, calc(0, 0));
        Assert.Equal(30, calc(0, 0));
        Assert.Equal(30, calc(0, 0)); // repeats last
    }

    [Fact]
    public void Sequences_CallbackChain()
    {
        var stub = new DelegateStubTests.Stubs.Transform();

        #region delegate-sequences-callback
        // Callback sequences
        stub.Interceptor
            .Call((x) => x * 1)
            .ThenReturn((x) => x * 2)
            .ThenReturn((x) => x * 3);
        #endregion

        Transform transform = stub;
        Assert.Equal(10, transform(10)); // x * 1
        Assert.Equal(20, transform(10)); // x * 2
        Assert.Equal(30, transform(10)); // x * 3
        Assert.Equal(30, transform(10)); // repeats last
    }

    [Fact]
    public void Sequences_ThenReturn()
    {
        var stub = new DelegateStubTests.Stubs.Transform();

        #region delegate-sequences-thenreturns
        // ThenReturn for fixed values after callback
        stub.Interceptor
            .Call((x) => x)
            .ThenReturn(99);
        #endregion

        Transform transform = stub;
        Assert.Equal(5, transform(5));  // Return
        Assert.Equal(99, transform(5)); // ThenReturn
        Assert.Equal(99, transform(5)); // repeats last
    }

    [Fact]
    public void Sequences_ThenDefault()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-sequences-thendefault
        // ThenDefault: return default(T) after exhaustion instead of repeating
        stub.Interceptor
            .Call((a, b) => 100)
            .ThenReturn((a, b) => 200)
            .ThenDefault();
        // Call 1: 100, Call 2: 200, Call 3+: 0 (default(int))
        #endregion

        Calculate calc = stub;
        Assert.Equal(100, calc(0, 0));
        Assert.Equal(200, calc(0, 0));
        Assert.Equal(0, calc(0, 0)); // default(int)
    }
}

// =============================================================================
// Async Delegate Auto-Wrapping Samples
// =============================================================================

public class AsyncAutoWrappingTests
{
    [Fact]
    public async Task AsyncDelegate_ThreeTierAutoWrapping()
    {
        var stub = new DelegateStubTests.Stubs.AsyncTransform();

        #region delegate-async-auto-wrapping
        // Tier 1: Returns takes inner type - auto-wraps in Task.FromResult
        stub.Interceptor.Return(42);
        #endregion

        AsyncTransform op = stub;
        Assert.Equal(42, await op(10));

        stub.Interceptor.Reset();

        // Tier 2: simplified callback
        stub.Interceptor.Call((int x) => x * 2);
        Assert.Equal(20, await op(10));

        stub.Interceptor.Reset();

        // Tier 3: full delegate
        stub.Interceptor.Call((int x) => Task.FromResult(x * 2));
        Assert.Equal(20, await op(10));
    }
}

// =============================================================================
// When Chain Samples
// =============================================================================

public class WhenChainTests
{
    [Fact]
    public void When_ValueMatching()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-when-value-matching
        // Match specific argument values
        stub.Interceptor.When(1, 2).Return(100)
            .ThenWhen(3, 4).Return(200)
            .ThenCall((a, b) => a + b);  // terminal fallback
        #endregion

        Calculate calc = stub;
        Assert.Equal(100, calc(1, 2));
        Assert.Equal(200, calc(3, 4));
        Assert.Equal(11, calc(5, 6)); // fallback
    }

    [Fact]
    public void When_PredicateMatching()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-when-predicate-matching
        // Match via predicate
        stub.Interceptor.When((int a, int b) => a > 10).Return(999);
        #endregion

        Calculate calc = stub;
        Assert.Equal(999, calc(20, 1));
        Assert.Equal(0, calc(1, 2)); // no match, default
    }

    [Fact]
    public void When_PredicateSingleParam()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-when-predicate-single-param
        // Single-parameter delegate
        stub.Interceptor.When(s => s.Length > 5).Return("LONG");
        #endregion

        Formatter format = stub;
        Assert.Equal("LONG", format("longstring"));
        Assert.Null(format("hi")); // no match, default
    }

    [Fact]
    public void When_Chained()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-when-chained
        stub.Interceptor
            .When("one").Return("ONE")
            .ThenWhen("two").Return("TWO")
            .ThenWhen(s => s.StartsWith("x")).Return("X_PREFIX");
        #endregion

        Formatter format = stub;
        Assert.Equal("ONE", format("one"));
        Assert.Equal("TWO", format("two"));
        Assert.Equal("X_PREFIX", format("xyz"));
    }

    [Fact]
    public void When_VoidDelegateChains()
    {
        var stub = new DelegateStubTests.Stubs.ProcessValues();
        var calls = new List<string>();

        #region delegate-when-void-chains
        stub.Interceptor
            .When(1, 2).Call((a, b) => calls.Add("first"))
            .ThenWhen(3, 4).Call((a, b) => calls.Add("second"));
        #endregion

        ProcessValues process = stub;
        process(1, 2);
        process(3, 4);

        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public void When_ThenNone()
    {
        var stub = new DelegateStubTests.Stubs.Formatter();

        #region delegate-when-thennone
        // After "one" is matched, subsequent calls fall through to default behavior
        stub.Interceptor.When("one").Return("ONE").ThenNone();
        #endregion

        Formatter format = stub;
        Assert.Equal("ONE", format("one"));
        Assert.Null(format("one")); // ThenNone consumed the matcher
    }
}

// =============================================================================
// Strict Mode Samples
// =============================================================================

public class StrictModeTests
{
    [Fact]
    public void StrictMode_ThrowsOnUnconfigured()
    {
        #region delegate-strict-mode
        var stub = new DelegateStubTests.Stubs.Calculate();
        stub.Strict = true;

        Calculate calc = stub;
        Assert.Throws<StubException>(() => calc(1, 2)); // Throws StubException.NotConfigured
        #endregion
    }

    [Fact]
    public void StrictMode_SequenceExhaustion()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-strict-mode-sequences
        stub.Strict = true;
        stub.Interceptor.Return(10, 20);

        Calculate op = stub;
        Assert.Equal(10, op(0, 0)); // first value
        Assert.Equal(20, op(0, 0)); // second value
        Assert.Throws<StubException>(() => op(0, 0)); // Throws StubException.SequenceExhausted
        #endregion
    }
}

// =============================================================================
// Configuration Mutual Exclusivity Sample
// =============================================================================

public class ReturnOverloadsReplaceEachOtherTests
{
    [Fact]
    public void ReturnValue_And_ReturnCallback_LastOneWins()
    {
        var stub = new DelegateStubTests.Stubs.Calculate();

        #region delegate-config-mutual-exclusivity
        stub.Interceptor.Return(42);
        stub.Interceptor.Call((a, b) => a + b); // Clears Return(42)
        #endregion

        Calculate calc = stub;
        Assert.Equal(3, calc(1, 2)); // Return(callback) wins

        stub.Interceptor.Call((a, b) => a + b);
        stub.Interceptor.Return(99);              // Replaces callback
        Assert.Equal(99, calc(1, 2)); // Return(value) wins
    }
}
