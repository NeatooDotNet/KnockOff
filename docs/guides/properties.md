# Properties

KnockOff supports all property types: get/set, get-only, and set-only.

## Property Types

### Get/Set Properties

<!-- snippet: docs:properties:get-set-property -->
```csharp
public interface IPropUserService
{
    string Name { get; set; }
}

[KnockOff]
public partial class PropUserServiceKnockOff : IPropUserService { }
```
<!-- /snippet -->

Generated:
- `NameBacking` — protected backing field
- `knockOff.Name.GetCount` — number of getter calls
- `knockOff.Name.SetCount` — number of setter calls
- `knockOff.Name.LastSetValue` — last value set
- `knockOff.Name.OnGet` — getter callback
- `knockOff.Name.OnSet` — setter callback

### Get-Only Properties

<!-- snippet: docs:properties:get-only-property -->
```csharp
public interface IPropConfig
{
    string ConnectionString { get; }
}

[KnockOff]
public partial class PropConfigKnockOff : IPropConfig { }
```
<!-- /snippet -->

For get-only properties:
- Backing field is still generated
- Only `OnGet` callback is available

<!-- snippet: docs:properties:get-only-usage -->
```csharp
// Use callback to provide value
        knockOff.ConnectionString.OnGet = (ko) => "Server=test";
```
<!-- /snippet -->

### Set-Only Properties

```csharp
public interface ILogger
{
    string Output { set; }
}
```

For set-only properties:
- Only `OnSet` callback and `SetCount`/`LastSetValue` are available
- No backing field (nothing to get)

## Tracking

### Get Tracking

<!-- snippet: docs:properties:get-tracking -->
```csharp
_ = service.Name;
        _ = service.Name;
        _ = service.Name;

        var getCount = knockOff.Name.GetCount;  // 3
```
<!-- /snippet -->

### Set Tracking

<!-- snippet: docs:properties:set-tracking -->
```csharp
service.Name = "First";
        service.Name = "Second";
        service.Name = "Third";

        var setCount = knockOff.Name.SetCount;          // 3
        var lastValue = knockOff.Name.LastSetValue;     // "Third"
```
<!-- /snippet -->

## Customization

### Default Behavior (Backing Field)

Without callbacks, properties use a backing field:

<!-- snippet: docs:properties:default-behavior -->
```csharp
service.Name = "Test";
        var value = service.Name;  // "Test" - read from backing
```
<!-- /snippet -->

### OnGet Callback

Override getter behavior:

<!-- snippet: docs:properties:onget-callback -->
```csharp
knockOff.Name.OnGet = (ko) => "Always This Value";

        var value = service.Name;  // "Always This Value"
```
<!-- /snippet -->

Dynamic values:

<!-- snippet: docs:properties:dynamic-values -->
```csharp
var counter = 0;
        knockOff.Name.OnGet = (ko) => $"Call-{++counter}";

        var first = service.Name;   // "Call-1"
        var second = service.Name;  // "Call-2"
```
<!-- /snippet -->

### OnSet Callback

Override setter behavior:

<!-- snippet: docs:properties:onset-callback -->
```csharp
string? captured = null;
        knockOff.Name.OnSet = (ko, value) =>
        {
            captured = value;
            // Value does NOT go to backing field when OnSet is set
        };

        service.Name = "Test";
        // captured is now "Test"
```
<!-- /snippet -->

**Important**: When `OnSet` is set, the value is NOT stored in the backing field.

### Conditional Logic

<!-- snippet: docs:properties:conditional-logic -->
```csharp
public interface IPropConnection
{
    bool IsConnected { get; }
    void Connect();
}

[KnockOff]
public partial class PropConnectionKnockOff : IPropConnection { }
```
<!-- /snippet -->

<!-- snippet: docs:properties:conditional-usage -->
```csharp
knockOff.IsConnected.OnGet = (ko) =>
        {
            // Check other interceptor state
            return ko.Connect.WasCalled;
        };
```
<!-- /snippet -->

## Reset

<!-- snippet: docs:properties:reset -->
```csharp
knockOff.Name.Reset();

        var getCount = knockOff.Name.GetCount;    // 0
        var setCount = knockOff.Name.SetCount;    // 0
        var onGet = knockOff.Name.OnGet;          // null
        var onSet = knockOff.Name.OnSet;          // null
        // Note: Backing field is NOT cleared by Reset
```
<!-- /snippet -->

## Common Patterns

### Simulating Read-Only Computed Properties

<!-- snippet: docs:properties:computed-property -->
```csharp
public interface IPropPerson
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string FullName { get; }
}

[KnockOff]
public partial class PropPersonKnockOff : IPropPerson { }
```
<!-- /snippet -->

<!-- snippet: docs:properties:computed-usage -->
```csharp
// Set up first/last names
        person.FirstName = "John";
        person.LastName = "Doe";

        // Computed property uses backing values
        knockOff.FullName.OnGet = (ko) =>
            $"{person.FirstName} {person.LastName}";

        var fullName = person.FullName;  // "John Doe"
```
<!-- /snippet -->

### Tracking Property Changes

<!-- snippet: docs:properties:tracking-changes -->
```csharp
public interface IPropStatus
{
    string Status { get; set; }
}

[KnockOff]
public partial class PropStatusKnockOff : IPropStatus { }
```
<!-- /snippet -->

<!-- snippet: docs:properties:tracking-usage -->
```csharp
var changes = new List<string>();
        knockOff.Status.OnSet = (ko, value) =>
        {
            changes.Add(value);
            // Value still goes to backing when not using OnSet
        };
```
<!-- /snippet -->

### Throwing on Access

<!-- snippet: docs:properties:throwing-on-access -->
```csharp
public interface IPropSecure
{
    string SecretKey { get; }
}

[KnockOff]
public partial class PropSecureKnockOff : IPropSecure { }
```
<!-- /snippet -->

<!-- snippet: docs:properties:throwing-usage -->
```csharp
knockOff.SecretKey.OnGet = (ko) =>
            throw new UnauthorizedAccessException("Access denied");
```
<!-- /snippet -->
