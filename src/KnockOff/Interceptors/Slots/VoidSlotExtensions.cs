#pragma warning disable CA1062 // Validate arguments of public methods

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for void overload slots.
/// Each slot gets its own set of Call/When extension methods.
/// The C# compiler resolves the correct slot based on the delegate type.
/// </summary>
public static class VoidSlotExtensions
{
    // ---- Slot 1 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot1Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot1Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot1Interceptor.When(predicate);

    // ---- Slot 2 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot2Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot2Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot2Interceptor.When(predicate);

    // ---- Slot 3 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot3Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot3Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot3Interceptor.When(predicate);

    // ---- Slot 4 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot4Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot4Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot4Interceptor.When(predicate);

    // ---- Slot 5 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot5<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot5Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot5<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot5Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot5<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot5Interceptor.When(predicate);

    // ---- Slot 6 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot6<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot6Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot6<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot6Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot6<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot6Interceptor.When(predicate);

    // ---- Slot 7 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot7<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot7Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot7<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot7Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot7<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot7Interceptor.When(predicate);

    // ---- Slot 8 ----

    public static VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
        this IVoidOverloadSlot8<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot8Interceptor.Call(callback);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot8<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot8Interceptor.When(args);

    public static VoidMethodInterceptor<TDelegate, TArgs>.VoidWhenBuilder When<TDelegate, TArgs>(
        this IVoidOverloadSlot8<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        where TArgs : struct
        => self.VoidSlot8Interceptor.When(predicate);
}
