// src/Generator/Model/Inline/InlineInterfaceImplementation.cs
#nullable enable
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for an explicit interface member implementation.
/// </summary>
internal sealed record InlineInterfaceImplementation(
    /// <summary>The member kind.</summary>
    InlineMemberKind Kind,
    /// <summary>The fully qualified interface name for the explicit implementation.</summary>
    string InterfaceFullName,
    /// <summary>The simple interface name for error messages.</summary>
    string SimpleInterfaceName,
    /// <summary>The member name.</summary>
    string MemberName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Whether this is an init-only property.</summary>
    bool IsInitOnly,
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>The interceptor property name on the stub class.</summary>
    string InterceptorName,
    /// <summary>Parameter declarations for methods/indexers.</summary>
    string ParameterDeclarations,
    /// <summary>Argument list for method calls.</summary>
    string ArgumentList,
    /// <summary>Arguments for RecordCall.</summary>
    string RecordCallArgs,
    /// <summary>Arguments for OnCall invocation.</summary>
    string OnCallArgs,
    /// <summary>Default value expression for return.</summary>
    string DefaultExpression,
    /// <summary>Default value strategy.</summary>
    DefaultValueStrategy DefaultStrategy,
    /// <summary>Whether the return type is nullable.</summary>
    bool IsNullable,
    /// <summary>Concrete type for new instance creation, if applicable.</summary>
    string? ConcreteTypeForNew,
    /// <summary>Pragma disable for setter nullability, if needed.</summary>
    string? SetterPragmaDisable,
    /// <summary>Pragma restore for setter nullability, if needed.</summary>
    string? SetterPragmaRestore,
    /// <summary>For generic methods: type parameter declaration.</summary>
    string TypeParameterDecl,
    /// <summary>For generic methods: constraint clauses.</summary>
    string ConstraintClauses,
    /// <summary>For generic methods: Of&lt;T&gt;() access expression.</summary>
    string OfTypeAccess,
    /// <summary>For generic methods: non-generic argument list for RecordCall.</summary>
    string NonGenericArgList,
    /// <summary>Whether this is a generic method.</summary>
    bool IsGenericMethod,
    /// <summary>For indexers: key argument for dictionary lookup.</summary>
    string? KeyArg,
    /// <summary>Delegation target if this member delegates to another.</summary>
    InlineDelegationTarget? DelegationTarget,
    /// <summary>Statements to initialize out parameters (e.g., "paramName = default!;").</summary>
    EquatableArray<string> OutParameterInitializations);

/// <summary>
/// The kind of member being implemented.
/// </summary>
internal enum InlineMemberKind
{
    Property,
    Indexer,
    Method,
    Event
}

/// <summary>
/// Model for delegation target information.
/// </summary>
internal sealed record InlineDelegationTarget(
    /// <summary>The interface full name where the target is declared.</summary>
    string TargetInterfaceFullName,
    /// <summary>The target member name.</summary>
    string TargetMemberName,
    /// <summary>The target return type.</summary>
    string TargetReturnType,
    /// <summary>Cast arguments for method delegation.</summary>
    string CastArguments);
