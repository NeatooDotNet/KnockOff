/// <summary>
/// Code samples for docs/guides/properties.md
///
/// Snippets in this file:
/// - docs:properties:get-set-property
/// - docs:properties:get-only-property
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

#region docs:properties:get-set-property
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

#region docs:properties:get-only-property
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

#region docs:properties:conditional-logic
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

#region docs:properties:computed-property
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

#region docs:properties:tracking-changes
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

#region docs:properties:throwing-on-access
public interface IPropSecure
{
    string SecretKey { get; }
}

[KnockOff]
public partial class PropSecureKnockOff : IPropSecure { }
#endregion
