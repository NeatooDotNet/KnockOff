// src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Unified model for property interceptor generation.
/// Used by both FlatRenderer and InlineRenderer via PropertyInterceptorRenderer.
/// Contains all information needed to render a property interceptor class
/// with Get()/Set() methods, tracking, sequences, and verification.
/// </summary>
internal sealed record UnifiedPropertyInterceptorModel(
    // Identity
    /// <summary>Interceptor class name (e.g., "ValueInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>Property name (e.g., "Value").</summary>
    string PropertyName,
    /// <summary>The declaring interface type for Source(T) feature (e.g., "global::MyNamespace.IMyInterface").</summary>
    string DeclaringInterface,

    // Type information
    /// <summary>The property value type (e.g., "string", "int").</summary>
    string ValueType,
    /// <summary>The nullable version of the value type (e.g., "string?", "int?").</summary>
    string NullableValueType,
    /// <summary>Default expression for the value (e.g., "default!", "new List&lt;int&gt;()").</summary>
    string DefaultExpression,

    // Accessor configuration
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>Whether this is an init-only property (setter is init accessor).</summary>
    bool IsInitOnly,

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

/// <summary>
/// Options controlling property interceptor rendering behavior.
/// Allows customization for different rendering contexts (flat vs inline).
/// </summary>
internal sealed record PropertyInterceptorRenderOptions(
    /// <summary>Base indentation level (0 for flat, typically 2-3 for inline nested classes).</summary>
    int BaseIndent,
    /// <summary>Whether InvokeGet/InvokeSet methods should take a strict parameter (flat does, inline accesses stub.Strict).</summary>
    bool IncludeStrictParameter,
    /// <summary>How to access Strict mode in implementations (e.g., "Strict" or "stub.Strict").</summary>
    string StrictAccessExpression,
    /// <summary>Type parameters for the interceptor class (e.g., "&lt;T&gt;" for open generics). Empty for non-generic.</summary>
    string InterceptorTypeParameters = "",
    /// <summary>Constraint clauses for the interceptor class (e.g., " where T : class"). Empty for non-generic.</summary>
    string InterceptorConstraints = "");
