namespace KnockOff.Interceptors;

/// <summary>
/// Numbered slot interfaces for async non-void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
/// </summary>

public interface IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot1Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot2<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot2Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot3<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot3Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot4<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot4Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot5<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot5Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot6<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot6Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot7<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot7Interceptor { get; }
}

public interface IAsyncMethodOverloadSlot8<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    where TArgs : struct
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot8Interceptor { get; }
}
