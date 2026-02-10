// src/Generator/Model/Inline/InlineClassStubModel.cs
#nullable enable
using KnockOff;
using KnockOff.Model.Shared;

namespace KnockOff.Model.Inline;

/// <summary>
/// Model for a class stub in inline stub generation.
/// Uses composition pattern (wrapper + nested Impl class).
/// </summary>
internal sealed record InlineClassStubModel(
    /// <summary>The stub class name.</summary>
    string StubClassName,
    /// <summary>The fully qualified class type name.</summary>
    string ClassType,
    /// <summary>The base type for Impl class inheritance.</summary>
    string BaseType,
    /// <summary>Whether this is an open generic class.</summary>
    bool IsOpenGeneric,
    /// <summary>Type parameters for open generic classes.</summary>
    EquatableArray<TypeParameterModel> TypeParameters,
    /// <summary>Type parameter list string (e.g., "&lt;T&gt;").</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses string (e.g., "where T : class").</summary>
    string ConstraintClauses,
    /// <summary>Constructors for the class.</summary>
    EquatableArray<InlineConstructorModel> Constructors,
    /// <summary>Property interceptors.</summary>
    EquatableArray<InlineClassPropertyModel> Properties,
    /// <summary>Indexer interceptors.</summary>
    EquatableArray<InlineClassIndexerModel> Indexers,
    /// <summary>Method interceptors.</summary>
    EquatableArray<UnifiedMethodInterceptorModel> Methods,
    /// <summary>Event interceptors.</summary>
    EquatableArray<InlineClassEventModel> Events,
    /// <summary>Interceptor properties to generate on the wrapper class.</summary>
    EquatableArray<InlineInterceptorPropertyModel> InterceptorProperties,
    /// <summary>Reset statements for ResetInterceptors method.</summary>
    EquatableArray<string> ResetStatements,
    /// <summary>Impl class property overrides.</summary>
    EquatableArray<InlineClassImplPropertyModel> ImplProperties,
    /// <summary>Impl class indexer overrides.</summary>
    EquatableArray<InlineClassImplIndexerModel> ImplIndexers,
    /// <summary>Impl class method overrides.</summary>
    EquatableArray<InlineClassImplMethodModel> ImplMethods,
    /// <summary>Impl class event overrides.</summary>
    EquatableArray<InlineClassImplEventModel> ImplEvents,
    /// <summary>Generic method handler interceptor classes (Of&lt;T&gt;() pattern).</summary>
    EquatableArray<InlineGenericMethodHandlerModel> GenericMethodHandlers,
    /// <summary>Whether the class has required members.</summary>
    bool HasRequiredMembers,
    /// <summary>Required member names for initialization.</summary>
    EquatableArray<string> RequiredMemberNames,
    /// <summary>
    /// True when the target class is a C# record type.
    /// When true, the Impl class is emitted as 'sealed record' instead of 'sealed class'.
    /// </summary>
    bool IsRecord = false);

/// <summary>
/// Model for a constructor in class stub generation.
/// </summary>
internal sealed record InlineConstructorModel(
    /// <summary>Parameter declarations for the constructor.</summary>
    string ParameterDeclarations,
    /// <summary>Argument list for base constructor call.</summary>
    string BaseCallArguments);

