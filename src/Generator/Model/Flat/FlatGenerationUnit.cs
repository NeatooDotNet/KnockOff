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
    /// <summary>True if any interface has DIM members requiring a shim class.</summary>
    bool HasDimShim = false,
    /// <summary>Fully qualified names of interfaces that have DIM members (for shim class declaration).</summary>
    EquatableArray<string> DimInterfaceNames = default,
    /// <summary>DIM member interceptor names that need _source wired to the shim in the constructor.</summary>
    EquatableArray<string> DimInterceptorNames = default,
    /// <summary>Shim data for each interface that has DIM members. Contains abstract-only member info for shim generation.</summary>
    EquatableArray<FlatDimShimInfo> DimShimInfos = default);
