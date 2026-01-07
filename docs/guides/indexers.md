# Indexers

KnockOff supports indexer properties, both get-only and get/set.

## Basic Usage

<!-- snippet: docs:indexers:basic-interface -->
```csharp
public interface IIdxPropertyStore
{
    IdxPropertyInfo? this[string key] { get; }
}

public interface IIdxReadWriteStore
{
    IdxPropertyInfo? this[string key] { get; set; }
}

[KnockOff]
public partial class IdxPropertyStoreKnockOff : IIdxPropertyStore { }

[KnockOff]
public partial class IdxReadWriteStoreKnockOff : IIdxReadWriteStore { }
```
<!-- /snippet -->

## Naming Convention

Indexer handlers are named based on the key type and accessed via the interface KO property:
- `string` key → `StringIndexer`
- `int` key → `IntIndexer`
- Custom type → `{TypeName}Indexer`

```csharp
knockOff.IPropertyStore.StringIndexer  // for this[string key]
knockOff.IList.IntIndexer              // for this[int index]
```

Backing dictionaries are also interface-prefixed:
```csharp
knockOff.IPropertyStore_StringIndexerBacking  // Dictionary<string, PropertyInfo?>
knockOff.IList_IntIndexerBacking              // Dictionary<int, object?>
```

## Tracking

### Get Tracking

```csharp
var knockOff = new PropertyStoreKnockOff();
IPropertyStore store = knockOff;

// Pre-populate backing dictionary
knockOff.IPropertyStore_StringIndexerBacking["Name"] = new PropertyInfo { Value = "Test" };

_ = store["Name"];
_ = store["Age"];
_ = store["Name"];

Assert.Equal(3, knockOff.IPropertyStore.StringIndexer.GetCount);
Assert.Equal("Name", knockOff.IPropertyStore.StringIndexer.LastGetKey);

// All accessed keys
Assert.Equal(3, knockOff.IPropertyStore.StringIndexer.AllGetKeys.Count);
Assert.Equal("Name", knockOff.IPropertyStore.StringIndexer.AllGetKeys[0]);
Assert.Equal("Age", knockOff.IPropertyStore.StringIndexer.AllGetKeys[1]);
Assert.Equal("Name", knockOff.IPropertyStore.StringIndexer.AllGetKeys[2]);
```

### Set Tracking

```csharp
var knockOff = new ReadWriteStoreKnockOff();
IReadWriteStore store = knockOff;

var prop1 = new PropertyInfo { Name = "First" };
var prop2 = new PropertyInfo { Name = "Second" };

store["key1"] = prop1;
store["key2"] = prop2;

Assert.Equal(2, knockOff.IReadWriteStore.StringIndexer.SetCount);

// Last set entry
var last = knockOff.IReadWriteStore.StringIndexer.LastSetEntry;
Assert.Equal("key2", last?.key);
Assert.Same(prop2, last?.value);

// All set entries
Assert.Equal(2, knockOff.IReadWriteStore.StringIndexer.AllSetEntries.Count);
Assert.Equal("key1", knockOff.IReadWriteStore.StringIndexer.AllSetEntries[0].key);
Assert.Equal("key2", knockOff.IReadWriteStore.StringIndexer.AllSetEntries[1].key);
```

## Backing Dictionary

KnockOff generates a backing dictionary for each indexer (interface-prefixed):

```csharp
// Access directly to pre-populate
knockOff.IPropertyStore_StringIndexerBacking["Name"] = new PropertyInfo { Value = "Test" };
knockOff.IPropertyStore_StringIndexerBacking["Age"] = new PropertyInfo { Value = "25" };

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
Assert.True(knockOff.IReadWriteStore_StringIndexerBacking.ContainsKey("Key"));
```

## Callbacks

### OnGet Callback

