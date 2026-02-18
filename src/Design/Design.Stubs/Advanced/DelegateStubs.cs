// -----------------------------------------------------------------------------
// Design.Stubs - Delegate Stubs
// -----------------------------------------------------------------------------
// This file demonstrates the inline delegate stub pattern:
// - [KnockOff<DelegateType>] for delegate stubs
// - Implicit conversion to delegate type
// - Interceptor for configuration and verification
// - Return, Call, When matching
// - Void vs returning delegates
// -----------------------------------------------------------------------------

using Design.Domain.Delegates;
using KnockOff;

namespace Design.Stubs.Advanced;

// =============================================================================
// DELEGATE STUBS
// =============================================================================

[KnockOff<ArithmeticOperation>]
[KnockOff<LogAction>]
[KnockOff<SimpleAction>]
[KnockOff<Factory<string>>]
public partial class DelegateStubsDemo
{
    // =========================================================================
    // Inline Delegate Pattern
    // =========================================================================
    // DESIGN DECISION: [KnockOff<DelegateType>] generates a stub class for the
    // delegate type. The stub can be implicitly converted to the delegate type.
    //
    // GENERATOR BEHAVIOR: For delegate ArithmeticOperation:
    //
    //   public delegate int ArithmeticOperation(int a, int b);
    //
    // Generates:
    //
    //   public sealed class ArithmeticOperation : IKnockOffStub
    //   {
    //       public ArithmeticOperationInterceptor Interceptor { get; }
    //       private int Invoke(int a, int b) { ... }
    //       public static implicit operator ArithmeticOperation(ArithmeticOperation stub) => stub.Invoke;
    //   }
    // =========================================================================

    public void DelegateStub_BasicUsage()
    {
        // Create stub for delegate
        var stub = new Stubs.ArithmeticOperation();

        // Configure via interceptor
        stub.Interceptor.Return(42);

        // Implicit conversion to delegate type
        ArithmeticOperation operation = stub;

        // Invoke the delegate
        var result = operation(2, 3); // Returns 42
    }

    // =========================================================================
    // Interceptor.Return(value) - Constant Return Value
    // =========================================================================
    // DESIGN DECISION: Return(value) configures a constant return value.
    // The delegate always returns this value regardless of arguments.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public InterceptorInterceptor Return(int value) { ... }
    // =========================================================================

    public void Returns_ConstantValue()
    {
        var stub = new Stubs.ArithmeticOperation();

        stub.Interceptor.Return(100);

        ArithmeticOperation operation = stub;

        var r1 = operation(1, 2);  // Returns 100
        var r2 = operation(10, 20); // Returns 100
    }

    // =========================================================================
    // Interceptor.Call(callback) - Dynamic Behavior
    // =========================================================================
    // DESIGN DECISION: Call(callback) allows full control over delegate
    // behavior. The callback receives the same arguments as the delegate.
    //
    // GENERATOR BEHAVIOR: The fully generated interceptor class provides typed
    // Call methods:
    //
    //   public MethodCallBuilderImpl Call(Func<(int a, int b), int> callback) { ... }
    // =========================================================================

    public void Returns_DynamicBehavior()
    {
        var stub = new Stubs.ArithmeticOperation();

        // Configure to actually add the numbers
        stub.Interceptor.Call((a, b) => a + b);

        ArithmeticOperation operation = stub;

        var r1 = operation(2, 3);  // Returns 5
        var r2 = operation(10, 20); // Returns 30
    }

    // =========================================================================
    // LastArgs - Argument Tracking
    // =========================================================================
    // DESIGN DECISION: The interceptor tracks the last call arguments via
    // LastArgs property. For multi-argument delegates, this is a tuple.
    // For single-argument delegates, it's LastArg (singular).
    //
    // GENERATOR BEHAVIOR:
    //
    //   public (int? a, int? b)? LastArgs { get; private set; }
    //   // or for single arg:
    //   public string? LastArg { get; private set; }
    // =========================================================================

    public void LastArgs_Tracking()
    {
        var stub = new Stubs.ArithmeticOperation();
        stub.Interceptor.Return(0);

        ArithmeticOperation operation = stub;

        operation(5, 10);

        // Check the arguments
        var args = stub.Interceptor.LastArgs;
        // args?.a == 5
        // args?.b == 10
    }

    // =========================================================================
    // Void Delegates - LogAction Example
    // =========================================================================
    // DESIGN DECISION: Void delegates don't have Return(). They only have
    // Call(callback) for configuring behavior.
    //
    // GENERATOR BEHAVIOR: The fully generated interceptor class provides typed
    // Call methods:
    //
    //   public MethodCallBuilderImpl Call(Action<string> callback) { ... }
    // =========================================================================

    public void VoidDelegate_ExecuteOnly()
    {
        var stub = new Stubs.LogAction();
        var logged = new List<string>();

        // Configure to capture log messages
        stub.Interceptor.Call(msg => logged.Add(msg));

        LogAction logger = stub;

        logger("Hello");
        logger("World");

        // logged contains ["Hello", "World"]
    }

    // =========================================================================
    // Single Argument - LastArg
    // =========================================================================
    // DESIGN DECISION: Single-argument delegates use LastArg (singular)
    // instead of LastArgs.
    // =========================================================================

    public void SingleArg_LastArg()
    {
        var stub = new Stubs.LogAction();

        LogAction logger = stub;

        logger("Test message");

        // Single argument access
        var arg = stub.Interceptor.LastArg;
        // arg == "Test message"
    }

