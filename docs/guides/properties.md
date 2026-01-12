# Properties

KnockOff supports all property types: get/set, get-only, and set-only.

## Property Types

### Get/Set Properties

<!-- snippet: properties-get-set-property -->
```cs
public interface IPropUserService
{
    string Name { get; set; }
}

[KnockOff]
public partial class PropUserServiceKnockOff : IPropUserService { }
```
<!-- endSnippet -->

Generated:
- `knockOff.Name.Value` — backing value (read/write)
- `knockOff.Name.GetCount` — number of getter calls
- `knockOff.Name.SetCount` — number of setter calls
- `knockOff.Name.LastSetValue` — last value set
- `knockOff.Name.OnGet` — getter callback (for dynamic values)
- `knockOff.Name.OnSet` — setter callback (for custom logic)

### Get-Only Properties

<!-- snippet: properties-get-only-property -->
```cs
public interface IPropConfig
{
    string ConnectionString { get; }
}

[KnockOff]
public partial class PropConfigKnockOff : IPropConfig { }
```
<!-- endSnippet -->

For get-only properties:
- `Value` is available for setting the return value
- `OnGet` callback is available for dynamic values

**Use `Value` for static values (recommended):**

<!-- snippet: properties-get-only-usage -->
```cs
// Set value directly (recommended for static values)
knockOff.ConnectionString.Value = "Server=test";
```
<!-- endSnippet -->

**Use `OnGet` for dynamic/computed values:**

<!-- snippet: properties-get-only-dynamic -->
```cs
// Use OnGet callback for dynamic/computed values
knockOff.ConnectionString.OnGet = (ko) => Environment.GetEnvironmentVariable("DB_CONN") ?? "Server=fallback";
```
<!-- endSnippet -->

### Set-Only Properties

<!-- snippet: properties-set-only-property-interface -->
```cs
public interface IPropLogger
{
    string Output { set; }
}

[KnockOff]
public partial class PropLoggerKnockOff : IPropLogger { }
```
<!-- endSnippet -->

For set-only properties:
- Only `OnSet` callback and `SetCount`/`LastSetValue` are available
- No backing field (nothing to get)

## Tracking

### Get Tracking

<!-- snippet: properties-get-tracking -->
```cs
_ = service.Name;
_ = service.Name;
_ = service.Name;

var getCount = knockOff.Name.GetCount;  // 3
```
<!-- endSnippet -->

### Set Tracking

<!-- snippet: properties-set-tracking -->
```cs
service.Name = "First";
service.Name = "Second";
service.Name = "Third";

var setCount = knockOff.Name.SetCount;          // 3
var lastValue = knockOff.Name.LastSetValue;     // "Third"
```
<!-- endSnippet -->

## Customization

### Using Value (Recommended for Static Values)

The simplest way to configure a property is using `Value`:

<!-- snippet: properties-value-preset -->
```cs
// Pre-set a property value before test execution
knockOff.Name.Value = "John Doe";

// Now accessing the property returns the pre-set value
var name = service.Name;  // "John Doe"
```
<!-- endSnippet -->

**When to use `Value`:**
- Static test data that doesn't change
- Pre-populating properties before test execution
- Simple return values

### Default Behavior (Via Interface)

You can also set/get through the interface itself:

<!-- snippet: properties-default-behavior -->
```cs
service.Name = "Test";
var value = service.Name;  // "Test" - read from backing
```
<!-- endSnippet -->

### OnGet Callback (For Dynamic Values)

Use `OnGet` when you need dynamic or computed values:

<!-- snippet: properties-onget-callback -->
```cs
knockOff.Name.OnGet = (ko) => "Always This Value";

var value = service.Name;  // "Always This Value"
```
<!-- endSnippet -->

Dynamic values:

<!-- snippet: properties-dynamic-values -->
```cs
var counter = 0;
knockOff.Name.OnGet = (ko) => $"Call-{++counter}";

var first = service.Name;   // "Call-1"
var second = service.Name;  // "Call-2"
```
<!-- endSnippet -->

