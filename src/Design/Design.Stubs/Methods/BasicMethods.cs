// -----------------------------------------------------------------------------
// Design.Stubs - Basic Method Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental method stubbing APIs:
// - Return(value) for constant return values
// - Call(callback) for dynamic returns based on arguments
// - Async method handling with auto-wrapping
// - Void method handling
// - Argument capture (LastArg, LastArgs)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// BASIC METHOD STUB CONFIGURATION
// =============================================================================

[KnockOff<ICalculator>]
[KnockOff<IDataService>]
public partial class BasicMethodsDemo
{
    // =========================================================================
    // Return(value) - Constant Return Value
    // =========================================================================
    // DESIGN DECISION: Return(value) sets a constant return value for all calls.
    // This is the simplest configuration - no callback, no argument inspection.
    //
    // GENERATOR BEHAVIOR: The generated interceptor is fully generated (no
    // generic base types in tooltips). It stores the value internally:
    //
    //   public sealed class AddInterceptor : MethodInterceptorRuntime
    //   {
    //       public AddInterceptor Return(int value) { ... }
    //   }
    // =========================================================================

    public void Returns_SetsConstantValue()
    {
        var stub = new Stubs.ICalculator();

        // Configure to always return 42
        stub.Add.Return(42);

        ICalculator calc = stub;
        var result = calc.Add(100, 200); // Returns 42, ignores arguments

        // Return() is chainable (returns the interceptor)
        stub.Subtract.Return(10).Verifiable();
    }

    // =========================================================================
    // Call(callback) - Dynamic Return Based on Arguments
    // =========================================================================
    // DESIGN DECISION: Call(callback) receives typed arguments directly.
    // For 2+ params, the callback receives individual named parameters: (int a, int b) => a + b
    // For 0-1 params, the callback receives the raw type.
    //
    // This differs from NSubstitute's callInfo.Arg<T>() pattern which requires
    // extracting arguments from an object array at runtime.
    //
    // DID NOT DO THIS: Use untyped argument access
    //
    // REJECTED PATTERN (NSubstitute-style):
    //   stub.Add.Call(callInfo => callInfo.Arg<int>(0) + callInfo.Arg<int>(1));
    //
    // WHY NOT: Source generators can provide typed access at compile time.
    // Typed callbacks are safer and provide IntelliSense support.
    // =========================================================================

    public void Returns_ReceivesTypedArguments()
    {
        var stub = new Stubs.ICalculator();

        // Callback receives actual method arguments as named parameters (IntelliSense shows names)
        stub.Add.Call((int a, int b) => a + b);

        ICalculator calc = stub;
        var result = calc.Add(3, 5); // Returns 8 (3 + 5)
    }

    public void Returns_CanThrowExceptions()
    {
        var stub = new Stubs.ICalculator();

        // Callbacks can throw exceptions for error testing
        stub.Divide.Call((int a, int b) =>
        {
            if (b == 0)
                throw new DivideByZeroException();
            return a / b;
        });

        ICalculator calc = stub;
        // calc.Divide(10, 0) would throw DivideByZeroException
    }

    // =========================================================================
    // OVERALL PRINCIPLE: Configuration Methods — Last One Wins
    // =========================================================================
    //
    // All configuration methods use direct replacement. Calling any
    // configuration method replaces the previous configuration of the same kind:
    //
    //   - Return(value) and Call(callback) replace each other
    //   - Multiple Call(callback) calls — last wins
    //   - Multiple Get(value) or Get(callback) calls — last wins
    //   - Multiple Set(callback) calls — last wins
    //   - Multiple When() calls — last wins (replaces previous When chain)
    //
    // Within a When chain, ThenWhen() accumulates matchers. But calling
    // When() again as a new entry point replaces the entire chain.
    //
    // KNOWN BUG: When() currently accumulates like ThenWhen() instead of
    // replacing. See docs/todos/when-entry-point-should-clear-chain.md.
    //
    // COMMON MISTAKE: Expecting Return(value) and Call(callback) to combine
    //
    // WRONG:
    //   stub.Add.Call((int a, int b) => a + b);
    //   stub.Add.Return(42);  // This REPLACES the callback, does not combine
    //
    // NOTE: Return(value) sets a constant value. Call(callback) provides a
    // callback for dynamic behavior. Both work on void and non-void methods.
    // For non-void: Call(callback) returns the callback's result.
    // For void: Call(callback) executes the callback for side effects.
    // =========================================================================

    public void Returns_ValueAndCallback_AreExclusive()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Call((int a, int b) => a + b);  // Set callback
        stub.Add.Return(42);               // REPLACES callback with constant

