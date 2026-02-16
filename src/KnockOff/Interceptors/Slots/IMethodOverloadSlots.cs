namespace KnockOff.Interceptors;

/// <summary>
/// Numbered slot interfaces for non-void method overloads.
/// Each slot is a distinct interface type. A compositor implements at most one
/// of each slot number, so the C# compiler can unambiguously resolve extension methods.
/// </summary>

public interface IMethodOverloadSlot1<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot1Interceptor { get; }
}

public interface IMethodOverloadSlot2<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot2Interceptor { get; }
}

public interface IMethodOverloadSlot3<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot3Interceptor { get; }
}

public interface IMethodOverloadSlot4<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot4Interceptor { get; }
}

public interface IMethodOverloadSlot5<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot5Interceptor { get; }
}

public interface IMethodOverloadSlot6<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot6Interceptor { get; }
}

public interface IMethodOverloadSlot7<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot7Interceptor { get; }
}

public interface IMethodOverloadSlot8<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TArgs : struct
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot8Interceptor { get; }
}
