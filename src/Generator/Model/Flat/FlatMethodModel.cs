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
    string CallDelegateType,
    bool NeedsCustomDelegate,
    string? CustomDelegateName,
    string? CustomDelegateSignature,
    string DefaultExpression,
    bool ThrowsOnDefault,
    /// <summary>True if user provided a protected override method (base class pattern).</summary>
    bool HasUserOverride,
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
    bool IsPartOfOverloadGroup,

    // Ref return support
    /// <summary>True if the method returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the method returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,

    /// <summary>
    /// True when the explicit interface implementation needs #nullable disable / #nullable restore
    /// pragmas because it has unconstrained nullable type parameters (T? without where T : class).
    /// The ReturnType and ParameterDeclarations will have ? stripped from these type parameters.
    /// </summary>
    bool NeedsNullableDisable = false)
{
    /// <summary>True if the method returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