        ICalculator calc = stub;
        var result = calc.Add(3, 5); // Returns 42, not 8
    }

    // =========================================================================
    // Void Methods
    // =========================================================================
    // DESIGN DECISION: Void methods use Call(callback) for side effects and
    // Verify() for call count verification. No Return() since there's nothing
    // to return.
    //
    // GENERATOR BEHAVIOR: Void methods generate a fully generated interceptor
    // class extending non-generic MethodInterceptorRuntime:
    //
    //   public sealed class ResetInterceptor : MethodInterceptorRuntime
    //   {
    //       public ResetInterceptor Call(Action callback) { ... }
    //       public void Verify() { ... }
    //       public void Verify(Called called) { ... }
    //   }
    // =========================================================================

    public void VoidMethods_ExecuteForSideEffects()
    {
        var stub = new Stubs.ICalculator();
        var resetCount = 0;

        // Void Call uses Action, not Func
        stub.Reset.Call(() => resetCount++);

        ICalculator calc = stub;
        calc.Reset();
        calc.Reset();

        // resetCount is now 2
        stub.Reset.Verify(Called.Exactly(2));
    }

    // =========================================================================
    // Argument Capture (LastArg / LastArgs)
    // =========================================================================
    // DESIGN DECISION: Every method interceptor tracks the last call's arguments.
    // - LastArg: For single-parameter methods (e.g., GetById(int id))
    // - LastArgs: For multi-parameter methods, returns a named tuple
    //
    // GENERATOR BEHAVIOR: The fully generated interceptor tracks arguments:
    //
    //   public sealed class AddInterceptor : MethodInterceptorRuntime
    //   {
    //       // For methods with multiple parameters (named tuple)
    //       public (int a, int b)? LastArgs { get; private set; }
    //   }
    //
    // For single-parameter methods:
    //   public int? LastArg { get; private set; }  // Not LastArgs
    // =========================================================================

    public void ArgumentCapture_LastArgs_ForMultipleParameters()
    {
        var stub = new Stubs.ICalculator();
        stub.Add.Return(0);

        ICalculator calc = stub;
        calc.Add(3, 5);
        calc.Add(10, 20);

        // LastArgs captures the most recent call's arguments
        // Note: The interceptor exposes LastArgs, which is nullable
        var args = stub.Add.LastArgs;
        // args == (10, 20)

        // For Call() callbacks, you get LastArgs via the builder interface:
        var builder = stub.Subtract.Call((int a, int b) => a - b);
        calc.Subtract(100, 25);
        var subtractArgs = builder.LastArgs;
        // subtractArgs == (100, 25)
    }

    // =========================================================================
    // Async Methods - Auto-Wrapping
    // =========================================================================
    // DESIGN DECISION: For async methods (Task<T>, ValueTask<T>), Return()
    // automatically wraps the value with Task.FromResult or ValueTask.FromResult.
    //
    // This avoids boilerplate: Return(Task.FromResult("value"))
    // Instead: Return("value")
    //
    // DID NOT DO THIS: Require explicit Task.FromResult wrapping
    //
    // REJECTED PATTERN:
    //   stub.GetDataAsync.Return(Task.FromResult("data"));  // Verbose
    //
    // ACTUAL PATTERN:
    //   stub.GetDataAsync.Return("data");  // Auto-wrapped
    //
    // WHY NOT: Reducing boilerplate improves readability. The method signature
    // already indicates it's async - forcing explicit wrapping adds noise.
    //
    // GENERATOR BEHAVIOR: For Task<T> methods, Return() wraps automatically:
    //
    //   public GetDataAsyncInterceptor Return(string? value)
    //   {
    //       _returnValue = Task.FromResult(value);
    //       return this;
    //   }
    // =========================================================================

    public async Task AsyncMethods_Returns_AutoWraps()
    {
        var stub = new Stubs.IDataService();

        // No Task.FromResult needed - auto-wrapped
        stub.GetDataAsync.Return("test data");

        IDataService service = stub;
        var result = await service.GetDataAsync(1);
        // result == "test data"
    }

    public async Task AsyncMethods_Returns_CallbackAutoWraps()
    {
        var stub = new Stubs.IDataService();

        // Call callback for async methods: callback returns T, not Task<T>
        stub.GetDataAsync.Call((id) => $"Data for ID {id}");

        IDataService service = stub;
        var result = await service.GetDataAsync(42);
        // result == "Data for ID 42"
    }

    // =========================================================================
    // Void Async Methods (Task return)
    // =========================================================================
    // DESIGN DECISION: For async void methods (Task return, no value),
    // Call() receives the arguments and Return() is not available.
    //
    // GENERATOR BEHAVIOR: Task-returning void methods generate a fully
    // generated interceptor class:
    //
    //   public sealed class SaveDataAsyncInterceptor : MethodInterceptorRuntime
    //   {
    //       public SaveDataAsyncInterceptor Call(Action<string> callback) { ... }
    //       // Returns Task.CompletedTask when not configured
    //   }
    // =========================================================================

    public async Task VoidAsyncMethods_Execute()
    {
        var stub = new Stubs.IDataService();
        string? savedData = null;

        stub.SaveDataAsync.Call((data) => savedData = data);

        IDataService service = stub;
        await service.SaveDataAsync("important data");
        // savedData == "important data"
    }

    // =========================================================================
    // Reset() - Clear Tracking State
    // =========================================================================
    // DESIGN DECISION: Reset() clears tracking state without changing configuration.
    // - LastArg/LastArgs reset to default
    // - Call count resets to 0
    // - Return/Call configuration is PRESERVED
    //
    // DID NOT DO THIS: Reset() clears configuration too
    //
    // REJECTED PATTERN:
    //   stub.Add.Reset(); // Would clear Return(42) too
    //
    // WHY NOT: Separating tracking reset from configuration allows reusing
    // stubs across multiple test scenarios without reconfiguring.
    // =========================================================================

    public void Reset_ClearsTrackingButPreservesConfiguration()
    {
        var stub = new Stubs.ICalculator();
        stub.Add.Return(42);

        ICalculator calc = stub;
        calc.Add(1, 2);
        // stub.Add.LastArgs == (1, 2)

        stub.Add.Reset();
        // stub.Add.LastArgs == (0, 0) (default)
        // But Return(42) is still in effect

        var result = calc.Add(3, 4);
        // result == 42 (configuration preserved)
    }
}
