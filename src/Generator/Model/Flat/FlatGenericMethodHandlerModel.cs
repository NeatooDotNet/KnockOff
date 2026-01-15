// src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for generic methods using the Of&lt;T&gt;() pattern.
/// </summary>
internal sealed record FlatGenericMethodHandlerModel(
    string InterceptorName,
    string InterceptorClassName,
    string MethodName,
    string TypeParameterNames,
    string KeyType,
    string KeyConstruction,
    string ConstraintClauses,
    string TypedHandlerClassName,
    string DelegateSignature,
    EquatableArray<ParameterModel> NonGenericParams,
    string? LastCallType,
    bool IsVoid,
    string ReturnType,
    bool NeedsNewKeyword);
