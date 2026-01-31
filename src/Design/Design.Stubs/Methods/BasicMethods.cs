// -----------------------------------------------------------------------------
// Design.Stubs - Basic Method Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental method stubbing APIs:
// - Returns(value) for constant return values
// - OnCall(callback) for dynamic returns based on arguments
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
    // Returns(value) - Constant Return Value
    // =========================================================================
    // DESIGN DECISION: Returns(value) sets a constant return value for all calls.
    // This is the simplest configuration - no callback, no argument inspection.
    //
    // GENERATOR BEHAVIOR: The generated interceptor stores the value internally:
    //
    //   public class AddInterceptor : MethodInterceptor<int, (int a, int b), int>
    //   {
    //       private int _returnValue;
    //
    //       public AddInterceptor Returns(int value)
    //       {
    //           _returnValue = value;
    //           _callback = null;  // Clears any OnCall callback
    //           return this;
    //       }
    //   }
    // =========================================================================

    public void Returns_SetsConstantValue()
    {
        var stub = new Stubs.ICalculator();

        // Configure to always return 42
        stub.Add.Returns(42);

        ICalculator calc = stub;
        var result = calc.Add(100, 200); // Returns 42, ignores arguments

        // Returns() is chainable (returns the interceptor)
        stub.Subtract.Returns(10).Verifiable();
    }

    // =========================================================================
    // OnCall(callback) - Dynamic Return Based on Arguments
    // =========================================================================
    // DESIGN DECISION: OnCall(callback) receives typed arguments directly.
    // The callback signature matches the method parameters: (a, b) => result
    //
    // This differs from NSubstitute's callInfo.Arg<T>() pattern which requires
    // extracting arguments from an object array at runtime.
    //
    // DID NOT DO THIS: Use untyped argument access
    //
    // REJECTED PATTERN (NSubstitute-style):
    //   stub.Add.OnCall(callInfo => callInfo.Arg<int>(0) + callInfo.Arg<int>(1));
    //
    // WHY NOT: Source generators can provide typed access at compile time.
    // Typed callbacks are safer and provide IntelliSense support.
    // =========================================================================

    public void OnCall_ReceivesTypedArguments()
    {
        var stub = new Stubs.ICalculator();

        // Callback receives actual method arguments with correct types
        stub.Add.OnCall((a, b) => a + b);

        ICalculator calc = stub;
        var result = calc.Add(3, 5); // Returns 8 (3 + 5)
    }

    public void OnCall_CanThrowExceptions()
    {
        var stub = new Stubs.ICalculator();

        // Callbacks can throw exceptions for error testing
        stub.Divide.OnCall((a, b) =>
        {
            if (b == 0)
                throw new DivideByZeroException();
            return a / b;
        });

        ICalculator calc = stub;
        // calc.Divide(10, 0) would throw DivideByZeroException
    }

    // =========================================================================
    // COMMON MISTAKE: Returns() and OnCall() are Mutually Exclusive
    // =========================================================================
    //
    // COMMON MISTAKE: Expecting Returns() and OnCall() to combine
    //
    // WRONG:
    //   stub.Add.OnCall((a, b) => a + b);
    //   stub.Add.Returns(42);  // This REPLACES OnCall, does not combine
    //
    // Calling Returns() after OnCall() (or vice versa) replaces the previous
    // configuration. The last call wins.
    //
    // DESIGN DECISION: This mutual exclusivity is intentional:
    // - Returns() is for "I don't care about arguments, always return X"
    // - OnCall() is for "I need to compute return based on arguments"
    // These are fundamentally different use cases.
    // =========================================================================

    public void Returns_And_OnCall_AreExclusive()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.OnCall((a, b) => a + b);  // Set callback
        stub.Add.Returns(42);               // REPLACES callback with constant

        ICalculator calc = stub;
        var result = calc.Add(3, 5); // Returns 42, not 8
    }

    // =========================================================================
    // Void Methods
    // =========================================================================
    // DESIGN DECISION: Void methods have simpler interceptor APIs:
    // - OnCall(callback) for side effects
    // - Verify() for call count verification
    // - No Returns() since there's nothing to return
    //
    // GENERATOR BEHAVIOR: Void methods generate VoidMethodInterceptor:
    //
    //   public class ResetInterceptor : VoidMethodInterceptor<Unit>
    //   {
    //       public void OnCall(Action callback) { ... }
    //       public void Verify() { ... }
    //       public void Verify(Times times) { ... }
    //   }
    // =========================================================================

    public void VoidMethods_OnCallForSideEffects()
    {
        var stub = new Stubs.ICalculator();
        var resetCount = 0;

        // Void OnCall uses Action, not Func
        stub.Reset.OnCall(() => resetCount++);

        ICalculator calc = stub;
        calc.Reset();
        calc.Reset();

        // resetCount is now 2
        stub.Reset.Verify(Times.Exactly(2));
    }

    // =========================================================================
    // Argument Capture (LastArg / LastArgs)
    // =========================================================================
    // DESIGN DECISION: Every method interceptor tracks the last call's arguments.
    // - LastArg: For single-parameter methods (e.g., GetById(int id))
    // - LastArgs: For multi-parameter methods, returns a tuple
    //
    // GENERATOR BEHAVIOR: The interceptor tracks arguments:
    //
    //   public class AddInterceptor : MethodInterceptor<int, (int a, int b), int>
    //   {
    //       // For methods with multiple parameters
    //       public (int a, int b) LastArgs { get; private set; }
    //
    //       public int Call((int a, int b) args)
    //       {
    //           LastArgs = args;
    //           // ... invoke callback or return value
    //       }
    //   }
    //
    // For single-parameter methods:
    //   public int LastArg { get; private set; }  // Not LastArgs
    // =========================================================================

    public void ArgumentCapture_LastCallArgs_ForMultipleParameters()
    {
        var stub = new Stubs.ICalculator();
        stub.Add.Returns(0);

        ICalculator calc = stub;
        calc.Add(3, 5);
        calc.Add(10, 20);

        // LastCallArgs captures the most recent call's arguments
        // Note: The interceptor exposes LastCallArgs, which is nullable
        var args = stub.Add.LastCallArgs;
        // args == (10, 20)

        // For OnCall() return values, you get LastArgs via the builder interface:
        var builder = stub.Subtract.OnCall((a, b) => a - b);
        calc.Subtract(100, 25);
        var subtractArgs = builder.LastArgs;
        // subtractArgs == (100, 25)
    }

    // =========================================================================
    // Async Methods - Auto-Wrapping
    // =========================================================================
    // DESIGN DECISION: For async methods (Task<T>, ValueTask<T>), Returns()
    // automatically wraps the value with Task.FromResult or ValueTask.FromResult.
    //
    // This avoids boilerplate: Returns(Task.FromResult("value"))
    // Instead: Returns("value")
    //
    // DID NOT DO THIS: Require explicit Task.FromResult wrapping
    //
    // REJECTED PATTERN:
    //   stub.GetDataAsync.Returns(Task.FromResult("data"));  // Verbose
    //
    // ACTUAL PATTERN:
    //   stub.GetDataAsync.Returns("data");  // Auto-wrapped
    //
    // WHY NOT: Reducing boilerplate improves readability. The method signature
    // already indicates it's async - forcing explicit wrapping adds noise.
    //
    // GENERATOR BEHAVIOR: For Task<T> methods, Returns() wraps automatically:
    //
    //   public GetDataAsyncInterceptor Returns(string? value)
    //   {
    //       _returnValue = Task.FromResult(value);
    //       return this;
    //   }
    // =========================================================================

    public async Task AsyncMethods_Returns_AutoWraps()
    {
        var stub = new Stubs.IDataService();

        // No Task.FromResult needed - auto-wrapped
        stub.GetDataAsync.Returns("test data");

        IDataService service = stub;
        var result = await service.GetDataAsync(1);
        // result == "test data"
    }

    public async Task AsyncMethods_OnCall_ReturnsTaskDirectly()
    {
        var stub = new Stubs.IDataService();

        // OnCall for async methods: callback returns T, not Task<T>
        stub.GetDataAsync.OnCall((id) => $"Data for ID {id}");

        IDataService service = stub;
        var result = await service.GetDataAsync(42);
        // result == "Data for ID 42"
    }

    // =========================================================================
    // Void Async Methods (Task return)
    // =========================================================================
    // DESIGN DECISION: For async void methods (Task return, no value),
    // OnCall receives the arguments and Returns() is not available.
    //
    // GENERATOR BEHAVIOR: Task-returning void methods generate:
    //
    //   public class SaveDataAsyncInterceptor : VoidMethodInterceptor<string>
    //   {
    //       public void OnCall(Action<string> callback) { ... }
    //       // Returns Task.CompletedTask when not configured
    //   }
    // =========================================================================

    public async Task VoidAsyncMethods_OnCall()
    {
        var stub = new Stubs.IDataService();
        string? savedData = null;

        stub.SaveDataAsync.OnCall((data) => savedData = data);

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
    // - Returns/OnCall configuration is PRESERVED
    //
    // DID NOT DO THIS: Reset() clears configuration too
    //
    // REJECTED PATTERN:
    //   stub.Add.Reset(); // Would clear Returns(42) too
    //
    // WHY NOT: Separating tracking reset from configuration allows reusing
    // stubs across multiple test scenarios without reconfiguring.
    // =========================================================================

    public void Reset_ClearsTrackingButPreservesConfiguration()
    {
        var stub = new Stubs.ICalculator();
        stub.Add.Returns(42);

        ICalculator calc = stub;
        calc.Add(1, 2);
        // stub.Add.LastArgs == (1, 2)

        stub.Add.Reset();
        // stub.Add.LastArgs == (0, 0) (default)
        // But Returns(42) is still in effect

        var result = calc.Add(3, 4);
        // result == 42 (configuration preserved)
    }
}
