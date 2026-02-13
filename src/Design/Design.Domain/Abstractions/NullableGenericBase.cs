// -----------------------------------------------------------------------------
// Design.Domain - Abstract base class with nullable unconstrained generic methods
// Tests: Bug fix for CS0453 in class stubs (Nullable<T> on unconstrained type params)
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class demonstrating nullable unconstrained generic type parameters.
///
/// In C# 9+, T? on an unconstrained type parameter means "default value"
/// (null for reference types, default for value types). The generator must
/// handle this correctly in override methods and delegate signatures by
/// stripping the ? and wrapping with #nullable disable.
///
/// Without the fix, the generator emits TData? which the compiler interprets
/// as Nullable&lt;TData&gt; on unconstrained type params, causing CS0453.
/// </summary>
public abstract class NullableGenericBase
{
    /// <summary>
    /// Abstract: nullable return and parameter, unconstrained.
    /// Override must use #nullable disable to avoid CS0453.
    /// </summary>
    public abstract TData? NullableValues<TData>(TData? data);

    /// <summary>
    /// Abstract: nullable return only, unconstrained.
    /// Override must use #nullable disable to avoid CS0453.
    /// </summary>
    public abstract T? NullableReturn<T>();
}

/// <summary>
/// Virtual class demonstrating nullable unconstrained generic type parameters.
/// Same methods as NullableGenericBase but virtual (with default implementations).
/// Tests the virtual method override path with base call fallback.
/// </summary>
public class NullableGenericVirtual
{
    /// <summary>
    /// Virtual: nullable return and parameter, unconstrained.
    /// Override must use #nullable disable to avoid CS0453.
    /// </summary>
    public virtual TData? NullableValues<TData>(TData? data) => default;

    /// <summary>
    /// Virtual: nullable return only, unconstrained.
    /// Override must use #nullable disable to avoid CS0453.
    /// </summary>
    public virtual T? NullableReturn<T>() => default;
}
