// src/Generator/Model/Flat/FlatEventModel.cs
#nullable enable

namespace KnockOff.Model.Flat;

/// <summary>
/// Resolved model for an event in flat API generation.
/// </summary>
internal sealed record FlatEventModel(
    string InterceptorName,
    string InterceptorClassName,
    string DeclaringInterface,
    string EventName,
    string DelegateType,
    string RaiseParameters,
    string RaiseArguments,
    string RaiseReturnType,
    bool RaiseReturnsValue,
    bool UsesDynamicInvoke,
    bool NeedsNewKeyword);
