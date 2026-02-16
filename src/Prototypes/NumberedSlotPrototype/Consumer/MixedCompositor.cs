using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Consumer;

// Delegates for a mixed interface:
//   void Execute(string command)
//   int Execute(string command, int timeout)
public delegate void ExecuteVoidDelegate(string command);
public delegate int ExecuteIntDelegate(string command, int timeout);

/// <summary>
/// Compositor that mixes void and non-void overload slots on the same class.
/// Tests that IVoidOverloadSlot1 and IMethodOverloadSlot1 can coexist.
/// Slot numbering is per-family: void overloads get void slots, method overloads get method slots.
/// </summary>
public class MixedCompositor
    : IVoidOverloadSlot1<ExecuteVoidDelegate, string>,
      IMethodOverloadSlot1<ExecuteIntDelegate, (string command, int timeout), int>
{
    private readonly VoidMethodInterceptor<ExecuteVoidDelegate, string> _voidInterceptor = new();
    private readonly MethodInterceptor<ExecuteIntDelegate, (string command, int timeout), int> _methodInterceptor = new();

    VoidMethodInterceptor<ExecuteVoidDelegate, string>
        IVoidOverloadSlot1<ExecuteVoidDelegate, string>.VoidSlot1Interceptor => _voidInterceptor;

    MethodInterceptor<ExecuteIntDelegate, (string command, int timeout), int>
        IMethodOverloadSlot1<ExecuteIntDelegate, (string command, int timeout), int>.MethodSlot1Interceptor => _methodInterceptor;
}
