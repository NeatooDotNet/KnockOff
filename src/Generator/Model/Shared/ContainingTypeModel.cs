// src/Generator/Model/Shared/ContainingTypeModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a containing type for nested class declarations.
/// </summary>
internal sealed record ContainingTypeModel(
    string Keyword,
    string Name,
    string AccessModifier);