### OnSet Callback

Override setter behavior:

<!-- snippet: properties-onset-callback -->
```cs
string? captured = null;
knockOff.Name.OnSet = (ko, value) =>
{
    captured = value;
    // Value does NOT go to backing field when OnSet is set
};

service.Name = "Test";
// captured is now "Test"
```
<!-- endSnippet -->

**Important**: When `OnSet` is set, the value is NOT stored in the backing field.

### Conditional Logic

<!-- snippet: properties-conditional-logic -->
```cs
public interface IPropConnection
{
    bool IsConnected { get; }
    void Connect();
}

[KnockOff]
public partial class PropConnectionKnockOff : IPropConnection { }
```
<!-- endSnippet -->

<!-- snippet: properties-conditional-usage -->
```cs
knockOff.IsConnected.OnGet = (ko) =>
{
    // Check other interceptor state
    return ko.Connect.WasCalled;
};
```
<!-- endSnippet -->

## Reset

<!-- snippet: properties-reset -->
```cs
knockOff.Name.Reset();

var getCount = knockOff.Name.GetCount;    // 0
var setCount = knockOff.Name.SetCount;    // 0
var onGet = knockOff.Name.OnGet;          // null
var onSet = knockOff.Name.OnSet;          // null
// Note: Backing field is NOT cleared by Reset
```
<!-- endSnippet -->

## Common Patterns

### Simulating Read-Only Computed Properties

<!-- snippet: properties-computed-property -->
```cs
public interface IPropPerson
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string FullName { get; }
}

[KnockOff]
public partial class PropPersonKnockOff : IPropPerson { }
```
<!-- endSnippet -->

<!-- snippet: properties-computed-usage -->
```cs
// Set up first/last names
person.FirstName = "John";
person.LastName = "Doe";

// Computed property uses backing values
knockOff.FullName.OnGet = (ko) =>
    $"{person.FirstName} {person.LastName}";

var fullName = person.FullName;  // "John Doe"
```
<!-- endSnippet -->

### Tracking Property Changes

<!-- snippet: properties-tracking-changes -->
```cs
public interface IPropStatus
{
    string Status { get; set; }
}

[KnockOff]
public partial class PropStatusKnockOff : IPropStatus { }
```
<!-- endSnippet -->

<!-- snippet: properties-tracking-usage -->
```cs
var changes = new List<string>();
knockOff.Status.OnSet = (ko, value) =>
{
    changes.Add(value);
    // Value still goes to backing when not using OnSet
};
```
<!-- endSnippet -->

### Throwing on Access

<!-- snippet: properties-throwing-on-access -->
```cs
public interface IPropSecure
{
    string SecretKey { get; }
}

[KnockOff]
public partial class PropSecureKnockOff : IPropSecure { }
```
<!-- endSnippet -->

<!-- snippet: properties-throwing-usage -->
```cs
knockOff.SecretKey.OnGet = (ko) =>
    throw new UnauthorizedAccessException("Access denied");
```
<!-- endSnippet -->

## Value vs OnGet: Decision Guide

| Scenario | Use | Example |
|----------|-----|---------|
| Static test data | `Value` | `knockOff.Name.Value = "John"` |
| Pre-populate before test | `Value` | `knockOff.Count.Value = 42` |
| Different value each call | `OnGet` | `knockOff.Id.OnGet = (ko) => ++counter` |
| Depends on other stub state | `OnGet` | `knockOff.IsConnected.OnGet = (ko) => ko.Connect.WasCalled` |
| Computed from test context | `OnGet` | `knockOff.User.OnGet = (ko) => _testFixture.CurrentUser` |
| Throw on access | `OnGet` | `knockOff.Secret.OnGet = (ko) => throw new Exception()` |

**Rule of thumb:** Start with `Value`. Only use `OnGet` when you need dynamic behavior.
