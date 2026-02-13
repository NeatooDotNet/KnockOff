// src/Generator/Model/Flat/FlatDimShimInfo.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Shim data for a single interface that has DIM members.
/// Contains only the abstract members that need explicit delegation in the shim.
/// </summary>
internal sealed record FlatDimShimInfo(
    /// <summary>The fully qualified interface name this shim implements.</summary>
    string InterfaceFullName,
    /// <summary>Abstract property members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimPropertyMember> Properties,
    /// <summary>Abstract indexer members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimIndexerMember> Indexers,
    /// <summary>Abstract method members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimMethodMember> Methods,
    /// <summary>Abstract event members to delegate to _stub.</summary>
    EquatableArray<FlatDimShimEventMember> Events);

internal sealed record FlatDimShimPropertyMember(
    string InterfaceFullName,
    string Name,
    string ReturnType,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly);

internal sealed record FlatDimShimIndexerMember(
    string InterfaceFullName,
    string ReturnType,
    string ParameterDeclarations,
    string ArgumentList,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnly,
    bool ReturnsByRef,
    bool ReturnsByRefReadonly);

internal sealed record FlatDimShimMethodMember(
    string InterfaceFullName,
    string Name,
    string ReturnType,
    bool IsVoid,
    string ParameterDeclarations,
    string ArgumentList,
    bool IsGenericMethod,
    string TypeParameterDecl,
    string ConstraintClauses);

internal sealed record FlatDimShimEventMember(
    string InterfaceFullName,
    string Name,
    string DelegateType);
