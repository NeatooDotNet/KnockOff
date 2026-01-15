// src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a generic method handler class in inline stub generation.
/// Uses the Of&lt;T&gt;() pattern for type-safe access.
/// </summary>
internal sealed record InlineGenericMethodHandlerModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_ProcessInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The method name.</summary>
    string MethodName,
    /// <summary>The return type (may include type parameters).</summary>
    string ReturnType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Type parameter names (e.g., "T", "TKey, TValue").</summary>
    string TypeParameterNames,
    /// <summary>Key type for dictionary (single Type or tuple of Types).</summary>
    string KeyType,
    /// <summary>Key construction expression (e.g., "typeof(T)" or "(typeof(TKey), typeof(TValue))").</summary>
    string KeyConstruction,
    /// <summary>Constraint clauses for method type parameters.</summary>
    string MethodConstraintClauses,
    /// <summary>The typed handler class name (e.g., "ProcessTypedHandler").</summary>
    string TypedHandlerClassName,
    /// <summary>Delegate signature for the typed handler.</summary>
    string DelegateSignature,
    /// <summary>Non-generic parameters for tracking.</summary>
    EquatableArray<ParameterModel> NonGenericParameters,
    /// <summary>LastCallArg type (for single non-generic param) or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs tuple type (for multiple non-generic params) or null.</summary>
    string? LastCallArgsType,
    /// <summary>The stub class name for delegate type references.</summary>
    string StubClassName,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string InterfaceTypeParameterList,
    /// <summary>Constraint clauses for interface type parameters.</summary>
    string InterfaceConstraintClauses);
