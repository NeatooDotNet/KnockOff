// -----------------------------------------------------------------------------
// Design.Stubs - Property Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for properties:
// - OnGet().ThenGet() for getter sequences
// - OnSet().ThenSet() for setter sequences
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
    // OnGet().ThenGet() - Getter Sequences
    // =========================================================================
    // DESIGN DECISION: Getters support sequence behavior just like methods.
    // Each access to the property advances through the sequence.
    //
    // GENERATOR BEHAVIOR: Similar to method sequences:
    //
    //   public IPropertyGetSequence<string> ThenGet(string value) { ... }
    //   public IPropertyGetSequence<string> ThenGet(Func<string> callback) { ... }
    //
    // DESIGN DECISION: Sequences exhaust after all values are consumed.
    // After exhaustion, unconfigured gets return default(T).
    // =========================================================================

    public void ThenGet_GetterSequence()
    {
        var stub = new Stubs.IEntity();

        // First access returns "First", second returns "Second", then repeats "Final"
        stub.Name.OnGet("First")
            .ThenGet("Second")
            .ThenGet("Final");

        IEntity entity = stub;

        var r1 = entity.Name; // "First"
        var r2 = entity.Name; // "Second"
        var r3 = entity.Name; // "Final"
        var r4 = entity.Name; // null (sequence exhausted, returns default)
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

        stub.Name.OnGet("Static first")
            .ThenGet(() => $"Dynamic {++counter}")
            .ThenGet("Static final");

        IEntity entity = stub;

        var r1 = entity.Name; // "Static first"
        var r2 = entity.Name; // "Dynamic 1"
        var r3 = entity.Name; // "Static final"
        var r4 = entity.Name; // null (sequence exhausted)
    }

    // =========================================================================
    // OnSet().ThenSet() - Setter Sequences
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

        stub.Description.OnSet((v) => log.Add($"First: {v}"))
            .ThenSet((v) => log.Add($"Second: {v}"))
            .ThenSet((v) => log.Add($"Final: {v}"));

        IEntity entity = stub;

        entity.Description = "A"; // log: ["First: A"]
        entity.Description = "B"; // log: [..., "Second: B"]
        entity.Description = "C"; // log: [..., "Final: C"]
        entity.Description = "D"; // No callback - sequence exhausted
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
        stub.Description.OnGet(() => stored);

        // Configure setter to update backing store
        stub.Description.OnSet((v) => stored = v);

        IEntity entity = stub;

        var first = entity.Description; // "Initial"
        entity.Description = "Changed";
        var second = entity.Description; // "Changed"
    }
}
