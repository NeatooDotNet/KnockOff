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
    /// <summary>Stub override groups for tracking-only interceptors (groups stub override overloads by name).</summary>
    EquatableArray<FlatMethodGroup> StubOverrideGroups,
    EquatableArray<FlatGenericMethodHandlerModel> GenericMethodHandlers,
    /// <summary>Generic stub override handler groups for tracking-only interceptors with overloads.</summary>
    EquatableArray<FlatGenericMethodHandlerGroup> GenericStubOverrideHandlerGroups,
    EquatableArray<FlatEventModel> Events,
    /// <summary>Source providers for Source(T) methods - one per interface in the hierarchy.</summary>
    EquatableArray<SourceProviderInfo> SourceProviders,
    bool HasGenericMethods,
    bool ImplementsIKnockOffStub,
    bool Strict,
    /// <summary>When true, the user's partial class uses a primary constructor. Generated constructors need `: this()` initializer.</summary>
    bool HasPrimaryConstructor = false);
