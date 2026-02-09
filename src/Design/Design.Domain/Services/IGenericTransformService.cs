// -----------------------------------------------------------------------------
// Design.Domain - Generic interface with method-level type parameters
// Used to verify P2 (Generic Standalone) and P8 (Open Generic Interface)
// when interface methods have their own type parameters alongside
// the interface-level type parameter.
// See: docs/todos/generic-type-gaps.md
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Generic interface with methods that have their own type parameters.
/// Exercises the interaction between interface-level T and method-level type params.
///
/// Pattern 2: [KnockOff] partial class Stub&lt;T&gt; : IGenericTransformService&lt;T&gt;
/// Pattern 8: [KnockOff(typeof(IGenericTransformService&lt;&gt;))]
/// </summary>
public interface IGenericTransformService<T> where T : class
{
    /// <summary>Method using only the interface type param (baseline).</summary>
    T? GetById(int id);

    /// <summary>
    /// Method with a single method-level type param.
    /// Tests: class-level T + method-level TResult coexistence.
    /// </summary>
    TResult Convert<TResult>(T input) where TResult : new();

    /// <summary>
    /// Method with multiple method-level type params.
    /// Tests: class-level T + method-level TIn, TOut coexistence.
    /// </summary>
    TOut Map<TIn, TOut>(TIn input, T context) where TIn : notnull;

    /// <summary>
    /// Void generic method with constraint referencing the class type param.
    /// Tests: method-level constraint interacting with class-level type param.
    /// </summary>
    void Register<THandler>(THandler handler) where THandler : class;
}
