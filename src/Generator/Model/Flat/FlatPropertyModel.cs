// src/Generator/Model/Flat/FlatPropertyModel.cs
#nullable enable
using KnockOff;

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
    bool NeedsNewKeyword,
    InterfaceMemberInfo? DelegationTarget,
    string? DelegationTargetInterface,
    /// <summary>
    /// True if the user has defined a "protected override" property with the _ suffix
    /// in their partial class (base class user property pattern).
    /// </summary>
    bool HasUserOverride = false);
