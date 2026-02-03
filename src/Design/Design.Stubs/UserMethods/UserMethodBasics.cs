// -----------------------------------------------------------------------------
// Design.Stubs - User-Defined Methods
// -----------------------------------------------------------------------------
// This file explores and documents user-defined method patterns:
// - Basic user methods (protected methods matching interface signatures)
// - Tracking API on *2 interceptors (Verify, LastArg, Reset)
// - User method overloads
// - Mixed scenarios (some with user methods, some without)
// - Priority: user methods vs OnCall
// - Strict mode behavior
// -----------------------------------------------------------------------------

using System.Text;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.UserMethods;

// =============================================================================
// USER METHODS - OVERVIEW
// =============================================================================
//
// WHAT ARE USER METHODS?
// User methods are protected methods you define in a partial stub class that
// match an interface method signature. KnockOff calls your method instead of
// using smart defaults or configured callbacks.
//
// WHY USE THEM?
// 1. Reusable defaults - Define once, use across all tests
// 2. Complex logic - When callback lambdas get unwieldy
// 3. State management - When you need instance state
// 4. IDE support - Full IntelliSense, refactoring, debugging
//
// THE *2 SUFFIX:
// When you add a user method, KnockOff generates a tracking interceptor with
// a "2" suffix (e.g., Process2) to avoid name collision with your method.
// This interceptor is TRACKING-ONLY - no OnCall() available.
//
// =============================================================================

// =============================================================================
// BASIC USER METHODS
// =============================================================================

[KnockOff]
public partial class BasicUserMethodStub : IUserMethodService { }

public partial class BasicUserMethodStub
{
    // =========================================================================
    // User Method Definition
    // =========================================================================
    // PATTERN: Define a protected method with EXACT signature match:
    // - Same name
    // - Same return type
    // - Same parameter types (in same order)
    //
    // The method body is your implementation - called on every interface call.
    // =========================================================================

    protected string Process(string input)
    {
        // Your implementation - full C# flexibility
        return $"[Processed: {input}]";
    }

    protected int Calculate(int a, int b)
    {
        // Can include any logic you need
        return a + b;
    }

    protected void Execute(string command)
    {
        // Void methods work too
        // Could update instance state here
    }

    protected string? FindById(int id)
    {
        // Return null for "not found" scenarios
        return id > 0 ? $"Entity-{id}" : null;
    }
}

[KnockOff<IUserMethodService>]
public partial class UserMethodBasicsDemo
{
    // =========================================================================
    // Using Stubs with User Methods
    // =========================================================================

    public void BasicUsage()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Calls your protected Process method
        var result = service.Process("hello");
        // result == "[Processed: hello]"

