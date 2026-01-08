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

Indexer interceptors are named based on the key type:
- `string` key → `StringIndexer`
- `int` key → `Int32Indexer`
- Custom type → `{TypeName}Indexer`

```csharp
knockOff.StringIndexer       // for this[string key]
knockOff.Int32Indexer        // for this[int index]
```

Backing dictionaries:
```csharp
knockOff.StringIndexerBacking  // Dictionary<string, PropertyInfo?>
knockOff.Int32IndexerBacking   // Dictionary<int, object?>
```

## Tracking

### Get Tracking

<!-- snippet: docs:indexers:get-tracking -->
```csharp
_ = store["Name"];
        _ = store["Age"];

        var getCount = knockOff.StringIndexer.GetCount;       // 2
        var lastKey = knockOff.StringIndexer.LastGetKey;      // "Age"
```
<!-- /snippet -->

### Set Tracking

<!-- snippet: docs:indexers:set-tracking -->
```csharp
store["Key"] = value1;

        var setCount = knockOff.StringIndexer.SetCount;         // 1
        var lastEntry = knockOff.StringIndexer.LastSetEntry;
        var lastSetKey = lastEntry?.Key;                        // "Key"
        var lastSetValue = lastEntry?.Value;                    // value1
```
<!-- /snippet -->

## Backing Dictionary

KnockOff generates a backing dictionary for each indexer:

<!-- snippet: docs:indexers:backing-dictionary -->
```csharp
// Pre-populate backing dictionary
        knockOff.StringIndexerBacking["Config"] = new IdxPropertyInfo { Value = "Value1" };
        knockOff.StringIndexerBacking["Setting"] = new IdxPropertyInfo { Value = "Value2" };

        // Access returns backing values
        var config = store["Config"];   // Returns the pre-populated value
        var setting = store["Setting"]; // Returns the pre-populated value
```
<!-- /snippet -->

For get/set indexers, setting via the interface stores in the backing dictionary automatically (unless `OnSet` is defined).

## Callbacks

### OnGet Callback

<!-- snippet: docs:indexers:onget-callback -->
```csharp
knockOff.StringIndexer.OnGet = (ko, key) =>
        {
            // Compute or fetch value dynamically
            return new IdxPropertyInfo { Name = key, Value = key.Length };
        };

        var result = store["Hello"];  // Returns IdxPropertyInfo with Value = 5
```
<!-- /snippet -->

### OnSet Callback

<!-- snippet: docs:indexers:onset-callback -->
```csharp
knockOff.StringIndexer.OnSet = (ko, key, value) =>
        {
            changes.Add((key, value));
        };

        store["Key1"] = new IdxPropertyInfo { Value = "A" };
        store["Key2"] = new IdxPropertyInfo { Value = "B" };

        // changes contains [("Key1", ...), ("Key2", ...)]
```
<!-- /snippet -->

Note: When `OnSet` is set, values do NOT go to the backing dictionary.

### Callback Signatures

| Accessor | Callback Signature |
|----------|-------------------|
| Getter | `Func<TKnockOff, TKey, TValue>` |
| Setter | `Action<TKnockOff, TKey, TValue>` |

## Getter Behavior

When **no `OnGet` callback** is set:
1. Backing dictionary is checked (returns value if key exists)
2. Returns `default(T)` if key not found

<!-- snippet: docs:indexers:fallback-to-backing -->
```csharp
// No OnGet callback - falls back to backing dictionary
        knockOff.StringIndexerBacking["Existing"] = new IdxPropertyInfo { Value = "Found" };

        var existing = store["Existing"];  // Returns backing value
        var missing = store["Missing"];    // Returns null (not in backing)
```
<!-- /snippet -->

When **`OnGet` callback is set**, the callback completely replaces the getter logic. The backing dictionary is NOT checked automatically.

## Reset

<!-- snippet: docs:indexers:reset -->
```csharp
knockOff.StringIndexer.Reset();

        var getCount = knockOff.StringIndexer.GetCount;    // 0
        var onGet = knockOff.StringIndexer.OnGet;          // null
        // Note: Backing dictionary is NOT cleared
```
<!-- /snippet -->

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

### Dictionary-Like Behavior

<!-- snippet: docs:indexers:dictionary-like -->
```csharp
[KnockOff]
public partial class IdxConfigStoreKnockOff : IIdxConfigStore { }
```
<!-- /snippet -->

Pre-populate the backing dictionary for dictionary-like behavior.

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

<!-- snippet: docs:indexers:integer-indexer-usage -->
```csharp
knockOff.Int32IndexerBacking[0] = "First";
        knockOff.Int32IndexerBacking[1] = "Second";

        var first = list[0];   // "First"
        var second = list[1];  // "Second"

        var lastGetIndex = knockOff.Int32Indexer.LastGetKey;  // 1
```
<!-- /snippet -->
