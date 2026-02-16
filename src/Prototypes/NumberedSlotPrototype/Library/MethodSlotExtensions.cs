namespace NumberedSlotPrototype.Library;

/// <summary>
/// Extension methods for non-void method overload slots.
/// Each slot gets its own set of Return/When/Verify extension methods.
/// </summary>
public static class MethodSlotExtensions
{
    // ---- Slot 1 ----

    public static string Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.Return(callback);

    public static string When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.When(args);

    public static string Verify<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.Verify();

    // ---- Slot 2 ----

    public static string Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.Return(callback);

    public static string When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.When(args);

    public static string Verify<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.Verify();

    // ---- Slot 3 ----

    public static string Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.Return(callback);

    public static string When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.When(args);

    public static string Verify<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.Verify();

    // ---- Slot 4 ----

    public static string Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.Return(callback);

    public static string When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.When(args);

    public static string Verify<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.Verify();
}