/// <summary>
/// Model for a property in class stub generation.
/// </summary>
internal sealed record InlineClassPropertyModel(
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The property name.</summary>
    string PropertyName,
    /// <summary>The property return type.</summary>
    string ReturnType,
    /// <summary>The nullable return type.</summary>
    string NullableReturnType,
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>Whether this is a required property.</summary>
    bool IsRequired,
    /// <summary>The stub class name for delegate references.</summary>
    string StubClassName,
    /// <summary>Type parameter list string (e.g., "&lt;T&gt;").</summary>
    string TypeParameterList = "",
    /// <summary>Constraint clauses string (e.g., "where T : class").</summary>
    string ConstraintClauses = "",
    // Ref return support
    /// <summary>True if the property returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the property returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false)
{
    /// <summary>True if the property returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}

/// <summary>
/// Model for an indexer in class stub generation.
/// </summary>
internal sealed record InlineClassIndexerModel(
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The indexer name.</summary>
    string IndexerName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>The key type.</summary>
    string KeyType,
    /// <summary>Whether the indexer has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the indexer has a setter.</summary>
    bool HasSetter,
    /// <summary>Parameter declarations.</summary>
    string ParameterDeclarations,
    /// <summary>Argument list.</summary>
    string ArgumentList,
    /// <summary>Key expression for recording.</summary>
    string KeyExpression,
    /// <summary>The stub class name for delegate references.</summary>
    string StubClassName,
    /// <summary>Friendly name for the key type (e.g., "Int32", "Int32_String") for type-suffixed invoke methods in multi-indexer.</summary>
    string KeyTypeFriendlyName = "",
    /// <summary>Type parameter list string (e.g., "&lt;T&gt;").</summary>
    string TypeParameterList = "",
    /// <summary>Constraint clauses string (e.g., "where T : class").</summary>
    string ConstraintClauses = "",
    // Ref return support
    /// <summary>True if the indexer returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the indexer returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false)
{
    /// <summary>True if the indexer returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}

/// <summary>
/// Model for a method in class stub generation.
/// </summary>
internal sealed record InlineClassMethodModel(
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The handler property name (may include overload suffix).</summary>
    string HandlerName,
    /// <summary>The method name.</summary>
    string MethodName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Parameter declarations for the method.</summary>
    string ParameterDeclarations,
    /// <summary>Argument list for base call.</summary>
    string ArgumentList,
    /// <summary>Input parameters for tracking.</summary>
    EquatableArray<ParameterModel> InputParameters,
    /// <summary>Delegate type for Call.</summary>
    string DelegateType,
    /// <summary>LastCallArg type or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs type or null.</summary>
    string? LastCallArgsType,
    /// <summary>The stub class name for delegate references.</summary>
    string StubClassName,
    /// <summary>Type parameter list string (e.g., "&lt;T&gt;").</summary>
    string TypeParameterList = "",
    /// <summary>Constraint clauses string (e.g., "where T : class").</summary>
    string ConstraintClauses = "");

/// <summary>
/// Model for an event in class stub generation.
/// </summary>
internal sealed record InlineClassEventModel(
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The event name.</summary>
    string EventName,
    /// <summary>The delegate type.</summary>
    string DelegateType,
    /// <summary>Type parameter list string (e.g., "&lt;T&gt;").</summary>
    string TypeParameterList = "",
    /// <summary>Constraint clauses string (e.g., "where T : class").</summary>
    string ConstraintClauses = "",
    /// <summary>Parameter declarations for the Raise method (e.g., "object? sender, EventArgs e").</summary>
    string RaiseParameters = "",
    /// <summary>Argument names for the Raise method (e.g., "sender, e").</summary>
    string RaiseArguments = "",
    /// <summary>Return type of the Raise method (e.g., "void").</summary>
    string RaiseReturnType = "void",
    /// <summary>Whether the delegate returns a value (Func-style).</summary>
    bool RaiseReturnsValue = false,
    /// <summary>Whether the delegate requires DynamicInvoke (custom delegates).</summary>
    bool UsesDynamicInvoke = false);

// ==========================================================================
// Impl class member models - for nested Impl class overrides
// ==========================================================================

/// <summary>
/// Model for a property override in the Impl class.
/// </summary>
internal sealed record InlineClassImplPropertyModel(
    /// <summary>The property name.</summary>
    string PropertyName,
    /// <summary>The property return type.</summary>
    string ReturnType,
    /// <summary>The access modifier (public, protected, etc.).</summary>
    string AccessModifier,
    /// <summary>Whether this is a required property.</summary>
    bool IsRequired,
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>Whether this is an init-only setter.</summary>
    bool IsInitOnly,
    /// <summary>Whether this is an abstract property (no base call).</summary>
    bool IsAbstract,
    /// <summary>
    /// True if the user has defined a 'protected override' property with the _ suffix
    /// in their partial class (base class stub override property pattern).
    /// </summary>
    bool HasStubOverride = false,
    // Ref return support
    /// <summary>True if the property returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the property returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,
    /// <summary>True if the property setter has [AllowNull]. Generated override needs pragma disable CS8765.</summary>
    bool SetterHasAllowNull = false)
{
    /// <summary>True if the property returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}

/// <summary>
/// Model for an indexer override in the Impl class.
/// </summary>
internal sealed record InlineClassImplIndexerModel(
    /// <summary>The indexer name (always "Indexer" -- all indexers share one interceptor).</summary>
    string IndexerName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>The access modifier (public, protected, etc.).</summary>
    string AccessModifier,
    /// <summary>Parameter declarations (e.g., "string key").</summary>
    string ParameterDeclarations,
    /// <summary>Argument list (e.g., "key").</summary>
    string ArgumentList,
    /// <summary>Key expression for dictionary key (e.g., "key" or "(key1, key2)").</summary>
    string KeyExpression,
    /// <summary>Whether the indexer has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the indexer has a setter.</summary>
    bool HasSetter,
    /// <summary>Whether this is an abstract indexer (no base call).</summary>
    bool IsAbstract,
    /// <summary>Whether the indexer is nullable.</summary>
    bool IsNullable,
    /// <summary>Default value strategy.</summary>
    DefaultValueStrategy DefaultStrategy,
    /// <summary>Concrete type for new() if applicable.</summary>
    string? ConcreteTypeForNew,
    /// <summary>Invoke suffix for type-suffixed methods in multi-indexer (e.g., "_String", "_Int32"). Empty for single-indexer.</summary>
    string InvokeSuffix = "",
    // Ref return support
    /// <summary>True if the indexer returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the indexer returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,
    /// <summary>True if the setter is init-only.</summary>
    bool IsInitOnly = false,
    /// <summary>True if the setter has [AllowNull] attribute.</summary>
    bool SetterHasAllowNull = false)
{
    /// <summary>True if the indexer returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}

/// <summary>
/// Model for a method override in the Impl class.
/// </summary>
internal sealed record InlineClassImplMethodModel(
    /// <summary>The handler property name on the wrapper (e.g., "DoWork", "DoWork1").</summary>
    string HandlerName,
    /// <summary>The method name.</summary>
    string MethodName,
    /// <summary>The return type.</summary>
    string ReturnType,
    /// <summary>The access modifier (public, protected, etc.).</summary>
    string AccessModifier,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Whether the return type is Task.</summary>
    bool IsTask,
    /// <summary>Whether the return type is ValueTask.</summary>
    bool IsValueTask,
    /// <summary>Whether this is an abstract method (no base call).</summary>
    bool IsAbstract,
    /// <summary>Parameter declarations (e.g., "string name, int count").</summary>
    string ParameterDeclarations,
    /// <summary>Argument list (e.g., "name, count").</summary>
    string ArgumentList,
    /// <summary>Input argument list for RecordCall (e.g., "name, count").</summary>
    string InputArgumentList,
    /// <summary>Call argument list (e.g., "_stub, name, count").</summary>
    string CallArgumentList,
    /// <summary>Invoke suffix for multi-overload interceptors (e.g., "_NoParams_TNullable"). Empty for single-signature interceptors.</summary>
    string InvokeSuffix,
    /// <summary>
    /// True if the user has defined a 'protected override' method with the _ suffix
    /// in their partial class (base class stub override pattern).
    /// </summary>
    bool HasStubOverride = false,
    /// <summary>Whether this is a generic method override. Routes to generic rendering path in the Impl class.</summary>
    bool IsGenericMethod = false,
    /// <summary>Type parameter declaration for the override signature (e.g., "&lt;T&gt;" or "&lt;TKey, TValue&gt;").
    /// Empty for non-generic methods.</summary>
    string TypeParameterDecl = "",
    /// <summary>Constraint clauses for the override. Must be EMPTY for overrides -- C# inherits constraints from base.
    /// This field exists for potential future use but must be empty for override signatures.</summary>
    string ConstraintClauses = "",
    /// <summary>Of&lt;T&gt;() access expression to get the typed handler (e.g., ".Of&lt;T&gt;()").
    /// Empty for non-generic methods.</summary>
    string OfTypeAccess = "",
    /// <summary>Non-generic argument list for RecordCall. Excludes parameters typed with method-level type parameters.
    /// Empty for non-generic methods.</summary>
    string NonGenericArgList = "",
    /// <summary>Inner type argument for Task&lt;T&gt;/ValueTask&lt;T&gt; return types (e.g., "TResult").
    /// Used for async return type handling in abstract method fallback. Empty when not applicable.</summary>
    string TaskTypeArg = "",
    // Ref return support
    /// <summary>True if the method returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the method returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,
    /// <summary>True if the method has [DoesNotReturn]. Generated override must also have it.</summary>
    bool DoesNotReturn = false)
{
    /// <summary>True if the method returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}

/// <summary>
/// Model for an event override in the Impl class.
/// </summary>
internal sealed record InlineClassImplEventModel(
    /// <summary>The event name.</summary>
    string EventName,
    /// <summary>The delegate type.</summary>
    string DelegateType,
    /// <summary>The access modifier (public, protected, etc.).</summary>
    string AccessModifier = "public");
