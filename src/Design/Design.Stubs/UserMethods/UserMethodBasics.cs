// -----------------------------------------------------------------------------
// Design.Stubs - User-Defined Methods
// -----------------------------------------------------------------------------
// This file explores and documents user-defined method patterns:
// - Base class pattern (protected override methods with _ suffix)
// - Tracking API on interceptors (Verify, LastArg, Reset)
// - OnCall/Returns to supersede user methods
// - User method overloads
// - Mixed scenarios (some with user methods, some without)
// - Priority: OnCall > user method
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
// User methods are protected override methods you define in a partial stub class
// to provide default behavior for interface methods. KnockOff generates a base
// class with virtual methods that you can override.
//
// WHY USE THEM?
// 1. Reusable defaults - Define once, use across all tests
// 2. Complex logic - When callback lambdas get unwieldy
// 3. State management - When you need instance state
// 4. IDE support - Full IntelliSense, refactoring, debugging
// 5. Compile-time safety - Signature errors caught by compiler
//
// THE BASE CLASS PATTERN:
// KnockOff generates a base class (e.g., BasicUserMethodStubBase) with virtual
// protected methods suffixed with underscore (e.g., Process_). You override
// these methods to provide default behavior:
//
//   // Generated base class (BasicUserMethodStubBase):
//   protected virtual string Process_(string input) => default!;
//
//   // Your override:
//   protected override string Process_(string input) => $"[Processed: {input}]";
//
// INTERCEPTOR NAMING:
// Tracker properties use clean names (stub.Process, not stub.Process2).
// The underscore suffix is only on the overridable method (Process_).
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
    // User Method Definition - Base Class Pattern
    // =========================================================================
    // PATTERN: Override the generated virtual method with underscore suffix:
    // - Add 'override' keyword
    // - Add '_' suffix to method name (e.g., Process_)
    // - Same return type and parameter types
    //
    // COMPILER ENFORCES SIGNATURE:
    // If you typo the method name or get parameters wrong, the compiler
    // catches it (no suitable method to override). No more silent failures!
    //
    // GENERATED BASE CLASS (BasicUserMethodStubBase):
    // protected virtual string Process_(string input) => default!;
    // protected virtual int Calculate_(int a, int b) => default!;
    // protected virtual void Execute_(string command) { }
    // protected virtual string? FindById_(int id) => default!;
    // =========================================================================

    protected override string Process_(string input)
    {
        // Your implementation - full C# flexibility
        return $"[Processed: {input}]";
    }

    protected override int Calculate_(int a, int b)
    {
        // Can include any logic you need
        return a + b;
    }

    protected override void Execute_(string command)
    {
        // Void methods work too
        // Could update instance state here
    }

    protected override string? FindById_(int id)
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

        // Calls your protected override Process_ method
        var result = service.Process("hello");
        // result == "[Processed: hello]"

        // Calls your protected override Calculate_ method
        var sum = service.Calculate(3, 4);
        // sum == 7
    }

    // =========================================================================
    // Tracking API - Clean Interceptor Names
    // =========================================================================
    // DESIGN DECISION: Interceptor properties use CLEAN names (no suffix).
    // With the base class pattern:
    // - Override method: Process_ (underscore suffix)
    // - Interceptor: stub.Process (clean name)
    //
    // The tracking interceptor provides:
    // - Verify(Times) - verify call count
    // - LastArg - last single argument
    // - LastArgs - last arguments as tuple
    // - Reset() - clear tracking state
    // - OnCall() - supersede user method per-test
    // - Returns() - constant value that supersedes user method
    // =========================================================================

    public void TrackingWithVerify()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Process("first");
        service.Process("second");
        service.Process("third");

        // Verify with clean interceptor name
        stub.Process.Verify(Times.Exactly(3));
        stub.Process.Verify(Times.AtLeast(2));
    }

    public void TrackingWithLastArg()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.FindById(42);
        service.FindById(99);

        // LastArg gives the most recent argument
        var lastId = stub.FindById.LastArg;
        // lastId == 99
    }

    public void TrackingWithLastArgs()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Calculate(10, 20);
        service.Calculate(30, 40);

        // LastArgs gives tuple of most recent arguments (nullable wrapper)
        var (a, b) = stub.Calculate.LastArgs!.Value;
        // a == 30, b == 40
    }

    public void TrackingWithReset()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        service.Process("test");
        stub.Process.Verify(Times.Once);

        // Reset clears all tracking state
        stub.Process.Reset();

        stub.Process.Verify(Times.Never);
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
    //   return Process_(args);                                            // User override
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
        // defaultResult == "[Processed: hello]" (from Process_ override)

        // OnCall supersedes the user method for per-test override
        stub.Process.OnCall(input => $"[Override: {input}]");
        var overrideResult = service.Process("hello");
        // overrideResult == "[Override: hello]" (OnCall wins)

        stub.Process.Verify(Times.Exactly(2)); // Both calls tracked
    }

    public void Returns_SupersedesUserMethod()
    {
        var stub = new BasicUserMethodStub();
        stub.Process.Returns("constant"); // Returns is shorthand for OnCall(_ => value)

        IUserMethodService service = stub;
        var result = service.Process("ignored");
        // result == "constant" (Returns wins, ignores argument)
    }

    public void VoidMethod_OnCallSupersedes()
    {
        var stub = new BasicUserMethodStub();
        var callbackInvoked = false;
        stub.Execute.OnCall(cmd => callbackInvoked = true);

        IUserMethodService service = stub;
        service.Execute("test");

        // callbackInvoked == true (OnCall was invoked, not user method)
        stub.Execute.Verify(Times.Once);
    }

    public void Reset_PreservesOnCallConfiguration()
    {
        var stub = new BasicUserMethodStub();
        stub.Calculate.OnCall((a, b) => a * b); // Override addition with multiplication

        IUserMethodService service = stub;
        service.Calculate(3, 4);
        stub.Calculate.Verify(Times.Once);

        // Reset clears tracking but preserves OnCall
        stub.Calculate.Reset();
        stub.Calculate.Verify(Times.Never);

        var result = service.Calculate(5, 6);
        // result == 30 (OnCall still active: 5 * 6)
    }
}

