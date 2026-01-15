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
    string ConstraintClauses);
