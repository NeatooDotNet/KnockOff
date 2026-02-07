// -----------------------------------------------------------------------------
// Design.Stubs - Property Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for properties:
// - Get().ThenGet() for getter sequences
// - Set().ThenSet() for setter sequences
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Properties;

// =============================================================================
// PROPERTY SEQUENCES
// =============================================================================

[KnockOff<IEntity>]
public partial class PropertySequencesDemo
{
    // =========================================================================
    // Get().ThenGet() - Getter Sequences
    // =========================================================================
    // DESIGN DECISION: Getters support sequence behavior just like methods.
    // Each access to the property advances through the sequence.
    //
    // GENERATOR BEHAVIOR: Similar to method sequences:
    //
    //   public IPropertyGetSequence<string> ThenGet(string value) { ... }
    //   public IPropertyGetSequence<string> ThenGet(Func<string> callback) { ... }
    //
    // DESIGN DECISION: After exhaustion, the last value repeats (NSubstitute-like).
    // Use ThenDefault() to return default(T) after exhaustion instead.
    // =========================================================================

    public void ThenGet_GetterSequence()
    {
        var stub = new Stubs.IEntity();

        // First access returns "First", second returns "Second", then repeats "Final"
        stub.Name.Get("First")
            .ThenGet("Second")
            .ThenGet("Final");

        IEntity entity = stub;

        var r1 = entity.Name; // "First"
        var r2 = entity.Name; // "Second"
        var r3 = entity.Name; // "Final"
        var r4 = entity.Name; // "Final" (repeats last value)
    }

    // =========================================================================
    // ThenGet with Callbacks
    // =========================================================================
    // DESIGN DECISION: ThenGet can accept callbacks for dynamic values.
    // Mix and match constants and callbacks in sequences.
    // =========================================================================

    public void ThenGet_WithCallbacks()
    {
        var stub = new Stubs.IEntity();
        var counter = 0;

        stub.Name.Get("Static first")
            .ThenGet(() => $"Dynamic {++counter}")
            .ThenGet("Static final");

        IEntity entity = stub;

        var r1 = entity.Name; // "Static first"
        var r2 = entity.Name; // "Dynamic 1"
        var r3 = entity.Name; // "Static final"
        var r4 = entity.Name; // "Static final" (repeats last value)
    }

    // =========================================================================
    // Set().ThenSet() - Setter Sequences
    // =========================================================================
    // DESIGN DECISION: Setters also support sequences. Each set call advances
    // through the sequence of callbacks.
    //
    // This is useful for testing scenarios where setter behavior changes over time.
    // =========================================================================

    public void ThenSet_SetterSequence()
    {
        var stub = new Stubs.IEntity();
        var log = new List<string>();

        stub.Description.Set((v) => log.Add($"First: {v}"))
            .ThenSet((v) => log.Add($"Second: {v}"))
            .ThenSet((v) => log.Add($"Final: {v}"));

        IEntity entity = stub;

        entity.Description = "A"; // log: ["First: A"]
        entity.Description = "B"; // log: [..., "Second: B"]
        entity.Description = "C"; // log: [..., "Final: C"]
        entity.Description = "D"; // log: [..., "Final: D"] (repeats last callback)
    }

    // =========================================================================
    // ThenDefault() - Explicit Default Termination
    // =========================================================================
    // DESIGN DECISION: ThenDefault() configures the sequence to return default(T)
    // after exhaustion instead of repeating the last value.
    //
    // This matches KnockOff's original behavior and is useful when tests need
    // to detect or handle exhaustion explicitly.
    // =========================================================================

    public void ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new Stubs.IEntity();

        stub.Name.Get("First")
            .ThenGet("Second")
            .ThenDefault();  // Return default after exhaustion

        IEntity entity = stub;

        var r1 = entity.Name; // "First"
        var r2 = entity.Name; // "Second"
        var r3 = entity.Name; // null (default - sequence exhausted)
        var r4 = entity.Name; // null (still default)
    }

    // =========================================================================
    // Backing Store with Sequences
    // =========================================================================
    // DESIGN DECISION: For backing store + sequence patterns, use closures.
    // The backing store is separate from the sequence configuration.
    // =========================================================================

    public void BackingStore_WithSequences()
    {
        var stub = new Stubs.IEntity();

        // Backing store pattern - use a closure variable
        string stored = "Initial";

        // Configure getter to return from backing store
        stub.Description.Get(() => stored);

        // Configure setter to update backing store
        stub.Description.Set((v) => stored = v);

        IEntity entity = stub;

        var first = entity.Description; // "Initial"
        entity.Description = "Changed";
        var second = entity.Description; // "Changed"
    }
}
