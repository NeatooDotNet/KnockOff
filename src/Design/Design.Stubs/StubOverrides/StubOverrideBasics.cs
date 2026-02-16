// -----------------------------------------------------------------------------
// Design.Stubs - Stub Overrides
// -----------------------------------------------------------------------------
// This file explores and documents user-defined method patterns:
// - Base class pattern (protected override methods with _ suffix)
// - Tracking API on interceptors (Verify, LastArg, Reset)
// - Returns/Execute to supersede stub overrides
// - Stub override overloads
// - Mixed scenarios (some with stub overrides, some without)
// - Priority: Returns/Execute > stub override
// - Strict mode behavior
// -----------------------------------------------------------------------------

using System.Text;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.StubOverrides;

// =============================================================================
// STUB OVERRIDES - OVERVIEW
// =============================================================================
//
// WHAT ARE STUB OVERRIDES?
// Stub overrides are protected override methods you define in a partial stub class
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
// KnockOff generates a base class (e.g., BasicStubOverrideStubBase) with virtual
// protected methods suffixed with underscore (e.g., Process_). You override
// these methods to provide default behavior:
//
//   // Generated base class (BasicStubOverrideStubBase):
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
// BASIC STUB OVERRIDES
// =============================================================================

[KnockOff]
public partial class BasicStubOverrideStub : IStubOverrideService { }

public partial class BasicStubOverrideStub
{
    // =========================================================================
    // Stub Override Definition - Base Class Pattern
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
    // GENERATED BASE CLASS (BasicStubOverrideStubBase):
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

[KnockOff<IStubOverrideService>]
public partial class StubOverrideBasicsDemo
{
    // =========================================================================
    // Using Stubs with Stub Overrides
    // =========================================================================

    public void BasicUsage()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

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
    // - Verify(Called) - verify call count
    // - LastArgs - last arguments (raw type for 1-param, tuple for 2+ params)
    // - Reset() - clear tracking state
    // - Returns()/Execute() - supersede stub override per-test
    // - Returns() - constant value that supersedes stub override
    // =========================================================================

    public void TrackingWithVerify()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        service.Process("first");
        service.Process("second");
        service.Process("third");

        // Verify with clean interceptor name
        stub.Process.Verify(Called.Exactly(3));
        stub.Process.Verify(Called.AtLeast(2));
    }

    public void TrackingWithLastArg()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        service.FindById(42);
        service.FindById(99);

        // LastArg gives the most recent argument (singular for single-param methods)
        var lastId = stub.FindById.LastArg;
        // lastId == 99
    }

    public void TrackingWithLastArgs()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        service.Calculate(10, 20);
        service.Calculate(30, 40);

        // LastArgs gives tuple of most recent arguments (nullable)
        var (a, b) = stub.Calculate.LastArgs!.Value;
        // a == 30, b == 40
    }

    public void TrackingWithReset()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        service.Process("test");
        stub.Process.Verify(Called.Once);

        // Reset clears all tracking state
        stub.Process.Reset();

        stub.Process.Verify(Called.Never);
        // LastArg is now default (null for reference types)
    }

    // =========================================================================
    // Returns()/Execute() Supersedes Stub Overrides
    // =========================================================================
    // Stub overrides provide shareable defaults. Returns/Execute override per test.
    //
    // RATIONALE: Stubs should be shareable yet configurable per test.
    // - Stub override = sensible default for most tests
    // - Returns/Execute = override when a specific test needs different behavior
    //
    // GENERATED CODE PATTERN:
    //   if (Interceptor.Callback is { } callback) return callback(args);  // Returns wins
    //   return Process_(args);                                            // Stub override
    //
    // Reset() behavior: Clears tracking state but PRESERVES Returns/Execute configuration.
    // This matches regular interceptor semantics.
    // =========================================================================

    public void Returns_SupersedesStubOverride_Callback()
    {
        var stub = new BasicStubOverrideStub();

        // By default, stub override is called
        IStubOverrideService service = stub;
        var defaultResult = service.Process("hello");
        // defaultResult == "[Processed: hello]" (from Process_ override)

        // Returns supersedes the stub override for per-test override
        stub.Process.Return(input => $"[Override: {input}]");
        var overrideResult = service.Process("hello");
        // overrideResult == "[Override: hello]" (Returns wins)

        stub.Process.Verify(Called.Exactly(2)); // Both calls tracked
    }

    public void Returns_SupersedesStubOverride()
    {
        var stub = new BasicStubOverrideStub();
        stub.Process.Return("constant"); // Returns(value) is shorthand for Returns(_ => value)

        IStubOverrideService service = stub;
        var result = service.Process("ignored");
        // result == "constant" (Returns wins, ignores argument)
    }

    public void VoidMethod_ExecuteSupersedes()
    {
        var stub = new BasicStubOverrideStub();
        var callbackInvoked = false;
        stub.Execute.Call(cmd => callbackInvoked = true);

        IStubOverrideService service = stub;
        service.Execute("test");

        // callbackInvoked == true (Execute was invoked, not stub override)
        stub.Execute.Verify(Called.Once);
    }

    public void Reset_PreservesReturnsConfiguration()
    {
        var stub = new BasicStubOverrideStub();
        stub.Calculate.Return((a, b) => a * b); // Override addition with multiplication

        IStubOverrideService service = stub;
        service.Calculate(3, 4);
        stub.Calculate.Verify(Called.Once);

        // Reset clears tracking but preserves Returns
        stub.Calculate.Reset();
        stub.Calculate.Verify(Called.Never);

        var result = service.Calculate(5, 6);
        // result == 30 (Returns still active: 5 * 6)
    }
}

