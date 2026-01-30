// -----------------------------------------------------------------------------
// Design.Stubs - Basic Property Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental property stubbing APIs:
// - OnGet(value) for constant getter return
// - OnGet(callback) for dynamic getter
// - OnSet(callback) for setter handling
// - LastSetValue capture for setters
// - VerifyGet() and VerifySet() for verification
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Properties;

// =============================================================================
// BASIC PROPERTY STUB CONFIGURATION
// =============================================================================

[KnockOff<IEntity>]
public partial class PropertyBasicsDemo
{
    // =========================================================================
    // OnGet(value) - Constant Getter Return
    // =========================================================================
    // DESIGN DECISION: OnGet(value) sets a constant return value for the getter.
    // This is the simplest configuration for get-only or get/set properties.
    //
    // GENERATOR BEHAVIOR: For get-only property:
    //
    //   public interface IEntity { int Id { get; } }
    //
    // The generator produces:
    //
    //   public class IdInterceptor
    //   {
    //       public IPropertyGetBuilder<int> OnGet(int value) { ... }
    //       public IPropertyGetBuilder<int> OnGet(Func<int> callback) { ... }
    //   }
    // =========================================================================

    public void OnGet_SetsConstantGetterValue()
    {
        var stub = new Stubs.IEntity();

        // Configure getter to return constant value
        stub.Id.OnGet(42);

        IEntity entity = stub;
        var id = entity.Id; // Returns 42
    }

    // =========================================================================
    // OnGet(callback) - Dynamic Getter
    // =========================================================================
    // DESIGN DECISION: OnGet(callback) allows dynamic getter behavior.
    // The callback is invoked each time the getter is accessed.
    //
    // This is useful for:
    // - Returning different values on subsequent accesses
    // - Computing values based on other state
    // - Tracking access patterns
    // =========================================================================

    public void OnGet_DynamicCallback()
    {
        var stub = new Stubs.IEntity();
        var accessCount = 0;

        // Callback invoked on each get
        stub.Id.OnGet(() =>
        {
            accessCount++;
            return accessCount * 10;
        });

        IEntity entity = stub;
        var first = entity.Id;  // Returns 10
        var second = entity.Id; // Returns 20
        var third = entity.Id;  // Returns 30
    }

    // =========================================================================
    // Get/Set Properties - Combined OnGet and OnSet
    // =========================================================================
    // DESIGN DECISION: For get/set properties, configure both OnGet and OnSet
    // separately. If you need a backing store pattern, implement it with
    // a closure variable.
    //
    // DID NOT DO THIS: Provide automatic backing store via Value property
    //
    // REJECTED PATTERN:
    //   stub.Description.Value = "Initial";  // No Value property exists
    //   var current = stub.Description.Value;
    //
    // WHY NOT: Keeping property interceptors simple - they configure callbacks.
    // For backing store behavior, use a closure.
    //
    // ACTUAL PATTERN:
    //   string backingStore = "Initial";
    //   stub.Description.OnGet(() => backingStore);
    //   stub.Description.OnSet((v) => backingStore = v);
    // =========================================================================

    public void GetSetProperty_WithBackingStore()
    {
        var stub = new Stubs.IEntity();

        // Implement backing store with closure
        string backingStore = "Initial";
        stub.Description.OnGet(() => backingStore);
        stub.Description.OnSet((v) => backingStore = v);

        IEntity entity = stub;
        var desc = entity.Description; // Returns "Initial"

        entity.Description = "Updated";
        desc = entity.Description; // Returns "Updated"
    }

    // =========================================================================
    // OnSet(callback) - Setter Callback
    // =========================================================================
    // DESIGN DECISION: OnSet(callback) intercepts setter calls.
    // The callback receives the value being set.
    //
    // Use this when you need to:
    // - Validate values being set
    // - Capture values for assertions
    // - Trigger side effects on set
    // =========================================================================

    public void OnSet_InterceptsSetter()
    {
        var stub = new Stubs.IEntity();
        string? lastSetValue = null;

        // Intercept sets
        stub.Description.OnSet((value) =>
        {
            lastSetValue = value;
        });

        IEntity entity = stub;
        entity.Description = "Test value";

        // lastSetValue == "Test value"
    }

    // =========================================================================
    // LastSetValue - Capture Last Set Value
    // =========================================================================
    // DESIGN DECISION: The interceptor tracks the last value that was set,
    // available via LastSetValue property on the interceptor itself.
    //
    // This is independent of OnSet configuration - it always captures.
    // =========================================================================

    public void LastSetValue_CapturesSetterArgument()
    {
        var stub = new Stubs.IEntity();

        // No OnSet needed - LastSetValue always captures
        IEntity entity = stub;
        entity.Description = "First";
        entity.Description = "Second";
        entity.Description = "Third";

        // LastSetValue is on the interceptor
        var last = stub.Description.LastSetValue;
        // last == "Third"
    }

    // =========================================================================
    // VerifyGet() and VerifySet()
    // =========================================================================
    // DESIGN DECISION: Properties have separate verification for get and set.
    // - VerifyGet() checks the getter was accessed
    // - VerifySet() checks the setter was called
    //
    // Each accepts optional Times constraint.
    // =========================================================================

    public void Verify_GetterAndSetterSeparately()
    {
        var stub = new Stubs.IEntity();
        stub.Description.OnGet("Test");

        IEntity entity = stub;

        // Access getter twice
        _ = entity.Description;
        _ = entity.Description;

        // Set once
        entity.Description = "Updated";

        // Verify getter and setter separately
        stub.Description.VerifyGet(Times.Exactly(2));
        stub.Description.VerifySet(Times.Once);
    }

    // =========================================================================
    // Get-Only Properties
    // =========================================================================
    // DESIGN DECISION: Get-only properties only generate OnGet and VerifyGet.
    // There's no OnSet, VerifySet, or LastSetValue (no setter).
    // =========================================================================

    public void GetOnlyProperty_NoSetterApis()
    {
        var stub = new Stubs.IEntity();

        // Id is get-only
        stub.Id.OnGet(100);

        IEntity entity = stub;
        var id = entity.Id; // 100

        stub.Id.VerifyGet(Times.Once);

        // These don't exist for get-only:
        // stub.Id.OnSet(...)     // Compile error
        // stub.Id.VerifySet(...) // Compile error
        // stub.Id.LastSetValue   // Compile error
    }

    // =========================================================================
    // Unconfigured Properties Return Default
    // =========================================================================
    // DESIGN DECISION: Properties without OnGet configuration return default(T).
    // In strict mode, they throw StubException.NotConfigured.
    // =========================================================================

    public void Unconfigured_ReturnsDefault()
    {
        var stub = new Stubs.IEntity();

        // Not configured - returns default
        IEntity entity = stub;
        var name = entity.Name; // Returns null (default for string)
        var id = entity.Id;     // Returns 0 (default for int)
    }

    // =========================================================================
    // Reset() - Clear Tracking
    // =========================================================================
    // DESIGN DECISION: Reset() clears tracking state (call counts, LastSetValue)
    // but preserves configuration (OnGet, OnSet).
    // =========================================================================

    public void Reset_ClearsTrackingPreservesConfig()
    {
        var stub = new Stubs.IEntity();
        stub.Description.OnGet("Test");

        IEntity entity = stub;
        _ = entity.Description;
        entity.Description = "Changed";

        stub.Description.Reset();

        // Tracking cleared - call counts now 0
        // But OnGet("Test") configuration preserved

        var value = entity.Description; // Still returns "Test"
    }
}
