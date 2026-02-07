// src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Unified model for indexer interceptor generation.
/// Used by both FlatRenderer and InlineRenderer via IndexerInterceptorRenderer.
/// Contains all information needed to render an indexer interceptor class
/// with Get()/Set() methods, tracking, sequences, and verification.
/// </summary>
internal sealed record UnifiedIndexerInterceptorModel(
    // Identity
    /// <summary>Interceptor class name (e.g., "IndexerInterceptor", "IndexerInt32Interceptor").</summary>
    string InterceptorClassName,
    /// <summary>Indexer display name for verification messages (e.g., "Indexer").</summary>
    string IndexerName,
    /// <summary>The declaring interface type for Source(T) feature (e.g., "global::MyNamespace.IMyInterface").</summary>
    string DeclaringInterface,

    // Type information
    /// <summary>The key type (single type or tuple for multiple parameters).</summary>
    string KeyType,
    /// <summary>The nullable version of the key type.</summary>
    string NullableKeyType,
    /// <summary>The key parameter name (e.g., "key", "index").</summary>
    string KeyParamName,
    /// <summary>The single key type for the dictionary backing (e.g., same as KeyType for single param, or tuple type).</summary>
    string SingleKeyType,
    /// <summary>The value/return type of the indexer.</summary>
    string ValueType,
    /// <summary>The nullable version of the value type.</summary>
    string NullableValueType,
    /// <summary>Default expression for the value (e.g., "default!").</summary>
    string DefaultExpression,

    // Accessor configuration
    /// <summary>Whether the indexer has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the indexer has a setter.</summary>
    bool HasSetter,

    // Parameter information (for multi-param indexers)
    /// <summary>Parameter signature for RecordGet/RecordSet (e.g., "int x, int y").</summary>
    string ParameterSignature,
    /// <summary>Parameter types for callback delegates (e.g., "int, int").</summary>
    string ParameterTypes,
    /// <summary>Key expression for recording (e.g., "key" or "(x, y)").</summary>
    string KeyExpression);

/// <summary>
/// Options controlling indexer interceptor rendering behavior.
/// Allows customization for different rendering contexts (flat vs inline).
/// </summary>
internal sealed record IndexerInterceptorRenderOptions(
    /// <summary>Base indentation level (0 for flat, typically 2-3 for inline nested classes).</summary>
    int BaseIndent,
    /// <summary>Whether InvokeGet/InvokeSet methods should take a strict parameter (flat does, inline accesses stub.Strict).</summary>
    bool IncludeStrictParameter,
    /// <summary>How to access Strict mode in implementations (e.g., "Strict" or "stub.Strict").</summary>
    string StrictAccessExpression,
    /// <summary>Type parameters for the interceptor class (e.g., "&lt;T&gt;" for open generics). Empty for non-generic.</summary>
    string InterceptorTypeParameters = "",
    /// <summary>Constraint clauses for the interceptor class (e.g., " where T : class"). Empty for non-generic.</summary>
    string InterceptorConstraints = "");
