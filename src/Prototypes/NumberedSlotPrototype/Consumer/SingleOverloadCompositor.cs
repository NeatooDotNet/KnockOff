using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Consumer;

// Delegate for a single non-overloaded method:
//   string Fetch(string url)
public delegate string FetchDelegate(string url);

/// <summary>
/// Compositor with only one overload slot.
/// Verifies the pattern still works when there's no overloading at all.
/// </summary>
public class SingleOverloadCompositor
    : IMethodOverloadSlot1<FetchDelegate, string, string>
{
    private readonly MethodInterceptor<FetchDelegate, string, string> _interceptor = new();

    MethodInterceptor<FetchDelegate, string, string>
        IMethodOverloadSlot1<FetchDelegate, string, string>.MethodSlot1Interceptor => _interceptor;
}
