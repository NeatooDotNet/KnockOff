// src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs
#nullable enable
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;

namespace KnockOff.Model.StandaloneClass;

/// <summary>
/// Top-level container for standalone class stub generation.
/// Follows the same wrapper + nested Impl pattern as inline class stubs (ClassRenderer),
/// but the user's partial class IS the wrapper (not generated inside a Stubs container).
/// </summary>
internal sealed record StandaloneClassGenerationUnit(
    /// <summary>The user's stub class name (e.g., "ServiceStub").</summary>
    string ClassName,
    /// <summary>The target class being stubbed (fully qualified, e.g., "global::MyNamespace.ServiceBase").</summary>
    string TargetClassName,
    /// <summary>The target class name for Impl inheritance (includes type params for generics).</summary>
    string TargetClassForInheritance,
    /// <summary>The namespace.</summary>
    string Namespace,
    /// <summary>Type parameters from the user's stub class.</summary>
    EquatableArray<TypeParameterModel> TypeParameters,
    /// <summary>Containing types chain for nested class support.</summary>
    EquatableArray<ContainingTypeModel> ContainingTypes,
    /// <summary>Property interceptor classes to generate.</summary>
    EquatableArray<InlineClassPropertyModel> Properties,
    /// <summary>Indexer interceptor classes to generate.</summary>
    EquatableArray<InlineClassIndexerModel> Indexers,
    /// <summary>Method interceptors to generate.</summary>
    EquatableArray<UnifiedMethodInterceptorModel> Methods,
    /// <summary>Event interceptors to generate.</summary>
    EquatableArray<InlineClassEventModel> Events,
    /// <summary>Generic method handler interceptor classes (Of&lt;T&gt;() pattern).</summary>
    EquatableArray<InlineGenericMethodHandlerModel> GenericMethodHandlers,
    /// <summary>Interceptor properties to add to the wrapper class.</summary>
    EquatableArray<InlineInterceptorPropertyModel> InterceptorProperties,
    /// <summary>Reset statements for ResetInterceptors method.</summary>
    EquatableArray<string> ResetStatements,
    /// <summary>Constructors for the wrapper class (creates nested Impl).</summary>
    EquatableArray<InlineConstructorModel> Constructors,
    /// <summary>Property overrides for the nested Impl class.</summary>
    EquatableArray<InlineClassImplPropertyModel> ImplProperties,
    /// <summary>Indexer overrides for the nested Impl class.</summary>
    EquatableArray<InlineClassImplIndexerModel> ImplIndexers,
    /// <summary>Method overrides for the nested Impl class.</summary>
    EquatableArray<InlineClassImplMethodModel> ImplMethods,
    /// <summary>Event overrides for the nested Impl class.</summary>
    EquatableArray<InlineClassImplEventModel> ImplEvents,
    /// <summary>Whether the target class has required members (C# 11).</summary>
    bool HasRequiredMembers,
    /// <summary>Required member names for initialization in Impl constructor.</summary>
    EquatableArray<string> RequiredMemberNames,
    /// <summary>Whether strict mode is enabled.</summary>
    bool Strict,
    /// <summary>Properties for the base class (virtual protected, user can override with '_' suffix).</summary>
    EquatableArray<BaseClassPropertyModel> BaseClassProperties,
    /// <summary>Methods for the base class (virtual protected, user can override with '_' suffix).</summary>
    EquatableArray<BaseClassMethodModel> BaseClassMethods,
    /// <summary>
    /// True when the target class is a C# record type.
    /// When true, the Impl class is emitted as 'sealed record' instead of 'sealed class'.
    /// </summary>
    bool IsRecord = false);
