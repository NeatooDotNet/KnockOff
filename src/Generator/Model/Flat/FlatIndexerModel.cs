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
    /// <summary>Ref kind prefix for the key parameter (e.g., "in " for in parameters).</summary>
    string KeyRefPrefix,
    string NullableKeyType,
    bool HasGetter,
    bool HasSetter,
    string SimpleInterfaceName,
    bool NeedsNewKeyword,
    /// <summary>Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern.</summary>
    string KeyTypeFriendlyName,
    /// <summary>Base name for indexer grouping (e.g., "Indexer").</summary>
    string BaseName,

    // Ref return support
    /// <summary>True if the indexer returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the indexer returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false)
{
    /// <summary>True if the indexer returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
