namespace NumberedSlotPrototype.Library;

/// <summary>
/// Simplified interceptor for non-void methods.
/// In real KnockOff this would store return callbacks and argument matchers.
/// For prototype purposes, methods return descriptive strings to verify resolution.
/// </summary>
public class MethodInterceptor<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    public string Return(TDelegate callback)
        => $"MethodReturn:{typeof(TDelegate).Name}";

    public string When(TArgs args)
        => $"MethodWhen:{typeof(TArgs).Name}:{args}";

    public string Verify()
        => $"MethodVerify:{typeof(TDelegate).Name}";
}
