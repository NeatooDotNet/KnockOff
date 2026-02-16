#pragma warning disable CA1062 // Validate arguments of public methods

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for async non-void method overload slots.
/// Each slot gets its own set of Return/When extension methods.
/// </summary>
public static class AsyncMethodSlotExtensions
{
    // ---- Slot 1 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot1Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot1Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot1Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot1Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot1Interceptor.When(predicate);

    // ---- Slot 2 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot2Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot2Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot2Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot2Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot2Interceptor.When(predicate);

    // ---- Slot 3 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot3Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot3Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot3Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot3Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot3Interceptor.When(predicate);

    // ---- Slot 4 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot4Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot4Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot4Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot4Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot4Interceptor.When(predicate);

    // ---- Slot 5 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot5Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot5Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot5Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot5Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot5Interceptor.When(predicate);

    // ---- Slot 6 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot6Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot6Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot6Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot6Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot6Interceptor.When(predicate);

    // ---- Slot 7 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot7Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot7Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot7Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot7Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot7Interceptor.When(predicate);

    // ---- Slot 8 ----

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot8Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot8Interceptor.Return(callback);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot8Interceptor.Return(value);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot8Interceptor.When(args);

    public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.WhenBuilder When<TDelegate, TSyncDelegate, TArgs, TReturn>(
        this IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncMethodSlot8Interceptor.When(predicate);
}
