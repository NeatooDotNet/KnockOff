/// <summary>
/// Code samples for docs/guides/properties.md
///
/// Snippets in this file:
/// - docs:properties:get-set-property
/// - docs:properties:get-only-property
/// - docs:properties:get-only-usage (Value - recommended)
/// - docs:properties:get-only-dynamic (OnGet - for dynamic values)
/// - docs:properties:value-preset
/// - docs:properties:conditional-logic
/// - docs:properties:computed-property
/// - docs:properties:tracking-changes
/// - docs:properties:throwing-on-access
///
/// Corresponding tests: PropertiesSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Get/Set Properties
// ============================================================================

#region properties-get-set-property
public interface IPropUserService
{
    string Name { get; set; }
}

[KnockOff]
public partial class PropUserServiceKnockOff : IPropUserService { }
#endregion

// ============================================================================
// Get-Only Properties
// ============================================================================

#region properties-get-only-property
public interface IPropConfig
{
    string ConnectionString { get; }
}

[KnockOff]
public partial class PropConfigKnockOff : IPropConfig { }
#endregion

// ============================================================================
// Conditional Logic (with Connect method)
// ============================================================================

#region properties-conditional-logic
public interface IPropConnection
{
    bool IsConnected { get; }
    void Connect();
}

[KnockOff]
public partial class PropConnectionKnockOff : IPropConnection { }
#endregion

// ============================================================================
// Computed Property
// ============================================================================

#region properties-computed-property
public interface IPropPerson
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string FullName { get; }
}

[KnockOff]
public partial class PropPersonKnockOff : IPropPerson { }
#endregion

// ============================================================================
// Status Tracking
// ============================================================================

#region properties-tracking-changes
public interface IPropStatus
{
    string Status { get; set; }
}

[KnockOff]
public partial class PropStatusKnockOff : IPropStatus { }
#endregion

// ============================================================================
// Throwing on Access
// ============================================================================

#region properties-throwing-on-access
public interface IPropSecure
{
    string SecretKey { get; }
}

[KnockOff]
public partial class PropSecureKnockOff : IPropSecure { }
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating property patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class PropertiesUsageExamples
{
    public static void GetOnlyUsage()
    {
        var knockOff = new PropConfigKnockOff();

        #region properties-get-only-usage
        // Set value directly (recommended for static values)
        knockOff.ConnectionString.Value = "Server=test";
        #endregion
    }

    public static void GetOnlyDynamicUsage()
    {
        var knockOff = new PropConfigKnockOff();

        #region properties-get-only-dynamic
        // Use OnGet callback for dynamic/computed values
        knockOff.ConnectionString.OnGet = (ko) => Environment.GetEnvironmentVariable("DB_CONN") ?? "Server=fallback";
        #endregion
    }

    public static void ValuePreset()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-value-preset
        // Pre-set a property value before test execution
        knockOff.Name.Value = "John Doe";

        // Now accessing the property returns the pre-set value
        var name = service.Name;  // "John Doe"
        #endregion

        _ = name;
    }

    public static void GetTracking()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-get-tracking
        _ = service.Name;
        _ = service.Name;
        _ = service.Name;

        var getCount = knockOff.Name.GetCount;  // 3
        #endregion

        _ = getCount;
    }

    public static void SetTracking()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-set-tracking
        service.Name = "First";
        service.Name = "Second";
        service.Name = "Third";

        var setCount = knockOff.Name.SetCount;          // 3
        var lastValue = knockOff.Name.LastSetValue;     // "Third"
        #endregion

        _ = (setCount, lastValue);
    }

    public static void DefaultBehavior()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-default-behavior
        service.Name = "Test";
        var value = service.Name;  // "Test" - read from backing
        #endregion

        _ = value;
    }

    public static void OnGetCallback()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-onget-callback
        knockOff.Name.OnGet = (ko) => "Always This Value";

        var value = service.Name;  // "Always This Value"
        #endregion

        _ = value;
    }

    public static void DynamicValues()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-dynamic-values
        var counter = 0;
        knockOff.Name.OnGet = (ko) => $"Call-{++counter}";

        var first = service.Name;   // "Call-1"
        var second = service.Name;  // "Call-2"
        #endregion

        _ = (first, second);
    }

    public static void OnSetCallback()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        #region properties-onset-callback
        string? captured = null;
        knockOff.Name.OnSet = (ko, value) =>
        {
            captured = value;
            // Value does NOT go to backing field when OnSet is set
        };

        service.Name = "Test";
        // captured is now "Test"
        #endregion

        _ = captured;
    }

    public static void ConditionalUsage()
    {
        var knockOff = new PropConnectionKnockOff();

        #region properties-conditional-usage
        knockOff.IsConnected.OnGet = (ko) =>
        {
            // Check other interceptor state
            return ko.Connect.WasCalled;
        };
        #endregion
    }

    public static void ResetUsage()
    {
        var knockOff = new PropUserServiceKnockOff();
        IPropUserService service = knockOff;

        service.Name = "Value";
        _ = service.Name;

        #region properties-reset
        knockOff.Name.Reset();

        var getCount = knockOff.Name.GetCount;    // 0
        var setCount = knockOff.Name.SetCount;    // 0
        var onGet = knockOff.Name.OnGet;          // null
        var onSet = knockOff.Name.OnSet;          // null
        // Note: Backing field is NOT cleared by Reset
        #endregion

        _ = (getCount, setCount, onGet, onSet);
    }

    public static void ComputedUsage()
    {
        var knockOff = new PropPersonKnockOff();
        IPropPerson person = knockOff;

        #region properties-computed-usage
        // Set up first/last names
        person.FirstName = "John";
        person.LastName = "Doe";

        // Computed property uses backing values
        knockOff.FullName.OnGet = (ko) =>
            $"{person.FirstName} {person.LastName}";

        var fullName = person.FullName;  // "John Doe"
        #endregion

        _ = fullName;
    }

    public static void TrackingChangesUsage()
    {
        var knockOff = new PropStatusKnockOff();

        #region properties-tracking-usage
        var changes = new List<string>();
        knockOff.Status.OnSet = (ko, value) =>
        {
            changes.Add(value);
            // Value still goes to backing when not using OnSet
        };
        #endregion

        _ = changes;
    }

    public static void ThrowingUsage()
    {
        var knockOff = new PropSecureKnockOff();

        #region properties-throwing-usage
        knockOff.SecretKey.OnGet = (ko) =>
            throw new UnauthorizedAccessException("Access denied");
        #endregion
    }
}
