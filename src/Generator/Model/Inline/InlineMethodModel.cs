// src/Generator/Model/Inline/InlineMethodModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a non-generic method interceptor class in inline stub generation.
/// </summary>
internal sealed record InlineMethodModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_GetAllInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The method name.</summary>
    string MethodName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Input parameters (excluding out parameters).</summary>
    EquatableArray<ParameterModel> InputParameters,
    /// <summary>Delegate type for OnCall callback.</summary>
    string DelegateType,
    /// <summary>Parameter declarations for RecordCall.</summary>
    string RecordCallParameters,
    /// <summary>LastCallArg type (for single input param) or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs tuple type (for multiple input params) or null.</summary>
    string? LastCallArgsType,
    /// <summary>The stub class name for delegate type references.</summary>
    string StubClassName,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses);
