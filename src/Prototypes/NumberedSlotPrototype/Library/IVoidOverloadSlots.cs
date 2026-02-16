namespace NumberedSlotPrototype.Library;

/// <summary>
/// Numbered slot interfaces for void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
///
/// The generator assigns overloads to slots: first overload -> Slot1, second -> Slot2, etc.
/// </summary>

public interface IVoidOverloadSlot1<TDelegate, TArgs>
    where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot1Interceptor { get; }
}

public interface IVoidOverloadSlot2<TDelegate, TArgs>
    where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot2Interceptor { get; }
}

public interface IVoidOverloadSlot3<TDelegate, TArgs>
    where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot3Interceptor { get; }
}

public interface IVoidOverloadSlot4<TDelegate, TArgs>
    where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot4Interceptor { get; }
}
