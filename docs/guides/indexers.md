# Indexers

KnockOff supports indexer properties, both get-only and get/set.

## Basic Usage

```csharp
public interface IPropertyStore
{
    PropertyInfo? this[string key] { get; }
}

public interface IReadWriteStore
{
    PropertyInfo? this[string key] { get; set; }
}

[KnockOff]
public partial class PropertyStoreKnockOff : IPropertyStore { }

[KnockOff]
public partial class ReadWriteStoreKnockOff : IReadWriteStore { }
```

## Naming Convention

Indexer handlers are named based on the key type:
- `string` key → `StringIndexer`
- `int` key → `IntIndexer`
- Custom type → `{TypeName}Indexer`

```csharp
knockOff.Spy.StringIndexer  // for this[string key]
knockOff.Spy.IntIndexer     // for this[int index]
```

## Tracking

### Get Tracking

```csharp
var knockOff = new PropertyStoreKnockOff();
IPropertyStore store = knockOff;

// Pre-populate backing dictionary
knockOff.StringIndexerBacking["Name"] = new PropertyInfo { Value = "Test" };

_ = store["Name"];
_ = store["Age"];
_ = store["Name"];

Assert.Equal(3, knockOff.Spy.StringIndexer.GetCount);
Assert.Equal("Name", knockOff.Spy.StringIndexer.LastGetKey);

// All accessed keys
Assert.Equal(3, knockOff.Spy.StringIndexer.AllGetKeys.Count);
Assert.Equal("Name", knockOff.Spy.StringIndexer.AllGetKeys[0]);
Assert.Equal("Age", knockOff.Spy.StringIndexer.AllGetKeys[1]);
Assert.Equal("Name", knockOff.Spy.StringIndexer.AllGetKeys[2]);
```

### Set Tracking

```csharp
var knockOff = new ReadWriteStoreKnockOff();
IReadWriteStore store = knockOff;

var prop1 = new PropertyInfo { Name = "First" };
var prop2 = new PropertyInfo { Name = "Second" };

store["key1"] = prop1;
store["key2"] = prop2;

Assert.Equal(2, knockOff.Spy.StringIndexer.SetCount);

// Last set entry
var last = knockOff.Spy.StringIndexer.LastSetEntry;
Assert.Equal("key2", last?.key);
Assert.Same(prop2, last?.value);

// All set entries
Assert.Equal(2, knockOff.Spy.StringIndexer.AllSetEntries.Count);
Assert.Equal("key1", knockOff.Spy.StringIndexer.AllSetEntries[0].key);
Assert.Equal("key2", knockOff.Spy.StringIndexer.AllSetEntries[1].key);
```

## Backing Dictionary

KnockOff generates a backing dictionary for each indexer:

```csharp
// Access directly to pre-populate
knockOff.StringIndexerBacking["Name"] = new PropertyInfo { Value = "Test" };
knockOff.StringIndexerBacking["Age"] = new PropertyInfo { Value = "25" };

// Values are returned when accessed via interface
IPropertyStore store = knockOff;
var name = store["Name"];
Assert.Equal("Test", name?.Value);
```

For get/set indexers, setting via the interface stores in the backing:

```csharp
IReadWriteStore store = knockOff;
store["Key"] = new PropertyInfo { Value = "Value" };

// Verify it's in backing
Assert.True(knockOff.StringIndexerBacking.ContainsKey("Key"));
```

## Callbacks

### OnGet Callback

```csharp
knockOff.Spy.StringIndexer.OnGet = (ko, key) => key switch
{
    "Name" => new PropertyInfo { Value = "Test" },
    "Age" => new PropertyInfo { Value = "25" },
    _ => null
};

IPropertyStore store = knockOff;
var name = store["Name"];
Assert.Equal("Test", name?.Value);

var unknown = store["Unknown"];
Assert.Null(unknown);
```

### OnSet Callback

