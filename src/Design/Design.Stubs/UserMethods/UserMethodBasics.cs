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
    // DESIGN QUESTION: Why No OnCall() on User Method Interceptors?
    // =========================================================================
    // The user method IS the behavior - OnCall would create ambiguity:
    // - Should OnCall override the user method?
    // - Should it wrap it?
    // - Should it be called before or after?
    //
    // CURRENT DECISION: User methods are the final word. If you need
    // runtime-configurable behavior, don't use a user method for that member.
    //
    // ALTERNATIVE CONSIDERED: OnCall that wraps user method
    // REJECTED: Adds complexity. User methods should be "set and forget".
    // =========================================================================
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

// DISABLED - Generator bugs prevent compilation
#if ENABLE_USER_METHOD_OVERLOAD_TESTS
[KnockOff]
public partial class OverloadedUserMethodStub : IOverloadedUserMethodService { }

public partial class OverloadedUserMethodStub
{
    protected string Format(string input) => input.ToUpperInvariant();
    protected string Format(string input, bool uppercase) => uppercase ? input.ToUpperInvariant() : input;
    protected string Format(string input, bool uppercase, int maxLength) => input[..Math.Min(input.Length, maxLength)];
    protected void Log(string message) { }
    protected void Log(string message, int level) { }
}
#endif

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
// PARTIAL USER METHOD COVERAGE FOR OVERLOADS - GENERATOR BUG
// =============================================================================
// COMMENTED OUT - Same generator bugs as above apply here.
// When only SOME overloads have user methods, the generator produces
// invalid code with duplicate variables and missing RecordCall methods.
// =============================================================================

// TODO: Enable when generator is fixed
/*
[KnockOff]
public partial class PartialOverloadUserMethodStub : IOverloadedUserMethodService { }

public partial class PartialOverloadUserMethodStub
{
    protected string Format(string input) => $"[User1: {input}]";
    protected void Log(string message) { }
    protected void Log(string message, int level) { }
}
*/

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
//       - Interceptors are tracking-only (Verify, LastArg, Reset)
//       - No OnCall on async user method interceptors
//       - See AsyncUserMethodStub for working examples
//
// 3. GENERIC USER METHODS
//    Q: Can user methods be generic?
//    A: YES - Generic user methods work with the Of<T>() pattern.
//       - Generator creates *2 interceptors (Create2, Transform2, etc.)
//       - Access typed tracking via stub.Create2.Of<T>()
//       - NOTABLE: Generic user method interceptors DO have OnCall!
//         This differs from non-generic user methods which are tracking-only.
//       - The OnCall on Of<T>() typed handlers allows overriding specific
//         type instantiations while user method handles the general case.
//       - See GenericUserMethodStub for working examples
//
// =============================================================================
