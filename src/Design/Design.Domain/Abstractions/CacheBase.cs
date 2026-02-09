// -----------------------------------------------------------------------------
// Design.Domain - Multi-type-param abstract base class
// Used to verify P4 (Generic Standalone Class) with two type parameters
// See: docs/todos/generic-type-gaps.md
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

/// <summary>
/// Abstract base class with two type parameters and constraints.
/// Used to verify that [KnockOffBase(typeof(CacheBase&lt;,&gt;))] generates
/// correct code when the base class has multiple type parameters.
///
/// Pattern 4: [KnockOffBase(typeof(CacheBase&lt;,&gt;))]
/// Pattern 9: [KnockOff(typeof(CacheBase&lt;,&gt;))]
/// </summary>
public abstract class CacheBase<TKey, TValue> where TKey : notnull
{
    /// <summary>Abstract method using both type params.</summary>
    public abstract TValue Get(TKey key);

    /// <summary>Abstract void method using both type params.</summary>
    public abstract void Set(TKey key, TValue value);

    /// <summary>Virtual property (non-generic member on generic class).</summary>
    public virtual string Name => "Cache";

    /// <summary>Virtual method returning bool, uses TKey.</summary>
    public virtual bool ContainsKey(TKey key) => false;

    /// <summary>
    /// Generic method with its own type param on a multi-type-param class.
    /// Tests the interaction of class-level TKey/TValue with method-level TResult.
    /// </summary>
    public virtual TResult Transform<TResult>(TValue value) => default!;
}