// =============================================================================
// USER METHOD OVERLOADS
// =============================================================================
// User methods work naturally with overloads. Each overload gets its own
// virtual method in the base class (with underscore suffix). You can override
// any subset of overloads - unoverridden ones use the interceptor path.
//
// BASE CLASS GENERATES:
//   protected virtual string Format_(string input) => default!;
//   protected virtual string Format_(string input, bool uppercase) => default!;
//   protected virtual string Format_(string input, bool uppercase, int maxLength) => default!;
//
// YOUR OVERRIDES:
//   protected override string Format_(string input) => input.ToUpperInvariant();
//   protected override string Format_(string input, bool uppercase) => ...
//   protected override string Format_(string input, bool uppercase, int maxLength) => ...
//
// TRACKING:
// All overloads share the same interceptor (stub.Format) with per-signature
// RecordCall methods for correct argument tracking.
// =============================================================================

// Generator produces per-signature RecordCall methods for user method overloads
[KnockOff]
public partial class OverloadedUserMethodStub : IOverloadedUserMethodService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class OverloadedUserMethodStub
{
    protected override string Format_(string input) => input.ToUpperInvariant();
    protected override string Format_(string input, bool uppercase) => uppercase ? input.ToUpperInvariant() : input;
    protected override string Format_(string input, bool uppercase, int maxLength) => input[..Math.Min(input.Length, maxLength)];
    protected override void Log_(string message) { }
    protected override void Log_(string message, int level) { }
}
#pragma warning restore CA1062

// =============================================================================
// MIXED SCENARIOS - Some Methods with User Impl, Some Without
// =============================================================================

