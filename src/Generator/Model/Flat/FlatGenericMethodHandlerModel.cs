// src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for generic methods using the Of&lt;T&gt;() pattern.
/// Supports multiple type parameter arities (e.g., Of&lt;T&gt;() and Of&lt;TIn, TOut&gt;()).
/// </summary>
internal sealed record FlatGenericMethodHandlerModel(
    string InterceptorName,
    string InterceptorClassName,
    string MethodName,
    bool NeedsNewKeyword,
    /// <summary>Type arity groups - one per unique type parameter count.</summary>
    EquatableArray<FlatGenericMethodArityGroup> ArityGroups);

/// <summary>
/// Model for a group of generic method overloads sharing the same type parameter arity
/// within the flat (standalone interface) pipeline.
/// </summary>
internal sealed record FlatGenericMethodArityGroup(
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
    /// <summary>Non-generic parameters for tracking.</summary>
    EquatableArray<ParameterModel> NonGenericParams,
    /// <summary>LastCall type for tracking.</summary>
    string? LastCallType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>The return type.</summary>
    string ReturnType) : IEquatable<FlatGenericMethodArityGroup>;
