#pragma warning disable CA1062 // Validate arguments of public methods

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for async void overload slots.
/// Each slot gets its own set of Call/When extension methods.
/// </summary>
public static class AsyncVoidSlotExtensions
{
    // ---- Slot 1 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot1<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot1Interceptor.When(predicate);

    // ---- Slot 2 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot2<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot2Interceptor.When(predicate);

    // ---- Slot 3 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot3<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot3Interceptor.When(predicate);

    // ---- Slot 4 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot4<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot4Interceptor.When(predicate);

    // ---- Slot 5 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot5<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot5Interceptor.When(predicate);

    // ---- Slot 6 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot6<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot6Interceptor.When(predicate);

    // ---- Slot 7 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot7<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot7Interceptor.When(predicate);

    // ---- Slot 8 ----

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TArgs> self, Action<TArgs> callback)
        where TDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.Call(callback);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.When(args);

    public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IAsyncVoidOverloadSlot8<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => self.AsyncVoidSlot8Interceptor.When(predicate);
}
