// -----------------------------------------------------------------------------
// Design.Domain - Repros: interface hierarchies using `new` to shadow a property
// with a different accessor set.
// -----------------------------------------------------------------------------

namespace Design.Domain.Entities;

public interface IInterfaceWide
{
    int Prop { get; set; }
}

public interface IInterfaceNarrow : IInterfaceWide
{
    new int Prop { get; }
}

// Opposite-direction shadow: base is get-only, derived adds a setter.
public interface IGetOnly
{
    int Value { get; }
}

public interface IGetSetFromGetOnly : IGetOnly
{
    new int Value { get; set; }
}

// Open-generic hierarchy — exercises Inline pipeline via pattern 8 and
// Flat pipeline via pattern 2 (generic standalone).
public interface IShadowedBase<T>
{
    T Item { get; set; }
}

public interface IShadowed<T> : IShadowedBase<T>
{
    new T Item { get; }
}
