// src/Generator/Model/Inline/InlinePropertyModel.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a property interceptor class in inline stub generation.
/// </summary>
internal sealed record InlinePropertyModel(
    /// <summary>The interceptor class name (e.g., "IRepositoryStub_ValueInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>The property name.</summary>
    string PropertyName,
    /// <summary>The property return type.</summary>
    string ReturnType,
    /// <summary>The nullable version of the return type.</summary>
    string NullableReturnType,
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>Whether this is an init-only property.</summary>
    bool IsInitOnly,
    /// <summary>The stub class name for delegate type references.</summary>
    string StubClassName,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses);
