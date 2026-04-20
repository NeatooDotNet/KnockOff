// -----------------------------------------------------------------------------
// Design.Domain - Repros: class hierarchies using `new` to shadow a virtual
// property with a different accessor set. Companion to IInterfaceNarrow.cs
// for exercising the class-based stub pipelines (patterns 3, 4, 6, 9).
// -----------------------------------------------------------------------------

namespace Design.Domain.Abstractions;

// Narrow-first: base is virtual get/set, derived shadows with get-only.
public class WideClassBase
{
    public virtual int Prop { get; set; }
}

public class NarrowClassDerived : WideClassBase
{
    public new virtual int Prop { get; }
}

// Widening direction: base is virtual get-only, derived adds a setter.
public class GetOnlyClassBase
{
    public virtual int Value { get; }
}

public class GetSetFromGetOnlyClass : GetOnlyClassBase
{
    public new virtual int Value { get; set; }
}

// Open-generic hierarchy for patterns 4 and 9.
public class ShadowedClassBase<T> where T : class
{
    public virtual T? Item { get; set; }
}

public class ShadowedClassDerived<T> : ShadowedClassBase<T> where T : class
{
    public new virtual T? Item { get; }
}
