# Indexers

KnockOff supports indexer properties, both get-only and get/set.

## Basic Usage

<!-- snippet: indexers-basic-interface -->
```cs
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
<!-- endSnippet -->

## Naming Convention

Indexer interceptors are named based on whether there are multiple indexers:

**Single indexer:**
<!-- pseudo:indexer-single -->
```csharp
knockOff.Indexer         // for this[string key] or this[int index]
knockOff.Indexer.Backing // Dictionary backing storage
```
<!-- /snippet -->

**Multiple indexers (type suffix for disambiguation):**
<!-- pseudo:indexer-multiple -->
```csharp
knockOff.IndexerString       // for this[string key]
knockOff.IndexerInt32        // for this[int index]
knockOff.IndexerString.Backing
knockOff.IndexerInt32.Backing
```
<!-- /snippet -->

## Tracking

### Get Tracking

<!-- snippet: indexers-get-tracking -->
```cs
_ = store["Name"];
_ = store["Age"];

var getCount = knockOff.Indexer.GetCount;       // 2
var lastKey = knockOff.Indexer.LastGetKey;      // "Age"
```
<!-- endSnippet -->

### Set Tracking

<!-- snippet: indexers-set-tracking -->
```cs
store["Key"] = value1;

var setCount = knockOff.Indexer.SetCount;         // 1
var lastEntry = knockOff.Indexer.LastSetEntry;
var lastSetKey = lastEntry?.Key;                        // "Key"
var lastSetValue = lastEntry?.Value;                    // value1
```
<!-- endSnippet -->

## Backing Dictionary

KnockOff generates a backing dictionary for each indexer:

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

For get/set indexers, setting via the interface stores in the backing dictionary automatically (unless `OnSet` is defined).

## Callbacks

### OnGet Callback

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

### OnSet Callback

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

<!-- snippet: indexers-fallback-to-backing -->
```cs
// No OnGet callback - falls back to backing dictionary
knockOff.Indexer.Backing["Existing"] = new IdxPropertyInfo { Value = "Found" };

var existing = store["Existing"];  // Returns backing value
var missing = store["Missing"];    // Returns null (not in backing)
```
<!-- endSnippet -->

When **`OnGet` callback is set**, the callback completely replaces the getter logic. The backing dictionary is NOT checked automatically.

## Reset

<!-- snippet: indexers-reset -->
```cs
knockOff.Indexer.Reset();

var getCount = knockOff.Indexer.GetCount;    // 0
var onGet = knockOff.Indexer.OnGet;          // null
// Note: Backing dictionary is NOT cleared
```
<!-- endSnippet -->

## Common Patterns

### Entity Property Access (IEntityBase Pattern)

<!-- snippet: indexers-entity-property -->
```cs
public interface IIdxEntityBase
{
    IIdxEntityProperty? this[string propertyName] { get; }
    bool IsNew { get; }
}

[KnockOff]
public partial class IdxEntityBaseKnockOff : IIdxEntityBase { }
```
<!-- endSnippet -->

### Dictionary-Like Behavior

<!-- snippet: indexers-dictionary-like -->
```cs
[KnockOff]
public partial class IdxConfigStoreKnockOff : IIdxConfigStore { }
```
<!-- endSnippet -->

Pre-populate the backing dictionary for dictionary-like behavior.

### Integer Indexers

<!-- snippet: indexers-integer-indexer -->
```cs
public interface IIdxList
{
    object? this[int index] { get; set; }
}

[KnockOff]
public partial class IdxListKnockOff : IIdxList { }
```
<!-- endSnippet -->

<!-- snippet: indexers-integer-indexer-usage -->
```cs
knockOff.Indexer.Backing[0] = "First";
knockOff.Indexer.Backing[1] = "Second";

var first = list[0];   // "First"
var second = list[1];  // "Second"

var lastGetIndex = knockOff.Indexer.LastGetKey;  // 1
```
<!-- endSnippet -->
