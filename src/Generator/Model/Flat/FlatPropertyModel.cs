// src/Generator/Model/Flat/FlatPropertyModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for a property in flat API generation.
/// </summary>
internal sealed record FlatPropertyModel(
    string InterceptorName,
    string InterceptorClassName,
    string DeclaringInterface,
    string MemberName,
    string ReturnType,
    string NullableReturnType,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    string DefaultExpression,
    string? SetterPragmaDisable,
    string? SetterPragmaRestore,
    string SimpleInterfaceName,
    string? UserMethodName,
    bool NeedsNewKeyword);
