// src/Generator/Model/Flat/FlatIndexerModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for an indexer in flat API generation.
/// </summary>
internal sealed record FlatIndexerModel(
    string InterceptorName,
    string InterceptorClassName,
    string DeclaringInterface,
    string ReturnType,
    string NullableReturnType,
    string DefaultExpression,
    string KeyType,
    string KeyParamName,
    string NullableKeyType,
    bool HasGetter,
    bool HasSetter,
    string SimpleInterfaceName,
    bool NeedsNewKeyword,
    /// <summary>Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern.</summary>
    string KeyTypeFriendlyName,
    /// <summary>Base name for indexer grouping (e.g., "Indexer").</summary>
    string BaseName);
