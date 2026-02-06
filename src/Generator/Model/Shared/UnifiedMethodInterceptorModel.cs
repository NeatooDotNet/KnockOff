// src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Unified model for method interceptor generation.
/// Used by both inline and flat renderers via MethodInterceptorRenderer.
/// Contains all information needed to render a method interceptor class
/// with OnCall() methods, tracking, sequences, and verification.
/// </summary>
internal sealed record UnifiedMethodInterceptorModel(
    // Identity
    /// <summary>Interceptor class name (e.g., "ProcessInterceptor").</summary>
    string InterceptorClassName,
    /// <summary>Method name (e.g., "Process").</summary>
    string MethodName,
    /// <summary>The declaring interface type for Source(T) feature (e.g., "global::MyNamespace.IMyInterface").</summary>
    string DeclaringInterface,

    // Owner context
    /// <summary>The class name that owns this interceptor (for delegate signatures).</summary>
    string OwnerClassName,
    /// <summary>Type parameters on the owner class (for delegate signatures).</summary>
    string OwnerTypeParameters,

    // Single-signature case (when Overloads is empty)
    /// <summary>All parameters for single-signature methods.</summary>
    EquatableArray<ParameterModel> Parameters,
    /// <summary>Trackable parameters (excludes out params) for single-signature methods.</summary>
    EquatableArray<ParameterModel> TrackableParameters,
    /// <summary>Parameter declarations string for single-signature methods.</summary>
    string ParameterDeclarations,
    /// <summary>Return type for single-signature methods.</summary>
    string ReturnType,
    /// <summary>Whether single-signature method returns void.</summary>
    bool IsVoid,
    /// <summary>OnCall delegate type for single-signature methods.</summary>
    string OnCallDelegateType,
    /// <summary>Whether a custom delegate is needed (vs Func/Action).</summary>
    bool NeedsCustomDelegate,
    /// <summary>Custom delegate signature if needed.</summary>
    string? CustomDelegateSignature,
    /// <summary>LastArg type for single param, null otherwise.</summary>
    string? LastArgType,
    /// <summary>LastArgs tuple type for multiple params, null otherwise.</summary>
    string? LastArgsType,
    /// <summary>IMethodCallBuilder interface type for OnCall return type.</summary>
    string BuilderInterface,
    /// <summary>Default expression when no callback configured.</summary>
    string DefaultExpression,
    /// <summary>Whether to throw when no callback and no default available.</summary>
    bool ThrowsOnDefault,

    // User method fallback support
    /// <summary>
    /// User method name for fallback (e.g., "Process_"). Null if no user override exists.
    /// When set, the interceptor's Invoke() falls back to this method instead of Source/Strict.
    /// </summary>
    string? UserMethodName,

    // Overload group case (when Overloads is not empty)
    /// <summary>
    /// Overload signatures when this is an overload group.
    /// Empty for single-signature methods.
    /// </summary>
    EquatableArray<MethodOverloadSignature> Overloads);

/// <summary>
/// Options controlling interceptor rendering behavior.
/// Allows customization for different rendering contexts (inline vs flat).
/// </summary>
internal sealed record InterceptorRenderOptions(
    /// <summary>Base indentation level (0 for flat, typically 2-3 for inline nested classes).</summary>
    int BaseIndent,
    /// <summary>Whether Invoke() method should take a strict parameter (flat does, inline accesses stub.Strict).</summary>
    bool IncludeStrictParameter,
    /// <summary>How to access Strict mode in implementations (e.g., "Strict" or "stub.Strict").</summary>
    string StrictAccessExpression,
    /// <summary>Type parameters for the interceptor class (e.g., "&lt;T&gt;" for open generics). Empty for non-generic.</summary>
    string InterceptorTypeParameters = "",
    /// <summary>Constraint clauses for the interceptor class (e.g., " where T : class"). Empty for non-generic.</summary>
    string InterceptorConstraints = "",
    /// <summary>
    /// When true, the Invoke() method falls back to calling the user method instead of Source/Strict.
    /// Used for standalone stubs with user method overrides. The user method name comes from the model.
    /// </summary>
    bool UserMethodFallback = false,
    /// <summary>
    /// Stub type name for user method fallback (e.g., "MyStub" or "MyStub&lt;T&gt;").
    /// When set along with UserMethodFallback, Invoke() takes a stub parameter to call user methods.
    /// Required for flat/standalone stubs where the interceptor is nested but needs to call instance methods.
    /// </summary>
    string? StubTypeName = null);
