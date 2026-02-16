#pragma warning disable CA1062 // Validate arguments of public methods

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for non-void method overload slots.
/// Each slot gets its own set of Return/When extension methods.
/// </summary>
public static class MethodSlotExtensions
{
    // ---- Slot 1 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot1Interceptor.When(predicate);

    // ---- Slot 2 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot2<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot2Interceptor.When(predicate);

    // ---- Slot 3 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot3<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot3Interceptor.When(predicate);

    // ---- Slot 4 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot4<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot4Interceptor.When(predicate);

    // ---- Slot 5 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot5<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot5Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot5<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot5Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot5<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot5Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot5<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot5Interceptor.When(predicate);

    // ---- Slot 6 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot6<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot6Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot6<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot6Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot6<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot6Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot6<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot6Interceptor.When(predicate);

    // ---- Slot 7 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot7<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot7Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot7<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot7Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot7<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot7Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot7<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot7Interceptor.When(predicate);

    // ---- Slot 8 ----

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot8<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => self.MethodSlot8Interceptor.Return(callback);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot8<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => self.MethodSlot8Interceptor.Return(value);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot8<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => self.MethodSlot8Interceptor.When(args);

    public static MethodInterceptor<TDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TArgs, TReturn>(
        this IMethodOverloadSlot8<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.MethodSlot8Interceptor.When(predicate);
}
