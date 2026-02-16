using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Consumer;

/// <summary>
/// Compositor for overloaded non-void Calculate method.
/// Uses IMethodOverloadSlot1/2 for two overloads with different signatures.
/// </summary>
public class CalculateCompositor
    : IMethodOverloadSlot1<CalculateDelegate1, int, int>,
      IMethodOverloadSlot2<CalculateDelegate2, (int x, int y), int>
{
    private readonly MethodInterceptor<CalculateDelegate1, int, int> _interceptor1 = new();
    private readonly MethodInterceptor<CalculateDelegate2, (int x, int y), int> _interceptor2 = new();

    MethodInterceptor<CalculateDelegate1, int, int>
        IMethodOverloadSlot1<CalculateDelegate1, int, int>.MethodSlot1Interceptor => _interceptor1;

    MethodInterceptor<CalculateDelegate2, (int x, int y), int>
        IMethodOverloadSlot2<CalculateDelegate2, (int x, int y), int>.MethodSlot2Interceptor => _interceptor2;
}
