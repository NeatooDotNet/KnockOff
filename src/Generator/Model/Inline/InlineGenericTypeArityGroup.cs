// src/Generator/Model/Inline/InlineGenericTypeArityGroup.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a group of generic method overloads sharing the same type parameter arity.
/// E.g., all overloads with &lt;T&gt; or all overloads with &lt;TIn, TOut&gt;.
/// Used by inline, class, and standalone class pipelines.
/// </summary>
internal sealed record InlineGenericTypeArityGroup(
    /// <summary>Type parameter names (e.g., "T" or "TIn, TOut").</summary>
    string TypeParameterNames,
    /// <summary>Number of type parameters (1, 2, etc.).</summary>
    int TypeParameterCount,
    /// <summary>Dictionary key type (e.g., "global::System.Type" or "(global::System.Type, global::System.Type)").</summary>
    string KeyType,
    /// <summary>Key construction expression (e.g., "typeof(T)" or "(typeof(TIn), typeof(TOut))").</summary>
    string KeyConstruction,
    /// <summary>Constraint clauses for the Of method.</summary>
    string ConstraintClauses,
    /// <summary>The typed handler class name (e.g., "RunTypedHandler" or "RunTypedHandler2").</summary>
    string TypedHandlerClassName,
    /// <summary>Delegate signature declaration.</summary>
    string DelegateSignature,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>Non-generic parameters for tracking.</summary>
    EquatableArray<ParameterModel> NonGenericParameters,
    /// <summary>LastCallArg type (for single non-generic param) or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs tuple type (for multiple non-generic params) or null.</summary>
    string? LastCallArgsType,
    /// <summary>Whether the delegate signature needs #nullable disable because of unconstrained nullable type parameters.
    /// When true, the renderer wraps the delegate declaration with #nullable disable/restore and the
    /// DelegateSignature/ReturnType have already been stripped of ? on unconstrained type params.</summary>
    bool NeedsNullableDisable = false) : IEquatable<InlineGenericTypeArityGroup>;