[KnockOff]
public partial class MixedUserMethodStub : IMixedUserMethodService { }

public partial class MixedUserMethodStub
{
    // Only override SOME interface methods as user methods
    // Others will use the regular interceptor API (no override needed)

    protected override string WithUserMethod_(string input)
    {
        return $"[User: {input}]";
    }

    protected override int ComputeWithUserMethod_(int value)
    {
        return value * 2;
    }

    // WithoutUserMethod_ and ComputeWithoutUserMethod_ are NOT overridden
    // They use the regular interceptor path (OnCall, Returns, or default)
}

[KnockOff<IMixedUserMethodService>]
public partial class MixedUserMethodDemo
{
    public void MixedScenario()
    {
        var stub = new MixedUserMethodStub();

        // Methods WITH user override use the interceptor for tracking + OnCall
        stub.WithUserMethod.Verify(Times.Never);

        // Methods WITHOUT user override also use interceptor (same API)
        stub.WithoutUserMethod.OnCall((input) => $"[Configured: {input}]");
        stub.WithoutUserMethod.Verify(Times.Never);

        stub.ComputeWithoutUserMethod.Returns(42);

        IMixedUserMethodService service = stub;

        var r1 = service.WithUserMethod("test");       // "[User: test]" (from override)
        var r2 = service.WithoutUserMethod("test");    // "[Configured: test]" (from OnCall)
        var r3 = service.ComputeWithUserMethod(5);     // 10 (from override)
        var r4 = service.ComputeWithoutUserMethod(5);  // 42 (from Returns)

        stub.WithUserMethod.Verify(Times.Once);
        stub.WithoutUserMethod.Verify(Times.Once);
    }

    // =========================================================================
    // DESIGN OBSERVATION: Consistent Naming
    // =========================================================================
    // With the base class pattern, interceptor names are ALWAYS clean:
    // - User method present: stub.Method (with override behavior)
    // - No user method: stub.Method (default/interceptor behavior)
    //
    // The presence or absence of a user override is detected at compile time.
    // When override exists: OnCall > User override
    // When no override: OnCall > Strict/Default
    // =========================================================================
}

// =============================================================================
// PARTIAL USER METHOD COVERAGE FOR OVERLOADS
// =============================================================================
// You can override only SOME overloads. Each overload is handled independently:
// - Overridden: OnCall > User override
// - Not overridden: OnCall > Strict/Default
//
// Example: Format has 3 overloads, but only Format(string) is overridden.
// - Format(string) calls Format_(string) (your override)
// - Format(string,bool) uses interceptor path (no override)
// - Format(string,bool,int) uses interceptor path (no override)
//
// All overloads share the same interceptor (stub.Format) for tracking.
// =============================================================================

[KnockOff]
public partial class PartialOverloadUserMethodStub : IOverloadedUserMethodService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class PartialOverloadUserMethodStub
{
    // Only Format(string) is overridden - the other two Format overloads are NOT
    protected override string Format_(string input) => $"[User1: {input}]";

    // All Log overloads have user overrides
    protected override void Log_(string message) { }
    protected override void Log_(string message, int level) { }
}
#pragma warning restore CA1062

// =============================================================================
// STRICT MODE AND USER METHODS
// =============================================================================

[KnockOff(Strict = true)]
public partial class StrictUserMethodStub : IUserMethodService { }

public partial class StrictUserMethodStub
{
    // User overrides bypass strict mode - they ARE the configuration
    protected override string Process_(string input) => $"[Strict: {input}]";

    // Only Process_ is overridden
    // Other methods will throw in strict mode if called without OnCall configuration
}

[KnockOff<IUserMethodService>]
public partial class StrictModeUserMethodDemo
{
    public void StrictMode_UserMethodsBypassed()
    {
        var stub = new StrictUserMethodStub();
        IUserMethodService service = stub;

        // User override works in strict mode - it IS the configuration
        var result = service.Process("test");  // "[Strict: test]"

        // Non-overridden method in strict mode would throw without OnCall
        // service.Calculate(1, 2);  // Would throw - no override, no OnCall
    }

