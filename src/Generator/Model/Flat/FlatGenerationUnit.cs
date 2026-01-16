// src/Generator/Model/Flat/FlatGenerationUnit.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Flat;

/// <summary>
/// Top-level container for standalone stub generation.
/// Contains all resolved information needed to emit the file.
/// </summary>
internal sealed record FlatGenerationUnit(
    string ClassName,
    string Namespace,
    EquatableArray<string> InterfaceList,
    EquatableArray<TypeParameterModel> TypeParameters,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    EquatableArray<FlatPropertyModel> Properties,
    EquatableArray<FlatIndexerModel> Indexers,
    EquatableArray<FlatMethodModel> Methods,
    /// <summary>Method groups for interceptor generation (groups overloads by name).</summary>
    EquatableArray<FlatMethodGroup> MethodGroups,
    EquatableArray<FlatGenericMethodHandlerModel> GenericMethodHandlers,
    EquatableArray<FlatEventModel> Events,
    bool HasGenericMethods,
    bool ImplementsIKnockOffStub,
    bool Strict);
