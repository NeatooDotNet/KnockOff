// src/Generator/Model/Inline/InlineGenerationUnit.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Top-level container for inline stub generation.
/// Contains all resolved information needed to emit the file.
/// </summary>
internal sealed record InlineGenerationUnit(
    string ClassName,
    string Namespace,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    EquatableArray<InlineInterfaceStubModel> InterfaceStubs,
    EquatableArray<InlineDelegateStubModel> DelegateStubs,
    EquatableArray<InlineClassStubModel> ClassStubs,
    EquatableArray<InlinePartialPropertyModel> PartialProperties,
    bool HasGenericMethods);
