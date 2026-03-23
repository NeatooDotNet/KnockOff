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
    /// <summary>The default value strategy for the property type.</summary>
    DefaultValueStrategy DefaultStrategy,
    /// <summary>Concrete type when DefaultStrategy is NewInstance and property type is an interface.</summary>
    string? ConcreteTypeForNew,
    string? SetterPragmaDisable,
    string? SetterPragmaRestore,
    string SimpleInterfaceName,
    bool NeedsNewKeyword,
    InterfaceMemberInfo? DelegationTarget,
    string? DelegationTargetInterface,
    /// <summary>
    /// True if the user has defined a "protected override" property with the _ suffix
    /// in their partial class (base class stub override property pattern).
    /// </summary>
    bool HasStubOverride = false,

    // Ref return support
    /// <summary>True if the property returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the property returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,
    /// <summary>True if the property value type is a ref struct (e.g., Span&lt;T&gt;, ReadOnlySpan&lt;T&gt;). Cannot use generic base class.</summary>
    bool IsRefStructType = false)
{
    /// <summary>True if the property returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
