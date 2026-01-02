# Properties

KnockOff supports all property types: get/set, get-only, and set-only.

## Property Types

### Get/Set Properties

```csharp
public interface IUserService
{
    string Name { get; set; }
}

[KnockOff]
public partial class UserServiceKnockOff : IUserService { }
```

Generated:
- `NameBacking` — protected backing field
- `Spy.Name.GetCount` — number of getter calls
- `Spy.Name.SetCount` — number of setter calls
- `Spy.Name.LastSetValue` — last value set
- `Spy.Name.OnGet` — getter callback
- `Spy.Name.OnSet` — setter callback

### Get-Only Properties

```csharp
public interface IConfig
{
    string ConnectionString { get; }
}
```

For get-only properties:
- Backing field is still generated
- Only `OnGet` callback is available
- Set the backing field directly for default values

```csharp
var knockOff = new ConfigKnockOff();
knockOff.ConnectionStringBacking = "Server=localhost";

// Or use callback
knockOff.Spy.ConnectionString.OnGet = (ko) => "Server=test";
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

Assert.Equal(3, knockOff.Spy.Name.GetCount);
```

### Set Tracking

```csharp
service.Name = "First";
service.Name = "Second";
service.Name = "Third";

Assert.Equal(3, knockOff.Spy.Name.SetCount);
Assert.Equal("Third", knockOff.Spy.Name.LastSetValue);
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
knockOff.Spy.Name.OnGet = (ko) => "Always This Value";

var value = service.Name;
Assert.Equal("Always This Value", value);
```

Dynamic values:

```csharp
var counter = 0;
knockOff.Spy.Name.OnGet = (ko) => $"Call-{++counter}";

Assert.Equal("Call-1", service.Name);
Assert.Equal("Call-2", service.Name);
```

### OnSet Callback

Override setter behavior:

```csharp
string? captured = null;
knockOff.Spy.Name.OnSet = (ko, value) =>
{
    captured = value;
    // Value does NOT go to backing field when OnSet is set
};

service.Name = "Test";
Assert.Equal("Test", captured);
```

**Important**: When `OnSet` is set, the value is NOT stored in the backing field. The callback completely replaces the setter.

### Conditional Logic

```csharp
knockOff.Spy.IsConnected.OnGet = (ko) =>
{
    // Check other Spy state
    return ko.Spy.Connect.WasCalled;
};
```

## Reset

```csharp
service.Name = "Value";
_ = service.Name;

knockOff.Spy.Name.Reset();

Assert.Equal(0, knockOff.Spy.Name.GetCount);
Assert.Equal(0, knockOff.Spy.Name.SetCount);
Assert.Null(knockOff.Spy.Name.OnGet);
Assert.Null(knockOff.Spy.Name.OnSet);

// Backing field is NOT cleared by Reset
Assert.Equal("Value", knockOff.NameBacking);
```

## Common Patterns

### Pre-Populating Values

```csharp
var knockOff = new ConfigKnockOff();
knockOff.ConnectionStringBacking = "Server=localhost;Database=test";

IConfig config = knockOff;
Assert.Equal("Server=localhost;Database=test", config.ConnectionString);
```

### Simulating Read-Only Computed Properties

```csharp
knockOff.Spy.FullName.OnGet = (ko) =>
    $"{ko.FirstNameBacking} {ko.LastNameBacking}";
```

### Tracking Property Changes

```csharp
var changes = new List<string>();
knockOff.Spy.Status.OnSet = (ko, value) =>
{
    changes.Add(value);
    ko.StatusBacking = value;  // Still store it
};
```

### Throwing on Access

```csharp
knockOff.Spy.SecretKey.OnGet = (ko) =>
    throw new UnauthorizedAccessException("Access denied");
```