```csharp
knockOff.IPropertyStore.StringIndexer.OnGet = (ko, key) => key switch
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

knockOff.IReadWriteStore.StringIndexer.OnSet = (ko, key, value) =>
{
    captured[key] = value;
    // Note: When OnSet is set, value does NOT go to backing dictionary
};

IReadWriteStore store = knockOff;
store["Key"] = new PropertyInfo { Value = "Value" };

Assert.True(captured.ContainsKey("Key"));
Assert.False(knockOff.IReadWriteStore_StringIndexerBacking.ContainsKey("Key"));  // Not in backing
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
knockOff.IPropertyStore.StringIndexer.OnGet = (ko, key) =>
{
    // Custom logic for some keys
    if (key == "Special")
        return new PropertyInfo { Value = "Custom" };

    // Fall back to backing dictionary
    if (ko.IPropertyStore_StringIndexerBacking.TryGetValue(key, out var value))
        return value;

    return null;
};
```

## Reset

```csharp
_ = store["Key1"];
_ = store["Key2"];
store["Key3"] = prop;

knockOff.IPropertyStore.StringIndexer.Reset();

// Tracking cleared
Assert.Equal(0, knockOff.IPropertyStore.StringIndexer.GetCount);
Assert.Equal(0, knockOff.IPropertyStore.StringIndexer.SetCount);
Assert.Empty(knockOff.IPropertyStore.StringIndexer.AllGetKeys);
Assert.Empty(knockOff.IPropertyStore.StringIndexer.AllSetEntries);

// Callbacks cleared
Assert.Null(knockOff.IPropertyStore.StringIndexer.OnGet);
Assert.Null(knockOff.IPropertyStore.StringIndexer.OnSet);

// Backing dictionary is NOT cleared
Assert.True(knockOff.IPropertyStore_StringIndexerBacking.ContainsKey("Key3"));
```

## Common Patterns

### Entity Property Access (IEntityBase Pattern)

<!-- snippet: docs:indexers:entity-property -->
```csharp
public interface IIdxEntityBase
{
    IIdxEntityProperty? this[string propertyName] { get; }
    bool IsNew { get; }
}

[KnockOff]
public partial class IdxEntityBaseKnockOff : IIdxEntityBase { }
```
<!-- /snippet -->

```csharp
// Configure property access
knockOff.IIdxEntityBase.StringIndexer.OnGet = (ko, propertyName) =>
{
    return propertyName switch
    {
        "FirstName" => new IdxEntityProperty { IsModified = true },
        "LastName" => new IdxEntityProperty { IsModified = false },
        "Id" => new IdxEntityProperty { IsModified = ko.IIdxEntityBase.IsNew.OnGet?.Invoke(ko) ?? false },
        _ => null
    };
};
```

### Dictionary-Like Behavior

<!-- snippet: docs:indexers:dictionary-like -->
```csharp
[KnockOff]
public partial class IdxConfigStoreKnockOff : IIdxConfigStore { }
```
<!-- /snippet -->

```csharp
// Pre-populate like a dictionary
knockOff.IIdxConfigStore_StringIndexerBacking["config:timeout"] = new IdxConfigValue { Value = "30" };
knockOff.IIdxConfigStore_StringIndexerBacking["config:retries"] = new IdxConfigValue { Value = "3" };

IIdxConfigStore config = knockOff;
var timeout = config["config:timeout"];
```

### Conditional Access

```csharp
var accessLog = new List<string>();

knockOff.IPropertyStore.StringIndexer.OnGet = (ko, key) =>
{
    accessLog.Add($"Accessed: {key}");

    if (key.StartsWith("secure:"))
        throw new UnauthorizedAccessException($"Cannot access {key}");

    return ko.IPropertyStore_StringIndexerBacking.GetValueOrDefault(key);
};
```

### Integer Indexers

<!-- snippet: docs:indexers:integer-indexer -->
```csharp
public interface IIdxList
{
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class IdxListKnockOff : IIdxList { }
```
<!-- /snippet -->

```csharp
// IIdxList_IntIndexerBacking is Dictionary<int, object?>
knockOff.IIdxList_IntIndexerBacking[0] = "First";
knockOff.IIdxList_IntIndexerBacking[1] = "Second";

IIdxList list = knockOff;
Assert.Equal("First", list[0]);
Assert.Equal(0, knockOff.IIdxList.IntIndexer.LastGetKey);
```