    // =========================================================================
    // DESIGN DECISION: User Overrides Bypass Strict Mode
    // =========================================================================
    // Strict mode means "throw if unconfigured". User overrides ARE configured
    // by their very existence. This is consistent - the override IS the
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
    // Async User Methods - Task<T>, Task, ValueTask<T>
    // =========================================================================
    // Async user methods work exactly like sync methods - override the virtual
    // method with underscore suffix. The base class generates:
    //   protected virtual Task<string> ProcessAsync_(string input) => default!;
    //   protected virtual Task ExecuteAsync_(string command) => Task.CompletedTask;
    //   protected virtual ValueTask<int> ComputeAsync_(int value) => default!;
    // =========================================================================

    protected override async Task<string> ProcessAsync_(string input)
    {
        // Simulate async work
        await Task.Delay(1);
        return $"[Async: {input}]";
    }

    protected override async Task ExecuteAsync_(string command)
    {
        // Async void method
        await Task.Delay(1);
        // Side effect here
    }

    protected override async ValueTask<int> ComputeAsync_(int value)
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

        // Verify with clean interceptor name
        stub.ProcessAsync.Verify(Times.Once);
    }

    public async Task AsyncUserMethod_VoidTask()
    {
        var stub = new AsyncUserMethodStub();
        IAsyncUserMethodService service = stub;

        await service.ExecuteAsync("command");

        stub.ExecuteAsync.Verify(Times.Once);
        // LastArg captures the command
        var lastCommand = stub.ExecuteAsync.LastArg;
    }

    public async Task AsyncUserMethod_ValueTask()
    {
        var stub = new AsyncUserMethodStub();
        IAsyncUserMethodService service = stub;

        var result = await service.ComputeAsync(21);
        // result == 42

        stub.ComputeAsync.Verify(Times.Once);
    }
}

// =============================================================================
// GENERIC USER METHODS - EXCLUDED FROM BASE CLASS PATTERN
// =============================================================================

[KnockOff]
public partial class GenericUserMethodStub : IGenericUserMethodService { }

public partial class GenericUserMethodStub
{
    // =========================================================================
    // Generic User Methods - NOT SUPPORTED in Base Class Pattern
    // =========================================================================
    // DESIGN DECISION: Generic methods are EXCLUDED from the base class pattern.
    //
    // WHY:
    // 1. User override is ONE method for ALL type arguments
    // 2. The Of<T>() pattern allows DIFFERENT callbacks per type argument
    // 3. These are fundamentally different approaches
    //
    // For generic methods, use the standard interceptor API:
    //   stub.Create.Of<List<int>>().Returns(new List<int>());
    //   stub.Create.Of<User>().OnCall(() => new User { Id = 1 });
    //
    // The Of<T>() pattern handles both behavior AND verification consistently.
    // =========================================================================

    // These are NOT user methods - generic methods don't get base class virtuals
    // Use stub.Create.Of<T>() instead

    // If you need generic "default" behavior, you can still use:
    //   stub.Create.Of<List<int>>().Returns(new List<int>());
    //   stub.Transform.Of<string, StringBuilder>().Returns(new StringBuilder());
}

[KnockOff<IGenericUserMethodService>]
public partial class GenericUserMethodDemo
{
    public void GenericMethod_UseOfTPattern()
    {
        var stub = new GenericUserMethodStub();
        IGenericUserMethodService service = stub;

        // Configure via Of<T>().OnCall() - this is the ONLY way for generic methods
        stub.Create.Of<List<int>>().OnCall(() => new List<int> { 1, 2, 3 });

        var list = service.Create<List<int>>();
        // list == { 1, 2, 3 }

        // Verify with Of<T>()
        stub.Create.Of<List<int>>().Verify(Times.Once);
    }

