// src/Generator/Model/Shared/UnifiedGenericMethodHandlerModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Unified model for generic method handler generation.
/// Used by both inline and flat renderers via GenericMethodRenderer.
/// Represents a generic method with Of&lt;T&gt;() access pattern.
/// </summary>
internal sealed record UnifiedGenericMethodHandlerModel(
    // Identity
    /// <summary>Interceptor class name (e.g., "DeserializeInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>Method name (e.g., "Deserialize").</summary>
    string MethodName,

    // Type parameters
    /// <summary>Type parameter names (e.g., "T" or "TKey, TValue").</summary>
    string TypeParameterNames,
    /// <summary>Key type for dictionary storage (e.g., "System.Type" or "(System.Type, System.Type)").</summary>
    string KeyType,
    /// <summary>Key construction expression (e.g., "typeof(T)" or "(typeof(TKey), typeof(TValue))").</summary>
    string KeyConstruction,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses,

    // Typed handler
    /// <summary>Typed handler class name (e.g., "TypedHandler").</summary>
    string TypedHandlerClassName,

    // Owner context
    /// <summary>The class name that owns this interceptor (for delegate signatures).</summary>
    string OwnerClassName,
    /// <summary>Type parameters on the owner class (for delegate signatures).</summary>
    string OwnerTypeParameters,

    // Signature
    /// <summary>Delegate signature for the typed handler.</summary>
    string DelegateSignature,
    /// <summary>Parameters excluding type parameters (for tracking).</summary>
    EquatableArray<ParameterModel> NonGenericParams,
    /// <summary>Parameter declarations string.</summary>
    string ParameterDeclarations,
    /// <summary>LastCall type (tuple or single) for tracking.</summary>
    string? LastCallType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Return type.</summary>
    string ReturnType,
    /// <summary>Default expression when no callback configured.</summary>
    string DefaultExpression,
    /// <summary>Whether to throw when no callback and no default available.</summary>
    bool ThrowsOnDefault,

    // Visibility
    /// <summary>Whether this interceptor needs the 'new' keyword.</summary>
    bool NeedsNewKeyword);
