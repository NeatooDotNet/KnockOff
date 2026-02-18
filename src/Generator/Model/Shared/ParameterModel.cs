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
    string? XmlDoc = null);
