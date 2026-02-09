// src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a generic method handler class in inline stub generation.
/// Uses the Of&lt;T&gt;() pattern for type-safe access.
/// Supports multiple type parameter arities (e.g., Of&lt;T&gt;() and Of&lt;TIn, TOut&gt;()).
/// </summary>
internal sealed record InlineGenericMethodHandlerModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_ProcessInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The method name.</summary>
    string MethodName,
    /// <summary>The stub class name for delegate type references.</summary>
    string StubClassName,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string InterfaceTypeParameterList,
    /// <summary>Constraint clauses for interface type parameters.</summary>
    string InterfaceConstraintClauses,
    /// <summary>Type arity groups - one per unique type parameter count.</summary>
    EquatableArray<InlineGenericTypeArityGroup> ArityGroups);