// =============================================================================
// STUB OVERRIDE OVERLOADS
// =============================================================================
// Stub overrides work naturally with overloads. Each overload gets its own
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

// Generator produces per-signature RecordCall methods for stub override methods
[KnockOff]
public partial class OverloadedStubOverrideStub : IOverloadedStubOverrideService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class OverloadedStubOverrideStub
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
public partial class MixedStubOverrideStub : IMixedStubOverrideService { }

public partial class MixedStubOverrideStub
{
    // Only override SOME interface methods as stub overrides
    // Others will use the regular interceptor API (no override needed)

    protected override string WithStubOverride_(string input)
    {
        return $"[User: {input}]";
    }

    protected override int ComputeWithStubOverride_(int value)
    {
        return value * 2;
    }

    // WithoutStubOverride_ and ComputeWithoutStubOverride_ are NOT overridden
    // They use the regular interceptor path (Returns, Execute, or default)
}

[KnockOff<IMixedStubOverrideService>]
public partial class MixedStubOverrideDemo
{
    public void MixedScenario()
    {
        var stub = new MixedStubOverrideStub();

        // Methods WITH stub override use the interceptor for tracking + Returns
        stub.WithStubOverride.Verify(Called.Never);

        // Methods WITHOUT stub override also use interceptor (same API)
        stub.WithoutStubOverride.Return((input) => $"[Configured: {input}]");
        stub.WithoutStubOverride.Verify(Called.Never);

        stub.ComputeWithoutStubOverride.Return(42);

        IMixedStubOverrideService service = stub;

        var r1 = service.WithStubOverride("test");       // "[User: test]" (from override)
        var r2 = service.WithoutStubOverride("test");    // "[Configured: test]" (from Returns)
        var r3 = service.ComputeWithStubOverride(5);     // 10 (from override)
        var r4 = service.ComputeWithoutStubOverride(5);  // 42 (from Returns)

        stub.WithStubOverride.Verify(Called.Once);
        stub.WithoutStubOverride.Verify(Called.Once);
    }

    // =========================================================================
    // DESIGN OBSERVATION: Consistent Naming
    // =========================================================================
    // With the base class pattern, interceptor names are ALWAYS clean:
    // - Stub override present: stub.Method (with override behavior)
    // - No stub override: stub.Method (default/interceptor behavior)
    //
    // The presence or absence of a stub override is detected at compile time.
    // When override exists: Returns > Stub override
    // When no override: Returns > Strict/Default
    // =========================================================================
}

// =============================================================================
// PARTIAL STUB OVERRIDE COVERAGE FOR OVERLOADS
// =============================================================================
// You can override only SOME overloads. Each overload is handled independently:
// - Overridden: Returns > Stub override
// - Not overridden: Returns > Strict/Default
//
// Example: Format has 3 overloads, but only Format(string) is overridden.
// - Format(string) calls Format_(string) (your override)
// - Format(string,bool) uses interceptor path (no override)
// - Format(string,bool,int) uses interceptor path (no override)
//
// All overloads share the same interceptor (stub.Format) for tracking.
// =============================================================================

