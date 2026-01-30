// -----------------------------------------------------------------------------
// Design.Stubs - Source Delegation
// -----------------------------------------------------------------------------
// This file demonstrates the Source() API for delegating to real implementations:
// - Source(implementation) for partial stubbing
// - Unconfigured methods fall through to source
// - Reset() clears the source reference
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Advanced;

// =============================================================================
// SOURCE DELEGATION
// =============================================================================

[KnockOff<ICalculator>]
public partial class SourceDelegationDemo
{
    // =========================================================================
    // Source(implementation) - Delegate to Real Implementation
    // =========================================================================
    // DESIGN DECISION: Source() allows partial stubbing. When a method is NOT
    // configured, the stub delegates to the source implementation. When a method
    // IS configured, the stub uses the configuration.
    //
    // GENERATOR BEHAVIOR: For interface stub:
    //
    //   public void Source(ICalculator? source)
    //   {
    //       Add._source = source;
    //       Subtract._source = source;
    //       // ... for each interceptor
    //   }
    //
    // Each interceptor's Invoke method checks _source when not configured:
    //
    //   if (_source is { } src) { return src.Add(a, b); }
    // =========================================================================

    public void Source_DelegatesUnconfiguredMethods()
    {
        var stub = new Stubs.ICalculator();

        // Create a real implementation
        var realCalculator = new RealCalculator();

        // Set source for delegation
        stub.Source(realCalculator);

        ICalculator calc = stub;

        // Add is not configured - delegates to source
        var sum = calc.Add(2, 3); // Returns 5 (from real implementation)

        // Subtract is not configured - delegates to source
        var diff = calc.Subtract(10, 4); // Returns 6 (from real implementation)
    }

    // =========================================================================
    // Partial Stubbing - Configure Some, Delegate Others
    // =========================================================================
    // DESIGN DECISION: You can configure specific methods while letting others
    // delegate to the source. This is useful for:
    // - Testing specific method interactions
    // - Injecting errors in specific scenarios
    // - Isolating behavior for unit tests
    // =========================================================================

    public void Source_PartialStubbing()
    {
        var stub = new Stubs.ICalculator();
        var realCalculator = new RealCalculator();

        stub.Source(realCalculator);

        // Configure Add to return a specific value
        stub.Add.Returns(999);

        ICalculator calc = stub;

        // Add uses the stub configuration
        var sum = calc.Add(2, 3); // Returns 999 (stub)

        // Subtract still delegates to source
        var diff = calc.Subtract(10, 4); // Returns 6 (real)
    }

    // =========================================================================
    // Source Priority vs OnCall Configuration
    // =========================================================================
    // DESIGN DECISION: OnCall/Returns configuration always takes priority over
    // Source delegation. The source is only used when:
    // 1. The method is NOT configured (no OnCall/Returns)
    // 2. A When chain doesn't match (falls through to source)
    //
    // Order of evaluation:
    // 1. When chain (if configured)
    // 2. OnCall/Returns configuration
    // 3. Source delegation
    // 4. Default value (if not strict)
    // =========================================================================

    public void Source_ConfigurationTakesPriority()
    {
        var stub = new Stubs.ICalculator();
        var realCalculator = new RealCalculator();

        stub.Source(realCalculator);

        // Configure Divide to handle specific case
        stub.Divide.When(10, 2).Returns(5);

        ICalculator calc = stub;

        // Configured case - uses stub
        var r1 = calc.Divide(10, 2); // Returns 5 (stub)

        // Unconfigured case - falls through to source
        var r2 = calc.Divide(20, 4); // Returns 5 (real implementation)
    }

    // =========================================================================
    // Source(null) - Remove Delegation
    // =========================================================================
    // DESIGN DECISION: Passing null to Source() removes the delegation.
    // After this, unconfigured methods return default (or throw in strict mode).
    // =========================================================================

    public void Source_NullRemovesDelegation()
    {
        var stub = new Stubs.ICalculator();
        var realCalculator = new RealCalculator();

        stub.Source(realCalculator);

        ICalculator calc = stub;

        // Delegates to source
        var r1 = calc.Add(2, 3); // Returns 5

        // Remove source
        stub.Source(null);

        // No longer delegates - returns default
        var r2 = calc.Add(2, 3); // Returns 0 (default)
    }

    // =========================================================================
    // Reset() DOES Clear Source Reference
    // =========================================================================
    // DESIGN DECISION: Reset() clears ALL state on an interceptor, including
    // the source reference. This provides a clean slate for test isolation.
    //
    // GENERATOR BEHAVIOR: The interceptor's Reset() method includes:
    //
    //   public void Reset()
    //   {
    //       _source = null;
    //       // ... clears callbacks, call counts, etc.
    //   }
    //
    // If you need to keep the source while clearing call counts, re-establish
    // the source after calling Reset().
    // =========================================================================

    public void Reset_ClearsSourceReference()
    {
        var stub = new Stubs.ICalculator();
        var realCalculator = new RealCalculator();

        stub.Source(realCalculator);

        ICalculator calc = stub;

        // Delegates to source
        var r1 = calc.Add(2, 3); // Returns 5

        // Reset clears source (ACTUAL BEHAVIOR)
        stub.Add.Reset();

        // Source is cleared - returns default
        var r2 = calc.Add(2, 3); // Returns 0 (default)

        // Re-establish source if needed
        stub.Source(realCalculator);
    }

    // =========================================================================
    // COMMON MISTAKE: Expecting Source to Work with Class Stubs
    // =========================================================================
    //
    // COMMON MISTAKE: Using Source() with class stubs expecting full delegation
    //
    // For class stubs (ServiceBase), Source() only affects virtual/abstract
    // members. Non-virtual members are inherited from the base class and cannot
    // be intercepted or delegated.
    //
    // For interface stubs, all members can be delegated via Source().
    // =========================================================================

    // =========================================================================
    // DESIGN DECISION: Source Only Available for Interface Stubs
    // =========================================================================
    // Class stubs (stubbing abstract/virtual members) do NOT have a Source()
    // method. This is because:
    // 1. The stub IS-A the class, so non-virtual members come from the class
    // 2. Only virtual/abstract members can be overridden
    // 3. Delegation would require wrapping, not inheritance
    //
    // For class stubs, use the base class constructor or configure each
    // virtual member individually.
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
