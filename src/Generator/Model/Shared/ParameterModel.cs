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
    string RefPrefix);