[KnockOff]
public partial class PartialOverloadStubOverrideStub : IOverloadedStubOverrideService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class PartialOverloadStubOverrideStub
{
    // Only Format(string) is overridden - the other two Format overloads are NOT
    protected override string Format_(string input) => $"[User1: {input}]";

    // All Log overloads have stub overrides
    protected override void Log_(string message) { }
    protected override void Log_(string message, int level) { }
}
#pragma warning restore CA1062

// =============================================================================
// STRICT MODE AND STUB OVERRIDES
// =============================================================================

[KnockOff(Strict = true)]
public partial class StrictStubOverrideStub : IStubOverrideService { }

public partial class StrictStubOverrideStub
{
    // Stub overrides bypass strict mode - they ARE the configuration
    protected override string Process_(string input) => $"[Strict: {input}]";

    // Only Process_ is overridden
    // Other methods will throw in strict mode if called without Returns/Execute configuration
}

[KnockOff<IStubOverrideService>]
public partial class StrictModeStubOverrideDemo
{
    public void StrictMode_StubOverridesBypassed()
    {
        var stub = new StrictStubOverrideStub();
        IStubOverrideService service = stub;

        // Stub override works in strict mode - it IS the configuration
        var result = service.Process("test");  // "[Strict: test]"

        // Non-overridden method in strict mode would throw without Returns
        // service.Calculate(1, 2);  // Would throw - no override, no Returns
    }

    // =========================================================================
    // DESIGN DECISION: Stub Overrides Bypass Strict Mode
    // =========================================================================
    // Strict mode means "throw if unconfigured". Stub overrides ARE configured
    // by their very existence. This is consistent - the override IS the
    // behavior, just defined in a different way than Returns/Execute.
    // =========================================================================
}

// =============================================================================
// ASYNC STUB OVERRIDES
// =============================================================================

[KnockOff]
public partial class AsyncStubOverrideStub : IAsyncStubOverrideService { }

public partial class AsyncStubOverrideStub
{
    // =========================================================================
    // Async Stub Overrides - Task<T>, Task, ValueTask<T>
    // =========================================================================
    // Async stub overrides work exactly like sync methods - override the virtual
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

[KnockOff<IAsyncStubOverrideService>]
public partial class AsyncStubOverrideDemo
{
    public async Task AsyncStubOverride_BasicUsage()
    {
        var stub = new AsyncStubOverrideStub();
        IAsyncStubOverrideService service = stub;

        // Call async stub override
        var result = await service.ProcessAsync("hello");
        // result == "[Async: hello]"

        // Verify with clean interceptor name
        stub.ProcessAsync.Verify(Called.Once);
    }

    public async Task AsyncStubOverride_VoidTask()
    {
        var stub = new AsyncStubOverrideStub();
        IAsyncStubOverrideService service = stub;

        await service.ExecuteAsync("command");

        stub.ExecuteAsync.Verify(Called.Once);
        // LastArg captures the command (singular for single-param methods)
        var lastCommand = stub.ExecuteAsync.LastArg;
    }

    public async Task AsyncStubOverride_ValueTask()
    {
        var stub = new AsyncStubOverrideStub();
        IAsyncStubOverrideService service = stub;

        var result = await service.ComputeAsync(21);
        // result == 42

        stub.ComputeAsync.Verify(Called.Once);
    }
}

// =============================================================================
// GENERIC STUB OVERRIDES - EXCLUDED FROM BASE CLASS PATTERN
// =============================================================================

[KnockOff]
public partial class GenericStubOverrideStub : IGenericStubOverrideService { }

public partial class GenericStubOverrideStub
{
    // =========================================================================
    // Generic Stub Overrides - NOT SUPPORTED in Base Class Pattern
    // =========================================================================
    // DESIGN DECISION: Generic methods are EXCLUDED from the base class pattern.
    //
    // WHY:
    // 1. Stub override is ONE method for ALL type arguments
    // 2. The Of<T>() pattern allows DIFFERENT callbacks per type argument
    // 3. These are fundamentally different approaches
    //
    // For generic methods, use the standard interceptor API:
    //   stub.Create.Of<List<int>>().Return(new List<int>());
    //   stub.Create.Of<User>().Return(() => new User { Id = 1 });
    //
    // The Of<T>() pattern handles both behavior AND verification consistently.
    // =========================================================================