        // Calls your protected Calculate method
        var sum = service.Calculate(3, 4);
        // sum == 7
    }

    // =========================================================================
    // Tracking API - The *2 Interceptor
    // =========================================================================
    // DESIGN DECISION: User method interceptors have a "2" suffix and are
    // TRACKING-ONLY. They provide:
    // - Verify(Times) - verify call count
    // - LastArg - last single argument
    // - LastArgs - last arguments as tuple
    // - Reset() - clear tracking state
    //
    // They do NOT have OnCall() - the user method IS the behavior.
    // =========================================================================

    public void TrackingWithVerify()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Process("first");
        service.Process("second");
        service.Process("third");

        // Verify with the *2 interceptor
        stub.Process2.Verify(Times.Exactly(3));
        stub.Process2.Verify(Times.AtLeast(2));
    }

    public void TrackingWithLastArg()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.FindById(42);
        service.FindById(99);

        // LastArg gives the most recent argument
        var lastId = stub.FindById2.LastArg;
        // lastId == 99
    }

    public void TrackingWithLastArgs()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Calculate(10, 20);
        service.Calculate(30, 40);

        // LastArgs gives tuple of most recent arguments
        var (a, b) = stub.Calculate2.LastArgs;
        // a == 30, b == 40
    }

    public void TrackingWithReset()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Process("test");
        stub.Process2.Verify(Times.Once);

        // Reset clears all tracking state
        stub.Process2.Reset();

        stub.Process2.Verify(Times.Never);
        // LastArg is now default (null for reference types)
    }

    // =========================================================================
    // OnCall() Supersedes User Methods
    // =========================================================================
    // User methods provide shareable defaults. OnCall/Returns override per test.
    //
    // RATIONALE: Stubs should be shareable yet configurable per test.
    // - User method = sensible default for most tests
    // - OnCall/Returns = override when a specific test needs different behavior
    //
    // GENERATED CODE PATTERN:
    //   if (Interceptor.Callback is { } callback) return callback(args);  // OnCall wins
    //   return UserMethod(args);                                          // Fallback
    //
    // Reset() behavior: Clears tracking state but PRESERVES OnCall configuration.
    // This matches regular interceptor semantics.
    // =========================================================================

    public void OnCall_SupersedesUserMethod()
    {
        var stub = new BasicUserMethodStub();

        // By default, user method is called
        IUserMethodService service = stub;
        var defaultResult = service.Process("hello");
        // defaultResult == "[Processed: hello]" (from user method)

        // OnCall supersedes the user method for per-test override
        stub.Process2.OnCall(input => $"[Override: {input}]");
        var overrideResult = service.Process("hello");
        // overrideResult == "[Override: hello]" (OnCall wins)

        stub.Process2.Verify(Times.Exactly(2)); // Both calls tracked
    }

    public void Returns_SupersedesUserMethod()
    {
        var stub = new BasicUserMethodStub();
        stub.Process2.Returns("constant"); // Returns is shorthand for OnCall(_ => value)

        IUserMethodService service = stub;
        var result = service.Process("ignored");
        // result == "constant" (Returns wins, ignores argument)
    }

    public void VoidMethod_OnCallSupersedes()
    {
        var stub = new BasicUserMethodStub();
        var callbackInvoked = false;
        stub.Execute2.OnCall(cmd => callbackInvoked = true);

        IUserMethodService service = stub;
        service.Execute("test");

        // callbackInvoked == true (OnCall was invoked, not user method)
        stub.Execute2.Verify(Times.Once);
    }

    public void Reset_PreservesOnCallConfiguration()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate2.OnCall((a, b) => a * b); // Override addition with multiplication

        IUserMethodService service = stub;
        service.Calculate(3, 4);
        stub.Calculate2.Verify(Times.Once);

        // Reset clears tracking but preserves OnCall
        stub.Calculate2.Reset();
        stub.Calculate2.Verify(Times.Never);

        var result = service.Calculate(5, 6);
        // result == 30 (OnCall still active: 5 * 6)
    }
}

// =============================================================================
// USER METHOD OVERLOADS - GENERATOR BUG DISCOVERED
// =============================================================================
// **BUG ANALYSIS:**
//
// When an interface has overloaded methods AND user methods for all overloads,
// the generator creates a single *2 interceptor based on the FIRST overload's
// signature. The interceptor only has one RecordCall signature, but the
// interface implementations try to call it with different argument shapes.
//
// **GENERATED CODE (buggy):**
//
//   class Format2Interceptor {
//       void RecordCall(string input) { ... }  // Only first overload's signature
//   }
//
//   string IOverloadedUserMethodService.Format(string input) {
//       Format2.RecordCall(input);  // OK
//       return Format(input);
//   }
//
//   string IOverloadedUserMethodService.Format(string input, bool uppercase) {
//       Format2.RecordCall((input, uppercase));  // ERROR: No matching overload!
//       return Format(input, uppercase);
//   }
//
// **ROOT CAUSE:**
// The generator treats user method overloads like non-user-method overloads
// (single interceptor, but with LastArg type based on first overload).
// For regular methods, OnCall() targets specific overloads. For user methods,
// there's no way to specify which overload's args to track.
//
// **POSSIBLE FIXES:**
// 1. Generate RecordCall overloads matching each interface overload
// 2. Use object/tuple for args and lose type safety
// 3. Generate separate interceptors per overload (Format2_1, Format2_2, Format2_3)
// 4. Track only call count for overloaded user methods (lose LastArgs)
//
// TODO: Fix generator, then enable tests below.
// =============================================================================

