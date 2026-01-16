// src/Generator/Model/Flat/FlatMethodModel.cs
#nullable enable
using KnockOff;
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for a method in flat API generation.
/// </summary>
internal sealed record FlatMethodModel(
    string InterceptorName,
    string InterceptorClassName,
    string DeclaringInterface,
    string MethodName,
    string ReturnType,
    bool IsVoid,
    EquatableArray<ParameterModel> Parameters,
    string ParameterDeclarations,
    string ParameterNames,
    string RecordCallArgs,
    EquatableArray<ParameterModel> TrackableParameters,
    string? LastCallType,
    string OnCallDelegateType,
    bool NeedsCustomDelegate,
    string? CustomDelegateName,
    string? CustomDelegateSignature,
    string DefaultExpression,
    bool ThrowsOnDefault,
    string? UserMethodCall,
    string SimpleInterfaceName,
    string TypeParameterDecl,
    string TypeParameterList,
    string ConstraintClauses,
    bool NeedsNewKeyword,
    bool IsGenericMethod,
    bool IsNullableReturn,
    string OfTypeAccess,
    InterfaceMemberInfo? DelegationTarget,
    string? DelegationTargetInterface,
    /// <summary>Suffix for this signature when part of overload group (e.g., "String_Int32_String").</summary>
    string SignatureSuffix,
    /// <summary>True if this method is part of an overload group with multiple signatures.</summary>
    bool IsPartOfOverloadGroup);