    // These are NOT stub overrides - generic methods don't get base class virtuals
    // Use stub.Create.Of<T>() instead

    // If you need generic "default" behavior, you can still use:
    //   stub.Create.Of<List<int>>().Return(new List<int>());
    //   stub.Transform.Of<string, StringBuilder>().Return(new StringBuilder());
}

[KnockOff<IGenericStubOverrideService>]
public partial class GenericStubOverrideDemo
{
    public void GenericMethod_UseOfTPattern()
    {
        var stub = new GenericStubOverrideStub();
        IGenericStubOverrideService service = stub;

        // Configure via Of<T>().Return() - this is the ONLY way for generic methods
        stub.Create.Of<List<int>>().Return(() => new List<int> { 1, 2, 3 });

        var list = service.Create<List<int>>();
        // list == { 1, 2, 3 }

        // Verify with Of<T>()
        stub.Create.Of<List<int>>().Verify(Called.Once);
    }

    public void GenericMethod_MultiTypeParam()
    {
        var stub = new GenericStubOverrideStub();
        IGenericStubOverrideService service = stub;

        // Multi-type-parameter methods also use Of<T>().Return()
        stub.Transform.Of<string, StringBuilder>().Return(input => new StringBuilder($"transformed: {input}"));

        var result = service.Transform<string, StringBuilder>("hello");
        // result.ToString() == "transformed: hello"

        stub.Transform.Of<string, StringBuilder>().Verify(Called.Once);
    }
}

// =============================================================================
// OVERLOADED GENERIC STUB OVERRIDES - EXCLUDED FROM BASE CLASS PATTERN
// =============================================================================
// Generic methods (including overloaded ones) are excluded from the base class
// pattern. Use the standard Of<T>() interceptor API for all generic methods.
//
// NOTE: Overloaded generic methods have complex code generation requirements.
// See IGenericStubOverrideService (single overload) for working examples.
// =============================================================================

// OverloadedGenericStubOverrideStub removed - generator has a known issue with
// overloaded generic methods. See IGenericStubOverrideService for working examples.

// =============================================================================
// WHEN CHAIN SUPPORT WITH STUB OVERRIDES
// =============================================================================
//
// Stub override stubs now support the full .When() API. When chains have highest
// priority, allowing per-test customization while preserving stub override fallback.
//
// PRIORITY CHAIN:
// 1. When chains (parameter-specific matching)
// 2. Sequences (ThenReturns/ThenExecute chain)
// 3. Returns/Execute (explicit configuration)
// 4. Stub Override (fallback - always called if nothing else matches)
//
// This is the same priority order as inline stubs, except stub override replaces
// Source/Strict as the final fallback.

public static class WhenChainStubOverrideDemo
{
    // =========================================================================
    // BASIC WHEN MATCHING
    // =========================================================================

    public static void WhenMatchingWithStubOverrideFallback()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Configure When chain for specific values
        stub.Process.When("special").Return("[SPECIAL HANDLING]");

        // Call with matching value - When chain handles it
        var result1 = service.Process("special");  // Returns "[SPECIAL HANDLING]"

        // Call with non-matching value - falls through to stub override
        var result2 = service.Process("normal");   // Returns stub override result

        // LastArg tracks the most recent call (from When or stub override)
        var lastInput = stub.Process.LastArg;       // "normal"
    }

    // =========================================================================
    // PREDICATE WHEN MATCHING
    // =========================================================================

    public static void PredicateWhenWithStubOverrideFallback()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Configure predicate matching
        stub.Process.When(input => input.StartsWith("VIP:", StringComparison.Ordinal))
            .Return("[VIP CUSTOMER]");

        // Matching calls go to When chain
        var vip = service.Process("VIP:12345");     // Returns "[VIP CUSTOMER]"