// Generator fix: Per-signature RecordCall methods now generated for user method overloads
[KnockOff]
public partial class OverloadedUserMethodStub : IOverloadedUserMethodService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class OverloadedUserMethodStub
{
    protected string Format(string input) => input.ToUpperInvariant();
    protected string Format(string input, bool uppercase) => uppercase ? input.ToUpperInvariant() : input;
    protected string Format(string input, bool uppercase, int maxLength) => input[..Math.Min(input.Length, maxLength)];
    protected void Log(string message) { }
    protected void Log(string message, int level) { }
}
#pragma warning restore CA1062

// =============================================================================
// MIXED SCENARIOS - Some Methods with User Impl, Some Without
// =============================================================================

[KnockOff]
public partial class MixedUserMethodStub : IMixedUserMethodService { }

public partial class MixedUserMethodStub
{
    // Only implement SOME interface methods as user methods
    // Others will use the regular interceptor API

    protected string WithUserMethod(string input)
    {
        return $"[User: {input}]";
    }

    protected int ComputeWithUserMethod(int value)
    {
        return value * 2;
    }

    // WithoutUserMethod and ComputeWithoutUserMethod are NOT defined here
    // They use the regular interceptor (OnCall, Returns)
}

[KnockOff<IMixedUserMethodService>]
public partial class MixedUserMethodDemo
{
    public void MixedScenario()
    {
        var stub = new MixedUserMethodStub();

        // Methods WITH user methods use the *2 interceptor for tracking
        stub.WithUserMethod2.Verify(Times.Never);  // Tracking-only

        // Methods WITHOUT user methods use the regular interceptor
        stub.WithoutUserMethod.OnCall((input) => $"[Configured: {input}]");
        stub.WithoutUserMethod.Verify(Times.Never);  // Full interceptor API

        stub.ComputeWithoutUserMethod.Returns(42);

        IMixedUserMethodService service = stub;

        var r1 = service.WithUserMethod("test");       // "[User: test]"
        var r2 = service.WithoutUserMethod("test");    // "[Configured: test]"
        var r3 = service.ComputeWithUserMethod(5);     // 10
        var r4 = service.ComputeWithoutUserMethod(5);  // 42

        stub.WithUserMethod2.Verify(Times.Once);
        stub.WithoutUserMethod.Verify(Times.Once);
    }

    // =========================================================================
    // DESIGN OBSERVATION: Naming Convention
    // =========================================================================
    // - User method present: stub.Method2 (tracking-only)
    // - No user method: stub.Method (full interceptor API)
    //
    // The "2" suffix ONLY appears when a user method is defined.
    // This is determined at compile time by the generator.
    // =========================================================================
}

// =============================================================================
// PARTIAL USER METHOD COVERAGE FOR OVERLOADS
// =============================================================================
// When only SOME overloads have user methods:
// - User-method overloads use the *2 interceptor with tracking-only behavior
// - Non-user-method overloads use the regular interceptor with OnCall API
//
// Example: Format has 3 overloads, but only Format(string) has a user method.
// - stub.Format2 tracks calls to Format(string) via user method
// - stub.Format tracks calls to Format(string,bool) and Format(string,bool,int) via OnCall
// =============================================================================

[KnockOff]
public partial class PartialOverloadUserMethodStub : IOverloadedUserMethodService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class PartialOverloadUserMethodStub
{
    // Only Format(string) has a user method - the other two Format overloads do NOT
    protected string Format(string input) => $"[User1: {input}]";

    // All Log overloads have user methods
    protected void Log(string message) { }
    protected void Log(string message, int level) { }
}
#pragma warning restore CA1062

// =============================================================================
// STRICT MODE AND USER METHODS
// =============================================================================

[KnockOff(Strict = true)]
public partial class StrictUserMethodStub : IUserMethodService { }

public partial class StrictUserMethodStub
{
    // User methods bypass strict mode - they ARE the configuration
    protected string Process(string input) => $"[Strict: {input}]";

    // Only Process has user method
    // Other methods will throw in strict mode if called without configuration
}