```csharp
var captured = new Dictionary<string, PropertyInfo?>();

knockOff.Spy.StringIndexer.OnSet = (ko, key, value) =>
{
    captured[key] = value;
    // Note: When OnSet is set, value does NOT go to backing dictionary
};

IReadWriteStore store = knockOff;
store["Key"] = new PropertyInfo { Value = "Value" };

Assert.True(captured.ContainsKey("Key"));
Assert.False(knockOff.StringIndexerBacking.ContainsKey("Key"));  // Not in backing
```

### Callback Signatures

| Accessor | Callback Signature |
|----------|-------------------|
| Getter | `Func<TKnockOff, TKey, TValue>` |
| Setter | `Action<TKnockOff, TKey, TValue>` |

## Getter Behavior

When **no `OnGet` callback** is set:
1. Backing dictionary is checked (returns value if key exists)
2. Returns `default(T)` if key not found

When **`OnGet` callback is set**:
- The callback **completely replaces** the getter logic
- Backing dictionary is NOT checked automatically
- You must handle all cases in your callback

To combine callback with backing:

```csharp
knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
{
    // Custom logic for some keys
    if (key == "Special")
        return new PropertyInfo { Value = "Custom" };

    // Fall back to backing dictionary
    if (ko.StringIndexerBacking.TryGetValue(key, out var value))
        return value;

    return null;
};
```

## Reset

```csharp
_ = store["Key1"];
_ = store["Key2"];
store["Key3"] = prop;

knockOff.Spy.StringIndexer.Reset();

// Tracking cleared
Assert.Equal(0, knockOff.Spy.StringIndexer.GetCount);
Assert.Equal(0, knockOff.Spy.StringIndexer.SetCount);
Assert.Empty(knockOff.Spy.StringIndexer.AllGetKeys);
Assert.Empty(knockOff.Spy.StringIndexer.AllSetEntries);

// Callbacks cleared
Assert.Null(knockOff.Spy.StringIndexer.OnGet);
Assert.Null(knockOff.Spy.StringIndexer.OnSet);

// Backing dictionary is NOT cleared
Assert.True(knockOff.StringIndexerBacking.ContainsKey("Key3"));
```

## Common Patterns

### Entity Property Access (IEntityBase Pattern)

```csharp
public interface IEntityBase
{
    IEntityProperty? this[string propertyName] { get; }
    bool IsNew { get; }
}

[KnockOff]
public partial class EntityBaseKnockOff : IEntityBase { }

// Configure property access
knockOff.Spy.StringIndexer.OnGet = (ko, propertyName) =>
{
    return propertyName switch
    {
        "FirstName" => new EntityProperty { IsModified = true },
        "LastName" => new EntityProperty { IsModified = false },
        "Id" => new EntityProperty { IsModified = ko.Spy.IsNew.OnGet?.Invoke(ko) ?? false },
        _ => null
    };
};
```

### Dictionary-Like Behavior

```csharp
// Pre-populate like a dictionary
knockOff.StringIndexerBacking["config:timeout"] = new ConfigValue { Value = "30" };
knockOff.StringIndexerBacking["config:retries"] = new ConfigValue { Value = "3" };

IConfigStore config = knockOff;
var timeout = config["config:timeout"];
```

### Conditional Access

```csharp
var accessLog = new List<string>();

knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
{
    accessLog.Add($"Accessed: {key}");

    if (key.StartsWith("secure:"))
        throw new UnauthorizedAccessException($"Cannot access {key}");

    return ko.StringIndexerBacking.GetValueOrDefault(key);
};
```

### Integer Indexers

```csharp
public interface IList
{
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class ListKnockOff : IList { }

// IntIndexerBacking is Dictionary<int, object?>
knockOff.IntIndexerBacking[0] = "First";
knockOff.IntIndexerBacking[1] = "Second";

IList list = knockOff;
Assert.Equal("First", list[0]);
Assert.Equal(0, knockOff.Spy.IntIndexer.LastGetKey);
```
