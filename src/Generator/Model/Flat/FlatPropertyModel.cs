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
    bool HasUserOverride = false,

    // Ref return support
    /// <summary>True if the property returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the property returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false)
{
    /// <summary>True if the property returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
