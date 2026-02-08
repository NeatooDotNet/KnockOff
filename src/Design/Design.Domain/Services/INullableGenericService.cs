// -----------------------------------------------------------------------------
// Design.Domain - Interface with nullable unconstrained generic methods
// Tests: Bug fix for spurious `where TData : class` constraint (CS8665)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Interface demonstrating nullable unconstrained generic type parameters.
///
/// In C# 9+, T? on an unconstrained type parameter means "default value"
/// (null for reference types, default for value types). The generator must
/// NOT add `where T : class` to the explicit implementation.
///
/// Contrast with IConstrainedGenericMethod in tests, where constraints like
/// `where T : Attribute` DO require `where T : class` in explicit impl.
/// </summary>
public interface INullableGenericService
{
    /// <summary>
    /// Nullable return and parameter, unconstrained.
    /// Generator must NOT emit `where TData : class`.
    /// </summary>
    TData? NullableValues<TData>(TData? data);

    /// <summary>
    /// Nullable return only, unconstrained.
    /// Generator must NOT emit `where T : class`.
    /// </summary>
    T? NullableReturn<T>();

    /// <summary>
    /// Interface-only constraint (IDisposable).
    /// Generator must NOT emit `where T : class` because interface constraints
    /// do not imply reference type (structs can implement interfaces).
    /// </summary>
    T? InterfaceConstrainedReturn<T>() where T : IDisposable;

    /// <summary>
    /// Constrained with class-implying constraint (Attribute).
    /// Generator MUST emit `where T : class` in explicit impl.
    /// This verifies the existing fix still works.
    /// </summary>
    T? ConstrainedNullableReturn<T>() where T : Attribute;
}
