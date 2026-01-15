// src/Generator/Model/Shared/TypeParameterModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a resolved type parameter for code generation.
/// </summary>
internal sealed record TypeParameterModel(
    string Name,
    string Constraints);
