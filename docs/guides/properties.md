# Properties and Indexers

This guide covers stubbing properties (get/set, get-only, set-only, init, required) and indexers.

## Properties

### Basic Configuration

Use `Value` for simple property values:

<!-- snippet: properties-value-preset -->
```cs
// Pre-set a property value before test execution
knockOff.Name.Value = "John Doe";

// Now accessing the property returns the pre-set value
var name = service.Name;  // "John Doe"
```
<!-- endSnippet -->

Use `OnGet` for dynamic values:

<!-- snippet: properties-onget-callback -->
```cs
knockOff.Name.OnGet = (ko) => "Always This Value";

var value = service.Name;  // "Always This Value"
```
<!-- endSnippet -->

### Property Types

**Get/Set properties** have full tracking:

```csharp
stub.Name.Value = "Test";       // Set return value
stub.Name.OnGet = (ko) => val;  // Dynamic getter
stub.Name.OnSet = (ko, v) => {};// Setter callback
stub.Name.GetCount              // Getter call count
stub.Name.SetCount              // Setter call count
stub.Name.LastSetValue          // Last value set
```

**Get-only properties** can use `Value` or `OnGet`:

<!-- snippet: properties-get-only-usage -->
```cs
// Set value directly (recommended for static values)
knockOff.ConnectionString.Value = "Server=test";
```
<!-- endSnippet -->

**Set-only properties** track setter calls:

```csharp
stub.Output.SetCount      // Number of sets
stub.Output.LastSetValue  // Last value set
stub.Output.OnSet = (ko, value) => { };
```

### Tracking

<!-- snippet: properties-get-tracking -->
```cs
_ = service.Name;
_ = service.Name;
_ = service.Name;

var getCount = knockOff.Name.GetCount;  // 3
```
<!-- endSnippet -->

<!-- snippet: properties-set-tracking -->
```cs
service.Name = "First";
service.Name = "Second";
service.Name = "Third";

var setCount = knockOff.Name.SetCount;          // 3
var lastValue = knockOff.Name.LastSetValue;     // "Third"
```
<!-- endSnippet -->

### Init Properties (C# 9+)

Init-only properties are configured via `Value`:

```csharp
public interface IEntity
{
    string Id { get; init; }
}

stub.Id.Value = "entity-123";
var id = entity.Id;  // "entity-123"
```

### Required Properties (C# 11+)

Required properties work like regular properties. The `[SetsRequiredMembers]` attribute is auto-generated:

```csharp
public class AuditableEntity
{
    public required string Id { get; set; }
    public required string CreatedBy { get; set; }
}

// [KnockOff<AuditableEntity>]
stub.Id.OnGet = (ko) => "audit-001";
stub.CreatedBy.OnGet = (ko) => "admin";
```

### Conditional Logic

<!-- snippet: properties-conditional-usage -->
```cs
knockOff.IsConnected.OnGet = (ko) =>
{
    // Check other interceptor state
    return ko.Connect.WasCalled;
};
```
<!-- endSnippet -->

### Reset

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

## Indexers

Indexers use a backing dictionary for storage.

### Basic Usage

<!-- snippet: indexers-backing-dictionary -->
```cs
// Pre-populate backing dictionary
knockOff.Indexer.Backing["Config"] = new IdxPropertyInfo { Value = "Value1" };
knockOff.Indexer.Backing["Setting"] = new IdxPropertyInfo { Value = "Value2" };

// Access returns backing values
var config = store["Config"];   // Returns the pre-populated value
var setting = store["Setting"]; // Returns the pre-populated value
```
<!-- endSnippet -->

### Naming

- Single indexer: `stub.Indexer`
- Multiple indexers: `stub.IndexerString`, `stub.IndexerInt32` (key type suffix)

### Tracking

<!-- snippet: indexers-get-tracking -->
```cs
_ = store["Name"];
_ = store["Age"];

var getCount = knockOff.Indexer.GetCount;       // 2
var lastKey = knockOff.Indexer.LastGetKey;      // "Age"
```
<!-- endSnippet -->

<!-- snippet: indexers-set-tracking -->
```cs
store["Key"] = value1;

var setCount = knockOff.Indexer.SetCount;         // 1
var lastEntry = knockOff.Indexer.LastSetEntry;
var lastSetKey = lastEntry?.Key;                        // "Key"
var lastSetValue = lastEntry?.Value;                    // value1
```
<!-- endSnippet -->

### Callbacks

<!-- snippet: indexers-onget-callback -->
```cs
knockOff.Indexer.OnGet = (ko, key) =>
{
    // Compute or fetch value dynamically
    return new IdxPropertyInfo { Name = key, Value = key.Length };
};

var result = store["Hello"];  // Returns IdxPropertyInfo with Value = 5
```
<!-- endSnippet -->

<!-- snippet: indexers-onset-callback -->
```cs
knockOff.Indexer.OnSet = (ko, key, value) =>
{
    changes.Add((key, value));
};

store["Key1"] = new IdxPropertyInfo { Value = "A" };
store["Key2"] = new IdxPropertyInfo { Value = "B" };

// changes contains [("Key1", ...), ("Key2", ...)]
```
<!-- endSnippet -->

**Note:** When `OnSet` is set, values do NOT go to the backing dictionary.

### Integer Indexers

<!-- snippet: indexers-integer-indexer-usage -->
```cs
knockOff.Indexer.Backing[0] = "First";
knockOff.Indexer.Backing[1] = "Second";

var first = list[0];   // "First"
var second = list[1];  // "Second"

var lastGetIndex = knockOff.Indexer.LastGetKey;  // 1
```
<!-- endSnippet -->

## Value vs OnGet

| Scenario | Use |
|----------|-----|
| Static test data | `Value` |
| Different value each call | `OnGet` |
| Depends on stub state | `OnGet` |
| Throw on access | `OnGet` |

**Rule of thumb:** Start with `Value`. Only use `OnGet` when you need dynamic behavior.