    public void GenericMethod_MultiTypeParam()
    {
        var stub = new GenericUserMethodStub();
        IGenericUserMethodService service = stub;

        // Multi-type-parameter methods also use Of<T>().OnCall()
        stub.Transform.Of<string, StringBuilder>().OnCall(input => new StringBuilder($"transformed: {input}"));

        var result = service.Transform<string, StringBuilder>("hello");
        // result.ToString() == "transformed: hello"

        stub.Transform.Of<string, StringBuilder>().Verify(Times.Once);
    }
}

// =============================================================================
// OVERLOADED GENERIC USER METHODS - EXCLUDED FROM BASE CLASS PATTERN
// =============================================================================
// Generic methods (including overloaded ones) are excluded from the base class
// pattern. Use the standard Of<T>() interceptor API for all generic methods.
//
// NOTE: Overloaded generic methods have complex code generation requirements.
// See IGenericUserMethodService (single overload) for working examples.
// =============================================================================

// OverloadedGenericUserMethodStub removed - generator has a known issue with
// overloaded generic methods. See IGenericUserMethodService for working examples.

// =============================================================================
// WHEN CHAIN SUPPORT WITH USER METHODS
// =============================================================================
//
// User method stubs now support the full .When() API. When chains have highest
// priority, allowing per-test customization while preserving user method fallback.
//
// PRIORITY CHAIN:
// 1. When chains (parameter-specific matching)
// 2. Sequences (ThenCall chain)
// 3. OnCall/Returns (explicit configuration)
// 4. User Method (fallback - always called if nothing else matches)
//
// This is the same priority order as inline stubs, except user method replaces
// Source/Strict as the final fallback.

public static class WhenChainUserMethodDemo
{
    // =========================================================================
    // BASIC WHEN MATCHING
    // =========================================================================

    public static void WhenMatchingWithUserMethodFallback()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Configure When chain for specific values
        stub.Process.When("special").Returns("[SPECIAL HANDLING]");

        // Call with matching value - When chain handles it
        var result1 = service.Process("special");  // Returns "[SPECIAL HANDLING]"

        // Call with non-matching value - falls through to user method
        var result2 = service.Process("normal");   // Returns user method result

        // LastArg tracks the most recent call (from When or user method)
        var lastInput = stub.Process.LastArg;      // "normal"
    }

    // =========================================================================
    // PREDICATE WHEN MATCHING
    // =========================================================================

    public static void PredicateWhenWithUserMethodFallback()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Configure predicate matching
        stub.Process.When(input => input.StartsWith("VIP:", StringComparison.Ordinal))
            .Returns("[VIP CUSTOMER]");

        // Matching calls go to When chain
        var vip = service.Process("VIP:12345");     // Returns "[VIP CUSTOMER]"

        // Non-matching calls fall to user method
        var regular = service.Process("REG:12345"); // Returns user method result
    }

    // =========================================================================
    // WHEN CHAIN WITH THEN CHAINING
    // =========================================================================

    public static void WhenChainThenWhen()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Chain of When matchers
        stub.Process.When("first").Returns("[1st]")
            .ThenWhen("second").Returns("[2nd]")
            .ThenNone();  // Stop matching after sequence

        // First three calls match the chain
        var r1 = service.Process("first");    // "[1st]"
        var r2 = service.Process("second");   // "[2nd]"
        var r3 = service.Process("third");    // Falls to user method
    }

    // =========================================================================
    // MULTIPLE PARAMETERS - CALCULATOR EXAMPLE
    // =========================================================================

    public static void MultiParameterWhenMatching()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Configure specific parameter combinations
        stub.Calculate.When(0, 0).Returns(0);  // Edge case
        stub.Calculate.When((a, b) => a < 0 && b < 0).Returns(-1);  // Both negative

        // Matching calls use When chain
        var zero = service.Calculate(0, 0);      // 0 (from When)
        var neg = service.Calculate(-5, -3);     // -1 (from predicate When)

        // Non-matching calls fall to user method
        var normal = service.Calculate(10, 20);  // User method result
    }

    // =========================================================================
    // VERIFIABLE WHEN CHAINS
    // =========================================================================

    public static void VerifiableWhenChain()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Mark When chain as verifiable
        stub.Process.When("expected").Returns("result").Verifiable();

        // Call the method
        service.Process("expected");

        // Verify the When chain was fully consumed
        stub.Verify();  // Passes - When chain matched
    }

    // =========================================================================
    // MIXED SCENARIO - WHEN, SEQUENCE, AND USER METHOD
    // =========================================================================

    public static void MixedConfiguration()
    {
        var stub = new BasicUserMethodStub();
        IUserMethodService service = stub;

        // Priority: When > Sequences > OnCall > User Method
        stub.Process.When("priority1").Returns("[FROM WHEN]");

        // First call matches When
        var from_when = service.Process("priority1");  // "[FROM WHEN]"

        // Subsequent calls fall to user method (When chain exhausted for non-matching)
        var from_user = service.Process("anything");   // User method result

        // You can also configure OnCall which takes precedence over user method
        stub.Process.OnCall(input => $"[ONCALL: {input}]");
        var from_oncall = service.Process("test");     // "[ONCALL: test]"
    }
}

