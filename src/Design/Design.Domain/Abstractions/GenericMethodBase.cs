// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class with generic virtual methods for testing
// class stub generic method support (patterns 3, 4, 6, 9)
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with generic virtual/abstract methods.
/// Used to test class stub generic method support.
///
/// DESIGN DECISION: Generic methods on class stubs use the Of&lt;T&gt;() pattern,
/// identical to interface stubs. Users access typed handlers via stub.MethodName.Of&lt;T&gt;().
///
/// PATTERN COMPARISON:
/// - Non-generic method: stub.Process.Return("value")  -- direct configuration
/// - Generic method: stub.Convert.Of&lt;int&gt;().Return(v =&gt; 42)  -- typed handler
/// </summary>
public abstract class GenericMethodBase
{
    /// <summary>
    /// Virtual generic method returning T.
    /// Tests: generic return type, base call fallback when unconfigured.
    /// </summary>
    public virtual T Convert<T>(object value) => default!;

    /// <summary>
    /// Abstract generic void method.
    /// Tests: abstract generic method, no base fallback.
    /// </summary>
    public abstract void Register<T>();

    /// <summary>
    /// Virtual generic method with multiple type parameters and constraints.
    /// Tests: multiple type params, constraint propagation, base fallback.
    /// </summary>
    public virtual TResult Transform<TInput, TResult>(TInput input)
        where TInput : class
        where TResult : new()
        => new TResult();

    /// <summary>
    /// Non-generic method alongside generic methods.
    /// Tests: mixed method groups work correctly.
    /// </summary>
    public virtual string GetName() => "default";

    /// <summary>
    /// Generic method with non-generic parameters (for RecordCall tracking).
    /// Tests: non-generic params tracked via RecordCall, generic params excluded.
    /// </summary>
    public virtual void Process<T>(T item, string label) { }

    /// <summary>
    /// Non-generic overload of Process -- same name as the generic Process&lt;T&gt;.
    /// Tests: mixed overload edge case (generic + non-generic with same name).
    /// The builder must split these into separate interceptors:
    /// - Process (non-generic) -> UnifiedMethodInterceptorModel
    /// - Process (generic) -> InlineGenericMethodHandlerModel
    /// </summary>
    public virtual void Process(string label) { }
}
