// src/Generator/Model/Shared/ParameterModel.cs
#nullable enable
using Microsoft.CodeAnalysis;

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a resolved method parameter for code generation.
/// </summary>
internal sealed record ParameterModel(
    string Name,
    string EscapedName,
    string Type,
    string NullableType,
    RefKind RefKind,
    string RefPrefix,
    /// <summary>
    /// XML documentation text for this parameter, extracted from the original interface/class.
    /// Null if no documentation was provided. Already XML-escaped.
    /// </summary>
    string? XmlDoc = null,
    /// <summary>
    /// True if the parameter type is a ref struct (e.g., ReadOnlySpan&lt;T&gt;, Span&lt;T&gt;).
    /// Ref struct types cannot be boxed, used as generic type arguments, or stored in tuples.
    /// Methods with ref struct parameters get simplified interceptors (no args tracking).
    /// </summary>
    bool IsRefStruct = false,
    /// <summary>
    /// True if the parameter has the 'scoped' modifier.
    /// Scoped ref struct parameters must have 'scoped' on the implementing method.
    /// </summary>
    bool IsScoped = false);