        // Non-matching calls fall to stub override
        var regular = service.Process("REG:12345"); // Returns stub override result
    }

    // =========================================================================
    // WHEN CHAIN WITH THEN CHAINING
    // =========================================================================

    public static void WhenChainThenWhen()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Chain of When matchers
        stub.Process.When("first").Return("[1st]")
            .ThenWhen("second").Return("[2nd]")
            .ThenNone();  // Stop matching after sequence

        // First three calls match the chain
        var r1 = service.Process("first");    // "[1st]"
        var r2 = service.Process("second");   // "[2nd]"
        var r3 = service.Process("third");    // Falls to stub override
    }

    // =========================================================================
    // MULTIPLE PARAMETERS - CALCULATOR EXAMPLE
    // =========================================================================

    public static void MultiParameterWhenMatching()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Configure specific parameter combinations
        stub.Calculate.When((0, 0)).Return(0);  // Edge case
        stub.Calculate.When(args => args.a < 0 && args.b < 0).Return(-1);  // Both negative

        // Matching calls use When chain
        var zero = service.Calculate(0, 0);      // 0 (from When)
        var neg = service.Calculate(-5, -3);     // -1 (from predicate When)

        // Non-matching calls fall to stub override
        var normal = service.Calculate(10, 20);  // Stub override result
    }

    // =========================================================================
    // VERIFIABLE WHEN CHAINS
    // =========================================================================

    public static void VerifiableWhenChain()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Mark When chain as verifiable
        stub.Process.When("expected").Return("result").Verifiable();

        // Call the method
        service.Process("expected");

        // Verify the When chain was fully consumed
        stub.Verify();  // Passes - When chain matched
    }

    // =========================================================================
    // MIXED SCENARIO - WHEN, SEQUENCE, AND STUB OVERRIDE
    // =========================================================================

    public static void MixedConfiguration()
    {
        var stub = new BasicStubOverrideStub();
        IStubOverrideService service = stub;

        // Priority: When > Sequences > Returns > Stub Override
        stub.Process.When("priority1").Return("[FROM WHEN]");

        // First call matches When
        var from_when = service.Process("priority1");  // "[FROM WHEN]"

        // Subsequent calls fall to stub override (When chain exhausted for non-matching)
        var from_user = service.Process("anything");   // Stub override result

        // You can also configure Returns which takes precedence over stub override
        stub.Process.Return(input => $"[RETURNS: {input}]");
        var from_returns = service.Process("test");     // "[RETURNS: test]"
    }
}

// =============================================================================
// DESIGN SUMMARY - BASE CLASS STUB OVERRIDES
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
// - Basic stub overrides (any parameter count)
// - Clean interceptor names (stub.Process, not stub.Process2)
// - Interceptors with Verify(), LastArg, LastArgs, Reset(), Verifiable()
// - Returns/Execute supersede stub overrides per-test
// - Mixed stubs (some methods overridden, some not)
// - Strict mode bypass for overridden methods
// - Async stub overrides (Task<T>, Task, ValueTask<T>)
// - Stub override overloads (each overload independently overridable)
// - Partial overload coverage (override some, configure others)
//
// **NOT SUPPORTED (by design):**
// - Generic methods - use Of<T>() pattern instead
// - Inline stubs - only standalone pattern supports stub overrides
// - User-defined base classes - KnockOff generates the base class
//
// **ANSWERED QUESTIONS:**
//
// 1. INLINE PATTERN SUPPORT
//    Q: Can inline stubs ([KnockOff<T>]) have stub overrides?
//    A: NO - Inline stubs generate the entire class. Stub overrides require
//       a partial class where you can add protected overrides. These are
//       fundamentally incompatible patterns.
//
// 2. ASYNC STUB OVERRIDES
//    Q: Do async stub overrides work as expected?
//    A: YES - Task<T>, Task, and ValueTask<T> all work correctly.
//       - Override the virtual method (ProcessAsync_)
//       - Interceptors have Returns/Execute with auto-wrap
//       - See AsyncStubOverrideStub for working examples
//
// 3. GENERIC STUB OVERRIDES
//    Q: Can stub overrides be generic?
//    A: NO - Generic methods are excluded from the base class pattern.
//       - Use stub.Create.Of<T>() to configure behavior per type argument
//       - The Of<T>() pattern handles both behavior AND verification
//       - See GenericStubOverrideDemo for examples
//
// 4. EXISTING BASE CLASS
//    Q: What if my stub already has a base class?
//    A: NOT SUPPORTED - KnockOff generates the base class. A diagnostic
//       (KO0200) reports an error if you try to add a base class to a
//       standalone stub.
//
// =============================================================================
