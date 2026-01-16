// src/Generator/Model/Flat/FlatMethodGroup.cs
#nullable enable
using KnockOff;

namespace KnockOff.Model.Flat;

/// <summary>
/// Groups multiple method overloads that share the same interceptor.
/// Used for generating interceptor classes with multiple OnCall overloads.
/// </summary>
internal sealed record FlatMethodGroup(
    string InterceptorName,
    string InterceptorClassName,
    bool NeedsNewKeyword,
    EquatableArray<FlatMethodModel> Methods);
