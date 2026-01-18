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
    /// <summary>Indexer groups for OfXxx pattern (groups indexers by base name).</summary>
    EquatableArray<FlatIndexerGroup> IndexerGroups,
    EquatableArray<FlatMethodModel> Methods,
    /// <summary>Method groups for interceptor generation (groups overloads by name).</summary>
    EquatableArray<FlatMethodGroup> MethodGroups,
    EquatableArray<FlatGenericMethodHandlerModel> GenericMethodHandlers,
    EquatableArray<FlatEventModel> Events,
    /// <summary>Source providers for Source(T) methods - one per interface in the hierarchy.</summary>
    EquatableArray<SourceProviderInfo> SourceProviders,
    bool HasGenericMethods,
    bool ImplementsIKnockOffStub,
    bool Strict);
