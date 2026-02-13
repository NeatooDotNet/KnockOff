// src/Generator/Model/Inline/InlineInterfaceStubModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a single interface stub within the inline pattern.
/// Contains all resolved information needed to emit the stub class.
/// </summary>
internal sealed record InlineInterfaceStubModel(
    /// <summary>The stub class name (e.g., "IRepositoryStub").</summary>
    string StubClassName,
    /// <summary>The fully qualified interface name.</summary>
    string InterfaceFullName,
    /// <summary>The base type for the class declaration (handles open generics).</summary>
    string BaseType,
    /// <summary>Type parameters for the interface (for open generic interfaces).</summary>
    EquatableArray<TypeParameterModel> TypeParameters,
    /// <summary>Whether this is an open generic interface.</summary>
    bool IsOpenGeneric,
    /// <summary>Whether strict mode is enabled.</summary>
    bool Strict,
    /// <summary>Whether this interface has generic methods.</summary>
    bool HasGenericMethods,
    /// <summary>Property interceptors.</summary>
    EquatableArray<InlinePropertyModel> Properties,
    /// <summary>Indexer interceptors.</summary>
    EquatableArray<InlineIndexerModel> Indexers,
    /// <summary>Method interceptors (non-generic). Uses unified model for shared rendering.</summary>
    EquatableArray<UnifiedMethodInterceptorModel> Methods,
    /// <summary>Generic method handlers.</summary>
    EquatableArray<InlineGenericMethodHandlerModel> GenericMethodHandlers,
    /// <summary>Event interceptors.</summary>
    EquatableArray<InlineEventModel> Events,
    /// <summary>Interceptor properties to generate on the stub class.</summary>
    EquatableArray<InlineInterceptorPropertyModel> InterceptorProperties,
    /// <summary>All explicit interface implementations needed.</summary>
    EquatableArray<InlineInterfaceImplementation> Implementations,
    /// <summary>Source providers for Source(T) methods - one per interface in the hierarchy.</summary>
    EquatableArray<SourceProviderInfo> SourceProviders,
    /// <summary>True if a DIM shim class should be generated.</summary>
    bool HasDimShim = false,
    /// <summary>Explicit implementations for the DIM shim class (abstract members only). Empty if no shim needed.</summary>
    EquatableArray<InlineInterfaceImplementation> ShimImplementations = default);
