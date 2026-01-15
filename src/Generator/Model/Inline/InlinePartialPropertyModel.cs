// src/Generator/Model/Inline/InlinePartialPropertyModel.cs
#nullable enable

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a partial property auto-implementation.
/// </summary>
internal sealed record InlinePartialPropertyModel(
    /// <summary>The property name.</summary>
    string PropertyName,
    /// <summary>The stub type name (e.g., "IRepositoryStub").</summary>
    string StubTypeName,
    /// <summary>The access modifier (public, internal, etc.).</summary>
    string AccessModifier,
    /// <summary>The backing field name.</summary>
    string BackingFieldName);
