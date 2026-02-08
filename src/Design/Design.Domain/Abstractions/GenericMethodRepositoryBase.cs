// -----------------------------------------------------------------------------
// Design.Domain - Generic base class with method-level generic params
// Used to test class-level + method-level type parameter interaction
// (patterns 4 and 9)
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Generic base class that also has method-level type parameters.
/// Tests the interaction between class-level TEntity and method-level type params.
///
/// This exercises the subtle case where the builder must distinguish:
/// - TEntity: class-level type parameter (from the class declaration)
/// - TResult: method-level type parameter (from the method declaration)
///
/// Pattern 4: [KnockOffBase(typeof(GenericMethodRepositoryBase&lt;&gt;))]
/// Pattern 9: [KnockOff(typeof(GenericMethodRepositoryBase&lt;&gt;))]
/// </summary>
public abstract class GenericMethodRepositoryBase<TEntity> where TEntity : class
{
    /// <summary>
    /// Virtual method using class-level type param only (NOT a generic method).
    /// This uses TEntity from the class, not a method-level type param.
    /// </summary>
    public virtual TEntity? GetById(int id) => default;

    /// <summary>
    /// Generic method with method-level type param that interacts with class-level TEntity.
    /// Tests: method-level TResult alongside class-level TEntity.
    /// </summary>
    public virtual TResult ConvertEntity<TResult>(TEntity entity) => default!;

    /// <summary>
    /// Abstract generic void method.
    /// Tests: abstract generic method on a generic class.
    /// </summary>
    public abstract void MapTo<TTarget>(TEntity source);

    /// <summary>
    /// Non-generic method for comparison.
    /// Tests: non-generic methods still work on generic class alongside generic methods.
    /// </summary>
    public virtual string GetEntityName() => typeof(TEntity).Name;
}
