namespace KnockOff.Interceptors;

/// <summary>
/// Numbered slot interfaces for async void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
/// </summary>

public interface IAsyncVoidOverloadSlot1<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot1Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot2<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot2Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot3<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot3Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot4<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot4Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot5<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot5Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot6<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot6Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot7<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot7Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot8<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot8Interceptor { get; }
}
