// src/Generator/Model/Inline/InlineEventModel.cs
#nullable enable

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for an event interceptor class in inline stub generation.
/// </summary>
internal sealed record InlineEventModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_ChangedInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The event name.</summary>
    string EventName,
    /// <summary>The delegate type (without trailing ?).</summary>
    string DelegateType,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses,
    /// <summary>Parameter declarations for the Raise method (e.g., "object? sender, EventArgs e").</summary>
    string RaiseParameters,
    /// <summary>Argument names for the Raise method (e.g., "sender, e").</summary>
    string RaiseArguments,
    /// <summary>Return type of the Raise method (e.g., "void").</summary>
    string RaiseReturnType,
    /// <summary>Whether the delegate returns a value (Func-style).</summary>
    bool RaiseReturnsValue,
    /// <summary>Whether the delegate requires DynamicInvoke (custom delegates).</summary>
    bool UsesDynamicInvoke);
