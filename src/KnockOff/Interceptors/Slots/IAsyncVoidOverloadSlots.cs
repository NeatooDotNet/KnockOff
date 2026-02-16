namespace KnockOff.Interceptors;

/// <summary>
/// Numbered slot interfaces for async void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
/// </summary>

public interface IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot1Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot2<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot2Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot3<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot3Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot4<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot4Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot5<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot5Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot6<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot6Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot7<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot7Interceptor { get; }
}

public interface IAsyncVoidOverloadSlot8<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot8Interceptor { get; }
}
