// src/Generator/Model/Shared/UnifiedIndexerContainerModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Unified model for indexer container generation.
/// Used when multiple indexers with different key types exist.
/// Generates a container class with Of{KeyType} properties.
/// </summary>
internal sealed record UnifiedIndexerContainerModel(
    /// <summary>Container class name (e.g., "IndexerContainer").</summary>
    string ContainerClassName,
    /// <summary>Base name for the property (e.g., "Indexer").</summary>
    string BaseName,
    /// <summary>Whether this container needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>All indexers in this container, grouped by key type.</summary>
    EquatableArray<UnifiedIndexerModel> Indexers);

/// <summary>
/// Unified model for a single indexer in the container.
/// </summary>
internal sealed record UnifiedIndexerModel(
    /// <summary>Interceptor class name (e.g., "IndexerInt32Interceptor").</summary>
    string InterceptorClassName,
    /// <summary>Friendly name for the key type (e.g., "Int32", "String").</summary>
    string KeyTypeFriendlyName,
    /// <summary>Full key type (e.g., "int", "string").</summary>
    string KeyType,
    /// <summary>Nullable key type for tracking.</summary>
    string NullableKeyType,
    /// <summary>Key parameter name (e.g., "key", "index").</summary>
    string KeyParamName,
    /// <summary>Return type.</summary>
    string ReturnType,
    /// <summary>Nullable return type for tracking.</summary>
    string NullableReturnType,
    /// <summary>Default expression for getter.</summary>
    string DefaultExpression,
    /// <summary>Whether the indexer has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the indexer has a setter.</summary>
    bool HasSetter,

    // Owner context
    /// <summary>The class name that owns this interceptor (for callback signatures).</summary>
    string OwnerClassName,
    /// <summary>Type parameters on the owner class.</summary>
    string OwnerTypeParameters);
