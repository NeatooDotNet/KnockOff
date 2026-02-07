// -----------------------------------------------------------------------------
// Design.Stubs - Basic Property Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental property stubbing APIs:
// - Get(value) for constant getter return
// - Get(callback) for dynamic getter
// - Set(callback) for setter handling
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
    // Get(value) - Constant Getter Return
    // =========================================================================
    // DESIGN DECISION: Get(value) sets a constant return value for the getter.
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
    //       public IPropertyGetBuilder<int> Get(int value) { ... }
    //       public IPropertyGetBuilder<int> Get(Func<int> callback) { ... }
    //   }
    // =========================================================================

    public void Get_SetsConstantGetterValue()
    {
        var stub = new Stubs.IEntity();

        // Configure getter to return constant value
        stub.Id.Get(42);

        IEntity entity = stub;
        var id = entity.Id; // Returns 42
    }

    // =========================================================================
    // Get(callback) - Dynamic Getter
    // =========================================================================
    // DESIGN DECISION: Get(callback) allows dynamic getter behavior.
    // The callback is invoked each time the getter is accessed.
    //
    // This is useful for:
    // - Returning different values on subsequent accesses
    // - Computing values based on other state
    // - Tracking access patterns
    // =========================================================================

    public void Get_DynamicCallback()
    {
        var stub = new Stubs.IEntity();
        var accessCount = 0;

        // Callback invoked on each get
        stub.Id.Get(() =>
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
    // Get/Set Properties - Combined Get and Set
    // =========================================================================
    // DESIGN DECISION: For get/set properties, configure both Get and Set
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
    //   stub.Description.Get(() => backingStore);
    //   stub.Description.Set((v) => backingStore = v);
    // =========================================================================

    public void GetSetProperty_WithBackingStore()
    {
        var stub = new Stubs.IEntity();

        // Implement backing store with closure
        string backingStore = "Initial";
        stub.Description.Get(() => backingStore);
        stub.Description.Set((v) => backingStore = v);

        IEntity entity = stub;
        var desc = entity.Description; // Returns "Initial"

        entity.Description = "Updated";
        desc = entity.Description; // Returns "Updated"
    }

    // =========================================================================
    // Set(callback) - Setter Callback
    // =========================================================================
    // DESIGN DECISION: Set(callback) intercepts setter calls.
    // The callback receives the value being set.
    //
    // Use this when you need to:
    // - Validate values being set
    // - Capture values for assertions
    // - Trigger side effects on set
    // =========================================================================

    public void Set_InterceptsSetter()
    {
        var stub = new Stubs.IEntity();
        string? lastSetValue = null;

        // Intercept sets
        stub.Description.Set((value) =>
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
    // This is independent of Set configuration - it always captures.
    // =========================================================================

    public void LastSetValue_CapturesSetterArgument()
    {
        var stub = new Stubs.IEntity();

        // No Set needed - LastSetValue always captures
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
        stub.Description.Get("Test");

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
    // DESIGN DECISION: Get-only properties only generate Get and VerifyGet.
    // There's no Set, VerifySet, or LastSetValue (no setter).
    // =========================================================================

    public void GetOnlyProperty_NoSetterApis()
    {
        var stub = new Stubs.IEntity();

        // Id is get-only
        stub.Id.Get(100);

        IEntity entity = stub;
        var id = entity.Id; // 100

        stub.Id.VerifyGet(Times.Once);

        // These don't exist for get-only:
        // stub.Id.Set(...)     // Compile error
        // stub.Id.VerifySet(...) // Compile error
        // stub.Id.LastSetValue   // Compile error
    }

    // =========================================================================
    // Unconfigured Properties Return Default
    // =========================================================================
    // DESIGN DECISION: Properties without Get configuration return default(T).
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
    // but preserves configuration (Get, Set).
    // =========================================================================

    public void Reset_ClearsTrackingPreservesConfig()
    {
        var stub = new Stubs.IEntity();
        stub.Description.Get("Test");

        IEntity entity = stub;
        _ = entity.Description;
        entity.Description = "Changed";

        stub.Description.Reset();

        // Tracking cleared - call counts now 0
        // But Get("Test") configuration preserved

        var value = entity.Description; // Still returns "Test"
    }
}
