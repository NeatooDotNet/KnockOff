namespace NumberedSlotPrototype.Library;

/// <summary>
/// Simplified interceptor for void methods.
/// In real KnockOff this would store callbacks and argument matchers.
/// For prototype purposes, methods return descriptive strings to verify resolution.
/// </summary>
public class VoidMethodInterceptor<TDelegate, TArgs>
    where TDelegate : Delegate
{
    public string Call(TDelegate callback)
        => $"VoidCall:{typeof(TDelegate).Name}";

    public string When(TArgs args)
        => $"VoidWhen:{typeof(TArgs).Name}:{args}";

    public string Verify()
        => $"VoidVerify:{typeof(TDelegate).Name}";
}
