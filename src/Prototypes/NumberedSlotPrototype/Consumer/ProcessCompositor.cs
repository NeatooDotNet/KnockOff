using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Consumer;

/// <summary>
/// Compositor for overloaded void Process method.
/// Each overload is assigned to a distinct numbered slot.
/// This is what the generator would emit.
///
/// The key insight: the class implements IVoidOverloadSlot1, IVoidOverloadSlot2, IVoidOverloadSlot3
/// -- three DIFFERENT interface types, each implemented exactly once.
/// The old approach used IVoidOverload three times, which caused CS1061.
/// </summary>
public class ProcessCompositor
    : IVoidOverloadSlot1<ProcessDelegate1, string>,
      IVoidOverloadSlot2<ProcessDelegate2, (string data, int priority)>,
      IVoidOverloadSlot3<ProcessDelegate3, (string data, int priority, bool @async)>
{
    // Backing interceptors (generated fields)
    private readonly VoidMethodInterceptor<ProcessDelegate1, string> _interceptor1 = new();
    private readonly VoidMethodInterceptor<ProcessDelegate2, (string data, int priority)> _interceptor2 = new();
    private readonly VoidMethodInterceptor<ProcessDelegate3, (string data, int priority, bool @async)> _interceptor3 = new();

    // Explicit interface implementations
    VoidMethodInterceptor<ProcessDelegate1, string>
        IVoidOverloadSlot1<ProcessDelegate1, string>.VoidSlot1Interceptor => _interceptor1;

    VoidMethodInterceptor<ProcessDelegate2, (string data, int priority)>
        IVoidOverloadSlot2<ProcessDelegate2, (string data, int priority)>.VoidSlot2Interceptor => _interceptor2;

    VoidMethodInterceptor<ProcessDelegate3, (string data, int priority, bool @async)>
        IVoidOverloadSlot3<ProcessDelegate3, (string data, int priority, bool @async)>.VoidSlot3Interceptor => _interceptor3;
}
