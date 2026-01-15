// src/Generator/Model/Inline/InlineInterceptorPropertyModel.cs
#nullable enable

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for an interceptor property on the stub class.
/// </summary>
internal sealed record InlineInterceptorPropertyModel(
    /// <summary>The property name (e.g., "Value", "GetAll", "ChangedInterceptor").</summary>
    string PropertyName,
    /// <summary>The interceptor class type name.</summary>
    string InterceptorTypeName,
    /// <summary>Whether this property needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>Doc comment description.</summary>
    string Description);