// =============================================================================
// DESIGN SUMMARY - BASE CLASS USER METHODS
// =============================================================================
//
// **THE BASE CLASS PATTERN:**
// KnockOff generates a base class with virtual protected methods suffixed with
// underscore (e.g., Process_). Users override these methods to provide default
// behavior. The compiler enforces signature correctness.
//
// **KEY BENEFITS:**
// 1. Clean interceptor names - stub.Process instead of stub.Process2
// 2. Compile-time safety - typos/signature errors caught by compiler
// 3. IntelliSense discovery - type override and see available methods
// 4. Same API for configured and override methods
//
// **CONFIRMED WORKING:**
// - Basic user overrides (any parameter count)
// - Clean interceptor names (stub.Process, not stub.Process2)
// - Interceptors with Verify(), LastArg, LastArgs, Reset(), Verifiable()
// - OnCall/Returns supersede user overrides per-test
// - Mixed stubs (some methods overridden, some not)
// - Strict mode bypass for overridden methods
// - Async user methods (Task<T>, Task, ValueTask<T>)
// - User method overloads (each overload independently overridable)
// - Partial overload coverage (override some, configure others)
//
// **NOT SUPPORTED (by design):**
// - Generic methods - use Of<T>() pattern instead
// - Inline stubs - only standalone pattern supports user methods
// - User-defined base classes - KnockOff generates the base class
//
// **ANSWERED QUESTIONS:**
//
// 1. INLINE PATTERN SUPPORT
//    Q: Can inline stubs ([KnockOff<T>]) have user methods?
//    A: NO - Inline stubs generate the entire class. User methods require
//       a partial class where you can add protected overrides. These are
//       fundamentally incompatible patterns.
//
// 2. ASYNC USER METHODS
//    Q: Do async user overrides work as expected?
//    A: YES - Task<T>, Task, and ValueTask<T> all work correctly.
//       - Override the virtual method (ProcessAsync_)
//       - Interceptors have OnCall/Returns with auto-wrap
//       - See AsyncUserMethodStub for working examples
//
// 3. GENERIC USER METHODS
//    Q: Can user methods be generic?
//    A: NO - Generic methods are excluded from the base class pattern.
//       - Use stub.Create.Of<T>() to configure behavior per type argument
//       - The Of<T>() pattern handles both behavior AND verification
//       - See GenericUserMethodDemo for examples
//
// 4. EXISTING BASE CLASS
//    Q: What if my stub already has a base class?
//    A: NOT SUPPORTED - KnockOff generates the base class. A diagnostic
//       (KO0200) reports an error if you try to add a base class to a
//       standalone stub.
//
// =============================================================================