    // =========================================================================
    // No-Argument Delegates - SimpleAction Example
    // =========================================================================
    // DESIGN DECISION: No-argument delegates have no LastArg/LastArgs.
    // They only track call count.
    //
    // Note: SimpleAction is a void delegate with no parameters.
    // =========================================================================

    public void NoArgDelegate_CallCountOnly()
    {
        var stub = new Stubs.SimpleAction();
        var callCount = 0;

        stub.Interceptor.Call(() => callCount++);

        SimpleAction action = stub;

        action();
        action();
        action();

        // callCount == 3
        stub.Interceptor.Verify(Called.Exactly(3));
    }

    // =========================================================================
    // Generic Delegates - Factory<T> Example
    // =========================================================================
    // DESIGN DECISION: Generic delegates use closed generics in the attribute.
    // [KnockOff<Factory<string>>] generates a stub for Factory<string>.
    //
    // Note: The stub is generated for the CLOSED generic (Factory<string>),
    // not the open generic (Factory<>).
    // =========================================================================

    public void GenericDelegate_ClosedGeneric()
    {
        // Factory<string> stub - closed generic uses simple name
        var stub = new Stubs.Factory();

        stub.Interceptor.Return("Created item");

        Factory<string> factory = stub;

        var item = factory(); // Returns "Created item"
    }

    // =========================================================================
    // When Matching for Delegates
    // =========================================================================
    // DESIGN DECISION: Delegates support the same When() API as methods.
    // This allows conditional behavior based on arguments.
    // =========================================================================

    public void When_ConditionalBehavior()
    {
        var stub = new Stubs.ArithmeticOperation();

        stub.Interceptor.When(1, 2).Return(100)
            .ThenWhen(3, 4).Return(200)
            .ThenCall((a, b) => a + b);

        ArithmeticOperation operation = stub;

        var r1 = operation(1, 2); // Returns 100
        var r2 = operation(3, 4); // Returns 200
        var r3 = operation(5, 6); // Returns 11 (fallback)
    }

    // =========================================================================
    // When Matching for Void Delegates
    // =========================================================================
    // DESIGN DECISION: Void delegates have When() that returns a chain with
    // .Call() instead of .Return().
    // =========================================================================

    public void When_VoidDelegate()
    {
        var stub = new Stubs.LogAction();
        var important = new List<string>();
        var normal = new List<string>();

        stub.Interceptor.When(msg => msg.StartsWith("IMPORTANT:", StringComparison.Ordinal))
            .Call(msg => important.Add(msg))
            .ThenWhen(msg => true)
            .Call(msg => normal.Add(msg));

        LogAction logger = stub;

        logger("IMPORTANT: Error occurred");
        logger("Info: All good");

        // important contains ["IMPORTANT: Error occurred"]
        // normal contains ["Info: All good"]
    }

    // =========================================================================
    // Verification for Delegates
    // =========================================================================
    // DESIGN DECISION: Delegate interceptors have Verify() just like method
    // interceptors. They also support Called constraints.
    // =========================================================================

    public void Delegate_Verification()
    {
        var stub = new Stubs.ArithmeticOperation();
        stub.Interceptor.Return(0);

        ArithmeticOperation operation = stub;

        operation(1, 2);
        operation(3, 4);

        // Verify call count
        stub.Interceptor.Verify(); // At least once
        stub.Interceptor.Verify(Called.Exactly(2));
    }

    // =========================================================================
    // Reset for Delegates
    // =========================================================================
    // DESIGN DECISION: Reset() clears tracking (call count, LastArgs)
    // but preserves configuration (Return, Call).
    // =========================================================================

    public void Delegate_Reset()
    {
        var stub = new Stubs.ArithmeticOperation();
        stub.Interceptor.Return(42);

        ArithmeticOperation operation = stub;

        operation(1, 2);

        // Reset clears tracking
        stub.Interceptor.Reset();

        // Configuration preserved
        var result = operation(3, 4); // Still returns 42

        // But call count is reset
        stub.Interceptor.Verify(Called.Once); // Only one call after reset
    }

    // =========================================================================
    // DESIGN DECISION: Named Delegates Only
    // =========================================================================
    // KnockOff requires NAMED delegate types. You cannot use Func<>/Action<>
    // directly because they are not distinct types.
    //
    // DID NOT DO THIS: Support Func<>/Action<> directly
    //
    // REJECTED PATTERN:
    //   [KnockOff<Func<int, int, int>>] // Does not work
    //
    // WHY NOT: Func<int, int, int> is not a distinct type - all
    // Func<int, int, int> are the same type. Named delegates provide:
    // - Semantic meaning (ArithmeticOperation vs Func<int,int,int>)
    // - Distinct types for multiple delegates with same signature
    //
    // ACTUAL PATTERN: Define named delegate types and use those
    //   public delegate int ArithmeticOperation(int a, int b);
    //   [KnockOff<ArithmeticOperation>]
    // =========================================================================

    // =========================================================================
    // COMMON MISTAKE: Forgetting Implicit Conversion
    // =========================================================================
    //
    // COMMON MISTAKE: Using stub.Interceptor directly instead of converting
    //
    // The stub must be converted to the delegate type before use.
    // The Interceptor is for configuration, not invocation.
    //
    // WRONG:
    //   var result = stub.Interceptor.??? // No Invoke method
    //
    // RIGHT:
    //   ArithmeticOperation op = stub; // Implicit conversion
    //   var result = op(1, 2);
    // =========================================================================
}