[KnockOff<IUserMethodService>]
public partial class StrictModeUserMethodDemo
{
    public void StrictMode_UserMethodsBypassed()
    {
        var stub = new StrictUserMethodStub();
        IUserMethodService service = stub;

        // User method works in strict mode - it IS the configuration
        var result = service.Process("test");  // "[Strict: test]"

        // Non-user-method in strict mode would throw without configuration
        // service.Calculate(1, 2);  // Would throw - no user method, no OnCall
    }

    // =========================================================================
    // DESIGN DECISION: User Methods Bypass Strict Mode
    // =========================================================================
    // Strict mode means "throw if unconfigured". User methods ARE configured
    // by their very existence. This is consistent - the user method IS the
    // behavior, just defined in a different way than OnCall.
    // =========================================================================
}

// =============================================================================
// ASYNC USER METHODS
// =============================================================================

[KnockOff]
public partial class AsyncUserMethodStub : IAsyncUserMethodService { }

public partial class AsyncUserMethodStub
{
    // =========================================================================
    // Async User Methods - Task<T>
    // =========================================================================
    // QUESTION: Can user methods be async and return Task<T>?
    // Let's find out...
    // =========================================================================

    protected async Task<string> ProcessAsync(string input)
    {
        // Simulate async work
        await Task.Delay(1);
        return $"[Async: {input}]";
    }

    protected async Task ExecuteAsync(string command)
    {
        // Async void method
        await Task.Delay(1);
        // Side effect here
    }

    protected async ValueTask<int> ComputeAsync(int value)
    {
        // ValueTask version
        await Task.Yield();
        return value * 2;
    }
}

[KnockOff<IAsyncUserMethodService>]
public partial class AsyncUserMethodDemo
{
    public async Task AsyncUserMethod_BasicUsage()
    {
        var stub = new AsyncUserMethodStub();
        IAsyncUserMethodService service = stub;

        // Call async user method
        var result = await service.ProcessAsync("hello");
        // result == "[Async: hello]"

        // Verify with *2 interceptor
        stub.ProcessAsync2.Verify(Times.Once);
    }

    public async Task AsyncUserMethod_VoidTask()
    {
        var stub = new AsyncUserMethodStub();
        IAsyncUserMethodService service = stub;

        await service.ExecuteAsync("command");

        stub.ExecuteAsync2.Verify(Times.Once);
        // LastArg should capture the command
        var lastCommand = stub.ExecuteAsync2.LastArg;
    }

    public async Task AsyncUserMethod_ValueTask()
    {
        var stub = new AsyncUserMethodStub();
        IAsyncUserMethodService service = stub;

        var result = await service.ComputeAsync(21);
        // result == 42

        stub.ComputeAsync2.Verify(Times.Once);
    }
}

// =============================================================================
// GENERIC USER METHODS
// =============================================================================

[KnockOff]
public partial class GenericUserMethodStub : IGenericUserMethodService { }

public partial class GenericUserMethodStub
{
    // =========================================================================
    // Generic User Methods
    // =========================================================================
    // QUESTION: Can user methods be generic?
    // - How does the generator handle type parameters?
    // - How does tracking work with generic returns?
    // =========================================================================

    protected T Create<T>() where T : new()
    {
        // Generic user method with constraint
        return new T();
    }

    protected TOut Transform<TIn, TOut>(TIn input) where TOut : new()
    {
        // Multi-type-parameter generic method
        return new TOut();
    }

    protected T Clone<T>(T source) where T : ICloneable
    {
        // Generic with interface constraint
        return (T)source.Clone();
    }
}

[KnockOff<IGenericUserMethodService>]
public partial class GenericUserMethodDemo
{
    public void GenericUserMethod_Create()
    {
        var stub = new GenericUserMethodStub();
        IGenericUserMethodService service = stub;

        // Call generic user method
        var list = service.Create<List<int>>();

        // How do we verify? What's the interceptor name?
        // stub.Create2.Verify()?  Or stub.Create2.Of<List<int>>().Verify()?
    }

    public void GenericUserMethod_Transform()
    {
        var stub = new GenericUserMethodStub();
        IGenericUserMethodService service = stub;

        var result = service.Transform<string, StringBuilder>("hello");

        // Multi-type-parameter tracking?
    }
}

