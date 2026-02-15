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
    /// <summary>The default value strategy for the indexer value type.</summary>
    DefaultValueStrategy DefaultStrategy,
    /// <summary>Concrete type when DefaultStrategy is NewInstance and value type is an interface.</summary>
    string? ConcreteTypeForNew,
    string KeyType,
    string KeyParamName,
    /// <summary>Ref kind prefix for the key parameter (e.g., "in " for in parameters).</summary>
    string KeyRefPrefix,
    string NullableKeyType,
    bool HasGetter,
    bool HasSetter,
    string SimpleInterfaceName,
    bool NeedsNewKeyword,
    /// <summary>Friendly name for the key type (e.g., "Int32", "String") for type-suffixed invoke methods.</summary>
    string KeyTypeFriendlyName,

    // Multi-param indexer support
    /// <summary>Parameter declarations for the indexer (e.g., "int a, string b").</summary>
    string ParameterSignature = "",
    /// <summary>Parameter types for callback delegates (e.g., "int, string").</summary>
    string ParameterTypes = "",
    /// <summary>Key expression for recording (e.g., "key" or "(a, b)").</summary>
    string KeyExpression = "",
    /// <summary>Argument list for passing parameters (e.g., "a, b").</summary>
    string ArgumentList = "",

    // Init-only support
    /// <summary>True if the indexer setter is init-only.</summary>
    bool IsInitOnly = false,

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
