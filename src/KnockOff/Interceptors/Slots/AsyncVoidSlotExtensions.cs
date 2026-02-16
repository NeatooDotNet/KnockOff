#pragma warning disable CA1062 // Validate arguments of public methods

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for async void overload slots.
/// Each slot gets its own set of Call/When extension methods.
/// </summary>
public static class AsyncVoidSlotExtensions
{
    // ---- Slot 1 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.When(predicate);

    // ---- Slot 2 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.When(predicate);

    // ---- Slot 3 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.When(predicate);

    // ---- Slot 4 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.When(predicate);

    // ---- Slot 5 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.When(predicate);

    // ---- Slot 6 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.When(predicate);

    // ---- Slot 7 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.When(predicate);

    // ---- Slot 8 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TSyncDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TSyncDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TSyncDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TSyncDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TSyncDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.When(predicate);
}
