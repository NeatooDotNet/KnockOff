// -----------------------------------------------------------------------------
// Design.Stubs - Strict Mode
// -----------------------------------------------------------------------------
// This file demonstrates strict mode behavior:
// - Strict property on stubs
// - Constructor parameter for strict
// - StubException for unconfigured calls
// - Behavior differences from loose mode
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Advanced;

// =============================================================================
// STRICT MODE
// =============================================================================

[KnockOff<ICalculator>]
public partial class StrictModeDemo
{
    // =========================================================================
    // Loose Mode (Default) - Returns Default Values
    // =========================================================================
    // DESIGN DECISION: By default, stubs are "loose" - unconfigured methods
    // return default(T) for their return type. This makes tests less brittle
    // and is the common case for most unit tests.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public bool Strict { get; set; } = false;
    //
    //   // In Invoke:
    //   if (strict) throw StubException.NotConfigured(...);
    //   return default!;
    // =========================================================================

    public void LooseMode_ReturnsDefault()
    {
        // Loose mode is the default
        var stub = new Stubs.ICalculator();

        ICalculator calc = stub;

        // Not configured - returns default(int) = 0
        var sum = calc.Add(2, 3); // Returns 0

        // No exception thrown
    }

    // =========================================================================
    // Strict Mode via Constructor
    // =========================================================================
    // DESIGN DECISION: Strict mode can be enabled via constructor parameter.
    // When strict, ANY unconfigured method call throws StubException.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public ICalculator(bool strict = false)
    //   {
    //       Strict = strict;
    //   }
    // =========================================================================

    public void StrictMode_ViaConstructor()
    {
        // Enable strict mode via constructor
        var stub = new Stubs.ICalculator(strict: true);

        ICalculator calc = stub;

        // This would throw StubException.NotConfigured
        // var sum = calc.Add(2, 3); // Throws!
    }

    // =========================================================================
    // Strict Mode via Property
    // =========================================================================
    // DESIGN DECISION: Strict can also be toggled via property. This allows
    // changing behavior mid-test (though this is rare).
    // =========================================================================

    public void StrictMode_ViaProperty()
    {
        var stub = new Stubs.ICalculator();

        // Start loose
        ICalculator calc = stub;
        var r1 = calc.Add(2, 3); // Returns 0

        // Switch to strict
        stub.Strict = true;

        // Now unconfigured calls throw
        // var r2 = calc.Add(2, 3); // Throws StubException!

        // Switch back to loose
        stub.Strict = false;
        var r3 = calc.Add(2, 3); // Returns 0 again
    }

    // =========================================================================
    // Strict Mode with Configuration
    // =========================================================================
    // DESIGN DECISION: Strict mode only throws for UNCONFIGURED methods.
    // Configured methods work normally in both modes.
    // =========================================================================

    public void StrictMode_ConfiguredMethodsWork()
    {
        var stub = new Stubs.ICalculator(strict: true);

        // Configure Add
        stub.Add.Returns(42);

        ICalculator calc = stub;

        // Configured method works fine
        var sum = calc.Add(2, 3); // Returns 42

        // Unconfigured method would throw
        // var diff = calc.Subtract(10, 4); // Throws!
    }

    // =========================================================================
    // Strict Mode with Source Delegation
    // =========================================================================
    // DESIGN DECISION: When Source() is set, unconfigured methods delegate
    // to the source and do NOT throw in strict mode. The method is effectively
    // "configured" by having a source to delegate to.
    // =========================================================================

    public void StrictMode_WithSource()
    {
        var stub = new Stubs.ICalculator(strict: true);
        var realCalculator = new RealCalculator();

        stub.Source(realCalculator);

        ICalculator calc = stub;

        // Delegates to source - no exception even in strict mode
        var sum = calc.Add(2, 3); // Returns 5 (from source)
    }

    // =========================================================================
    // StubException.NotConfigured
    // =========================================================================
    // DESIGN DECISION: The exception thrown is StubException with a specific
    // NotConfigured factory method. This provides clear error messages.
    //
    // Exception message format:
    //   "Method 'Add' on stub 'ICalculator' was called but not configured."
    // =========================================================================

    public void StrictMode_ExceptionDetails()
    {
        var stub = new Stubs.ICalculator(strict: true);

        ICalculator calc = stub;

        try
        {
            calc.Add(2, 3);
        }
        catch (StubException ex)
        {
            // ex.Message contains method name and stub type
            // ex is StubException (KnockOff.StubException)
            _ = ex.Message;
        }
    }

    // =========================================================================
    // Strict Mode for Properties
    // =========================================================================
    // DESIGN DECISION: Properties follow the same strict mode rules.
    // Unconfigured property getters throw in strict mode.
    // =========================================================================

    // Note: ICalculator doesn't have properties, so we use IEntity here
    // This is documented but not demonstrated due to interface constraints

    // =========================================================================
    // COMMON MISTAKE: Forgetting to Configure in Strict Mode
    // =========================================================================
    //
    // COMMON MISTAKE: Using strict mode without configuring all called methods
    //
    // Strict mode is best used when you want to ensure ALL interactions are
    // explicitly configured. If you only care about specific methods, loose
    // mode is often more appropriate.
    //
    // GOOD PATTERN: Use strict mode when testing specific contracts
    // BAD PATTERN: Use strict mode and then configure everything with defaults
    // =========================================================================

    public void StrictMode_BestPractice()
    {
        // Good: Strict mode when you want explicit control
        var strictStub = new Stubs.ICalculator(strict: true);
        strictStub.Add.Returns(42);
        strictStub.Subtract.Returns(10);
        // Now all called methods are explicitly configured

        // Good: Loose mode when you only care about specific methods
        var looseStub = new Stubs.ICalculator();
        looseStub.Add.Returns(42);
        // Subtract returns 0, but we don't care about it in this test
    }

    // =========================================================================
    // DESIGN DECISION: No Method-Level Strict Control
    // =========================================================================
    // Strict mode is at the stub level, not per-method. Either the entire stub
    // is strict or it's loose.
    //
    // DID NOT DO THIS: Per-method strict configuration
    //
    // REJECTED PATTERN:
    //   stub.Add.Strict();  // No method-level strict flag
    //
    // WHY NOT: Simplicity. Strict vs loose is a testing philosophy choice
    // that typically applies to the entire test, not individual methods.
    // =========================================================================

    // Helper class for demonstration
    private sealed class RealCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Divide(int a, int b) => b == 0 ? 0 : a / b;
        public void Reset() { }
    }
}
