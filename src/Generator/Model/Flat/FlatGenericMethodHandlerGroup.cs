// src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Model for a group of generic method overloads that share an interceptor.
/// Supports multiple type parameter arities (multiple Of&lt;&gt;() methods)
/// with per-signature RecordCall/Call within each typed handler.
/// </summary>
internal sealed record FlatGenericMethodHandlerGroup(
    /// <summary>The interceptor property name (e.g., "Process2").</summary>
    string InterceptorName,
    /// <summary>The interceptor class name (e.g., "Process2Interceptor").</summary>
    string InterceptorClassName,
    /// <summary>The method name (e.g., "Process").</summary>
    string MethodName,
    /// <summary>Whether the interceptor property needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>Type arity groups - one per unique type parameter pattern.</summary>
    EquatableArray<FlatGenericTypeArityGroup> TypeArityGroups);

/// <summary>
/// Model for a group of generic overloads with the same type parameter arity.
/// E.g., all overloads with &lt;T&gt; or all overloads with &lt;TIn, TOut&gt;.
/// </summary>
internal sealed record FlatGenericTypeArityGroup(
    /// <summary>Type parameter names (e.g., "T" or "TIn, TOut").</summary>
    string TypeParameterNames,
    /// <summary>Number of type parameters (1, 2, etc.).</summary>
    int TypeParameterCount,
    /// <summary>Dictionary key type (e.g., "Type" or "(Type, Type)").</summary>
    string KeyType,
    /// <summary>Key construction expression (e.g., "typeof(T)" or "(typeof(TIn), typeof(TOut))").</summary>
    string KeyConstruction,
    /// <summary>Constraint clauses for the Of method.</summary>
    string ConstraintClauses,
    /// <summary>The typed handler class name (e.g., "ProcessTypedHandler").</summary>
    string TypedHandlerClassName,
    /// <summary>Signature groups within this type arity.</summary>
    EquatableArray<FlatGenericSignatureGroup> SignatureGroups,
    /// <summary>Whether return type is void (for any signature).</summary>
    bool IsVoid,
    /// <summary>The return type (from first signature, for reference).</summary>
    string ReturnType);

/// <summary>
/// Model for a single signature within a generic typed handler.
/// </summary>
internal sealed record FlatGenericSignatureGroup(
    /// <summary>Signature suffix for uniqueness (e.g., "_T_String").</summary>
    string SignatureSuffix,
    /// <summary>Delegate signature declaration.</summary>
    string DelegateSignature,
    /// <summary>Delegate name (without suffix for first, with suffix for others).</summary>
    string DelegateName,
    /// <summary>All parameters (including generic-typed ones).</summary>
    EquatableArray<ParameterModel> AllParameters,
    /// <summary>Non-generic parameters (parameters that don't use type parameters).</summary>
    EquatableArray<ParameterModel> NonGenericParams,
    /// <summary>LastCall type for tracking.</summary>
    string? LastCallType,
    /// <summary>Whether this overload returns void.</summary>
    bool IsVoid,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>RecordCall arguments expression.</summary>
    string RecordCallArgs);
