// src/Generator/Model/Flat/FlatIndexerGroup.cs
#nullable enable
using KnockOff;

namespace KnockOff.Model.Flat;

/// <summary>
/// Groups multiple indexers that share the same base name but have different key types.
/// Used for generating container classes with Of{KeyType} properties.
/// </summary>
internal sealed record FlatIndexerGroup(
    /// <summary>Base name for the group (e.g., "Indexer").</summary>
    string BaseName,
    /// <summary>Name of the container class (e.g., "IndexerContainer").</summary>
    string ContainerClassName,
    /// <summary>Whether this group needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>All indexers in this group.</summary>
    EquatableArray<FlatIndexerModel> Indexers);
