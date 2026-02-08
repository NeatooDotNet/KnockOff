// src/Generator/Model/Inline/InlineIndexerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for an indexer interceptor class in inline stub generation.
/// </summary>
internal sealed record InlineIndexerModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_IndexerInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The indexer name (Indexer, IndexerString, IndexerInt, etc.).</summary>
    string IndexerName,
    /// <summary>The return type of the indexer.</summary>
    string ReturnType,
    /// <summary>The key type (single type or tuple for multiple parameters).</summary>
    string KeyType,
    /// <summary>The single key type for dictionary backing.</summary>
    string SingleKeyType,
    /// <summary>The nullable key type.</summary>
    string NullableKeyType,
    /// <summary>Whether the indexer has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the indexer has a setter.</summary>
    bool HasSetter,
    /// <summary>Parameter declarations for RecordGet/RecordSet.</summary>
    string ParameterSignature,
    /// <summary>Parameter list for callback signatures.</summary>
    string ParameterTypes,
    /// <summary>Key expression for recording.</summary>
    string KeyExpression,
    /// <summary>The stub class name for delegate type references.</summary>
    string StubClassName,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses,
    /// <summary>The declaring interface type for Source(T) feature.</summary>
    string DeclaringInterface,
    /// <summary>Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern.</summary>
    string KeyTypeFriendlyName,

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
