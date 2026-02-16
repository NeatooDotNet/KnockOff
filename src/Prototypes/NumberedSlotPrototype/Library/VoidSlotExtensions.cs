namespace NumberedSlotPrototype.Library;

/// <summary>
/// Extension methods for void overload slots.
/// Each slot gets its own set of Call/When/Verify extension methods.
/// The C# compiler resolves the correct slot based on the delegate type.
/// </summary>
public static class VoidSlotExtensions
{
    // ---- Slot 1 ----

    public static string Call<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.VoidSlot1Interceptor.Call(callback);

    public static string When<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.VoidSlot1Interceptor.When(args);

    public static string Verify<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self)
        where TDelegate : Delegate
        => self.VoidSlot1Interceptor.Verify();

    // ---- Slot 2 ----

    public static string Call<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.VoidSlot2Interceptor.Call(callback);

    public static string When<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.VoidSlot2Interceptor.When(args);

    public static string Verify<TDelegate, TArgs>(
        this IVoidOverloadSlot2<TDelegate, TArgs> self)
        where TDelegate : Delegate
        => self.VoidSlot2Interceptor.Verify();

    // ---- Slot 3 ----

    public static string Call<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.VoidSlot3Interceptor.Call(callback);

    public static string When<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.VoidSlot3Interceptor.When(args);

    public static string Verify<TDelegate, TArgs>(
        this IVoidOverloadSlot3<TDelegate, TArgs> self)
        where TDelegate : Delegate
        => self.VoidSlot3Interceptor.Verify();

    // ---- Slot 4 ----

    public static string Call<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.VoidSlot4Interceptor.Call(callback);

    public static string When<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.VoidSlot4Interceptor.When(args);

    public static string Verify<TDelegate, TArgs>(
        this IVoidOverloadSlot4<TDelegate, TArgs> self)
        where TDelegate : Delegate
        => self.VoidSlot4Interceptor.Verify();
}
