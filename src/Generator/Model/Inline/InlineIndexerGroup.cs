// src/Generator/Model/Inline/InlineIndexerGroup.cs
#nullable enable

namespace KnockOff.Model.Inline;

/// <summary>
/// Groups multiple indexers that share the same base name but have different key types.
/// Used for generating container classes with Of{KeyType} properties.
/// </summary>
internal sealed record InlineIndexerGroup(
    /// <summary>Base name for the group (e.g., "Indexer").</summary>
    string BaseName,
    /// <summary>Name of the container class (e.g., "IndexerContainer").</summary>
    string ContainerClassName,
    /// <summary>Whether this group needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>All indexers in this group.</summary>
    EquatableArray<InlineIndexerModel> Indexers);
