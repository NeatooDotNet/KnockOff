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
- `IUserService_NameBacking` — protected backing field (interface-prefixed)
- `IUserService.Name.GetCount` — number of getter calls
- `IUserService.Name.SetCount` — number of setter calls
- `IUserService.Name.LastSetValue` — last value set
- `IUserService.Name.OnGet` — getter callback
- `IUserService.Name.OnSet` — setter callback

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
- Set the backing field directly for default values

```csharp
var knockOff = new PropConfigKnockOff();
knockOff.IPropConfig_ConnectionStringBacking = "Server=localhost";

// Or use callback
knockOff.IPropConfig.ConnectionString.OnGet = (ko) => "Server=test";
```

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

```csharp
IUserService service = knockOff;

_ = service.Name;
_ = service.Name;
_ = service.Name;

Assert.Equal(3, knockOff.IUserService.Name.GetCount);
```

### Set Tracking

```csharp
service.Name = "First";
service.Name = "Second";
service.Name = "Third";

Assert.Equal(3, knockOff.IUserService.Name.SetCount);
Assert.Equal("Third", knockOff.IUserService.Name.LastSetValue);
```

## Customization

### Default Behavior (Backing Field)

Without callbacks, properties use a backing field:

```csharp
var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

service.Name = "Test";
var value = service.Name;

Assert.Equal("Test", value);  // Read from backing
```

### OnGet Callback

Override getter behavior:

```csharp
knockOff.IUserService.Name.OnGet = (ko) => "Always This Value";

var value = service.Name;
Assert.Equal("Always This Value", value);
```

Dynamic values:

```csharp
var counter = 0;
knockOff.IUserService.Name.OnGet = (ko) => $"Call-{++counter}";

Assert.Equal("Call-1", service.Name);
Assert.Equal("Call-2", service.Name);
```

### OnSet Callback

Override setter behavior:

```csharp
string? captured = null;
knockOff.IUserService.Name.OnSet = (ko, value) =>
{
    captured = value;
    // Value does NOT go to backing field when OnSet is set
};

service.Name = "Test";
Assert.Equal("Test", captured);
```

**Important**: When `OnSet` is set, the value is NOT stored in the backing field. The callback completely replaces the setter.

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

```csharp
knockOff.IPropConnection.IsConnected.OnGet = (ko) =>
{
    // Check other handler state
    return ko.IPropConnection.Connect.WasCalled;
};
```

## Reset

```csharp
service.Name = "Value";
_ = service.Name;

knockOff.IUserService.Name.Reset();

Assert.Equal(0, knockOff.IUserService.Name.GetCount);
Assert.Equal(0, knockOff.IUserService.Name.SetCount);
Assert.Null(knockOff.IUserService.Name.OnGet);
Assert.Null(knockOff.IUserService.Name.OnSet);

// Backing field is NOT cleared by Reset
Assert.Equal("Value", knockOff.IUserService_NameBacking);
```

## Common Patterns

### Pre-Populating Values

```csharp
var knockOff = new ConfigKnockOff();
knockOff.IConfig_ConnectionStringBacking = "Server=localhost;Database=test";

IConfig config = knockOff;
Assert.Equal("Server=localhost;Database=test", config.ConnectionString);
```

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

```csharp
knockOff.IPropPerson.FullName.OnGet = (ko) =>
    $"{ko.IPropPerson_FirstNameBacking} {ko.IPropPerson_LastNameBacking}";
```

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

```csharp
var changes = new List<string>();
knockOff.IPropStatus.Status.OnSet = (ko, value) =>
{
    changes.Add(value);
    ko.IPropStatus_StatusBacking = value;  // Still store it
};
```

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

```csharp
knockOff.IPropSecure.SecretKey.OnGet = (ko) =>
    throw new UnauthorizedAccessException("Access denied");
```