// =============================================================================
// OVERLOADED GENERIC USER METHODS
// =============================================================================
// This tests the case where an interface has overloaded generic methods and
// the user provides user methods for all overloads. The generator must produce
// per-signature RecordCall methods within the typed handler.
// =============================================================================

// Generator fix: Per-signature RecordCall methods now generated for overloaded generic user methods
[KnockOff]
public partial class OverloadedGenericUserMethodStub : IOverloadedGenericUserMethodService { }

public partial class OverloadedGenericUserMethodStub
{
    // Single-parameter generic overload
    protected T Process<T>(T input) => input;

    // Two-parameter generic overload
    protected T Process<T>(T input, string options) => input;

    // Multi-type-parameter overload
    protected TOut Process<TIn, TOut>(TIn input) where TOut : new() => new TOut();
}

[KnockOff<IOverloadedGenericUserMethodService>]
public partial class OverloadedGenericUserMethodDemo
{
    public void OverloadedGenericUserMethod_Usage()
    {
        var stub = new OverloadedGenericUserMethodStub();
        IOverloadedGenericUserMethodService service = stub;

        // Call single-parameter overload
        var r1 = service.Process("hello");

        // Call two-parameter overload
        var r2 = service.Process("hello", "uppercase");

        // Call multi-type-parameter overload
        var r3 = service.Process<string, StringBuilder>("hello");

        // All calls should be tracked via Process2.Of<T>()
        // stub.Process2.Of<string>().Verify(Times.Exactly(2));  // r1 and r2
        // stub.Process2.Of<string, StringBuilder>().Verify(Times.Once);  // r3

        // Per-signature tracking within Of<T>():
        // stub.Process2.Of<string>().LastArg_T == "hello" (from r1)
        // stub.Process2.Of<string>().LastArgs_T_String == ("hello", "uppercase") (from r2)
    }
}

// =============================================================================
// DESIGN FINDINGS AND QUESTIONS
// =============================================================================
//
// **CONFIRMED WORKING:**
// - Basic user methods (single overload, any parameter count)
// - *2 interceptor with Verify(), LastArg, LastArgs, Reset(), Verifiable()
// - Mixed stubs (some methods with user impl, some without)
// - Strict mode bypass for user methods
// - Async user methods (Task<T>, Task, ValueTask<T>) - see AsyncUserMethodStub
// - Generic user methods with Of<T>() pattern - see GenericUserMethodStub
// - OnCall/Returns on non-generic user method interceptors (supersedes user method)
//
// **BUGS DISCOVERED:**
// 1. USER METHOD OVERLOADS - Generator produces invalid code
//    - RecordCall has only one signature (first overload's)
//    - Other overloads pass wrong argument types
//    - See detailed analysis above
//    - Tracked in: docs/todos/user-method-overload-bug.md
//
// 2. PARTIAL OVERLOAD COVERAGE - Same bug as #1
//    - If you implement only some overloads, same RecordCall issue
//
// **ANSWERED QUESTIONS:**
//
// 1. INLINE PATTERN SUPPORT
//    Q: Can inline stubs ([KnockOff<T>]) have user methods?
//    A: NO - Inline stubs generate the entire class. User methods require
//       a partial class where you can add protected methods. These are
//       fundamentally incompatible patterns.
//
// 2. ASYNC USER METHODS
//    Q: Do async user methods work as expected?
//    A: YES - Task<T>, Task, and ValueTask<T> all work correctly.
//       - Generator creates *2 interceptors (ProcessAsync2, ExecuteAsync2, etc.)
//       - Interceptors have OnCall/Returns with auto-wrap (Task.FromResult/ValueTask)
//       - Plain Task returns (void-like) have OnCall but no Returns
//       - See AsyncUserMethodStub for working examples
//
// 3. GENERIC USER METHODS
//    Q: Can user methods be generic?
//    A: YES - Generic user methods work with the Of<T>() pattern.
//       - Generator creates *2 interceptors (Create2, Transform2, etc.)
//       - Access typed tracking via stub.Create2.Of<T>()
//       - OnCall on Of<T>() typed handlers allows overriding specific
//         type instantiations while user method handles the general case.
//       - See GenericUserMethodStub for working examples
//       - Non-generic user methods will also get OnCall (see planned enhancement)
//
// =============================================================================
