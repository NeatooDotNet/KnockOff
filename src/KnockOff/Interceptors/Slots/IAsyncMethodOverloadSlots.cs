namespace KnockOff.Interceptors;

/// <summary>
/// Numbered slot interfaces for async non-void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
/// </summary>

public interface IAsyncMethodOverloadSlot1<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot1Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot2<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot2Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot3<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot3Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot4<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot4Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot5<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot5Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot6<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot6Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot7<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot7Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot8<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot8Interceptor { get; }
}
