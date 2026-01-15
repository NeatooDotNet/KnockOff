// src/Generator/Model/Inline/InlineDelegateStubModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a delegate stub in inline stub generation.
/// </summary>
internal sealed record InlineDelegateStubModel(
    /// <summary>The stub class name (same as delegate name).</summary>
    string StubClassName,
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The fully qualified delegate type name.</summary>
    string DelegateType,
    /// <summary>The return type of the delegate.</summary>
    string ReturnType,
    /// <summary>Whether the delegate returns void.</summary>
    bool IsVoid,
    /// <summary>Whether this is an open generic delegate.</summary>
    bool IsOpenGeneric,
    /// <summary>Type parameters for open generic delegates.</summary>
    EquatableArray<TypeParameterModel> TypeParameters,
    /// <summary>Type parameter list string.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses string.</summary>
    string ConstraintClauses,
    /// <summary>Delegate parameters.</summary>
    EquatableArray<ParameterModel> Parameters,
    /// <summary>Parameter declarations for Invoke method.</summary>
    string InvokeParameterDeclarations,
    /// <summary>Argument list for Invoke calls.</summary>
    string InvokeArgumentList,
    /// <summary>OnCall delegate type.</summary>
    string OnCallType,
    /// <summary>LastCallArg type (for single param) or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs tuple type (for multiple params) or null.</summary>
    string? LastCallArgsType,
    /// <summary>Default expression for return value.</summary>
    string DefaultExpression);
