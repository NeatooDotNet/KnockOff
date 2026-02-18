// src/Generator/Model/Shared/MethodOverloadSignature.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents one unique signature in a method overload group.
/// Used for generating per-signature delegates, sequences, and tracking classes.
/// </summary>
internal sealed record MethodOverloadSignature(
    /// <summary>Suffix for this signature (e.g., "String_Int32_void").</summary>
    string SignatureSuffix,
    /// <summary>All parameters for this signature.</summary>
    EquatableArray<ParameterModel> Parameters,
    /// <summary>Trackable parameters (excludes out params).</summary>
    EquatableArray<ParameterModel> TrackableParameters,
    /// <summary>Parameter declarations string (e.g., "string name, int count").</summary>
    string ParameterDeclarations,
    /// <summary>Return type for this signature.</summary>
    string ReturnType,
    /// <summary>Whether this signature returns void.</summary>
    bool IsVoid,
    /// <summary>Delegate type for this signature. Either a custom delegate name (e.g., "ProcessDelegate_String_void") or a Func/Action type (e.g., "global::System.Func&lt;string, string&gt;").</summary>
    string DelegateName,
    /// <summary>Full delegate signature declaration. Null when using Func/Action (no custom delegate needed).</summary>
    string? DelegateSignature,
    /// <summary>LastArg type for single param, null otherwise.</summary>
    string? LastArgType,
    /// <summary>LastArgs tuple type for multiple params, null otherwise.</summary>
    string? LastArgsType,
    /// <summary>IMethodCallBuilder interface type for Call return type.</summary>
    string BuilderInterface,
    /// <summary>Default expression when no callback configured.</summary>
    string DefaultExpression,
    /// <summary>Whether to throw when no callback and no default available.</summary>
    bool ThrowsOnDefault,
    /// <summary>
    /// Stub override name for this signature's fallback (e.g., "Process_"). Null if no stub override exists.
    /// In mixed overload groups, some signatures may have stub overrides while others do not.
    /// </summary>
    string? StubOverrideName = null,

    // Ref return support
    /// <summary>True if this signature returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if this signature returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,

    // XML documentation
    /// <summary>XML documentation summary text for this overload, extracted from the original interface/class. Null if none.</summary>
    string? XmlDocSummary = null,

    // Tuple delegate support
    /// <summary>True when the delegate type uses a named tuple parameter (2+ params, no ref/out). Affects how args are passed at call sites.</summary>
    bool UsesTupleCallDelegate = false)
{
    /// <summary>True if this signature returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
