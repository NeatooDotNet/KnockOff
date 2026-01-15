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
    EquatableArray<InlineClassMethodModel> Methods,
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
    /// <summary>Whether the class has required members.</summary>
    bool HasRequiredMembers,
    /// <summary>Required member names for initialization.</summary>
    EquatableArray<string> RequiredMemberNames);

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
    string StubClassName);

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
    string StubClassName);

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
    /// <summary>Delegate type for OnCall.</summary>
    string DelegateType,
    /// <summary>LastCallArg type or null.</summary>
    string? LastCallArgType,
    /// <summary>LastCallArgs type or null.</summary>
    string? LastCallArgsType,
    /// <summary>The stub class name for delegate references.</summary>
    string StubClassName);

/// <summary>
/// Model for an event in class stub generation.
/// </summary>
internal sealed record InlineClassEventModel(
    /// <summary>The interceptor class name.</summary>
    string InterceptorClassName,
    /// <summary>The event name.</summary>
    string EventName,
    /// <summary>The delegate type.</summary>
    string DelegateType);

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
    bool IsAbstract);

/// <summary>
/// Model for an indexer override in the Impl class.
/// </summary>
internal sealed record InlineClassImplIndexerModel(
    /// <summary>The indexer name (e.g., "Indexer", "IndexerString").</summary>
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
    string? ConcreteTypeForNew);

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
    /// <summary>OnCall argument list (e.g., "_stub, name, count").</summary>
    string OnCallArgumentList);

/// <summary>
/// Model for an event override in the Impl class.
/// </summary>
internal sealed record InlineClassImplEventModel(
    /// <summary>The event name.</summary>
    string EventName,
    /// <summary>The delegate type.</summary>
    string DelegateType);
