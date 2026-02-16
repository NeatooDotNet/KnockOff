namespace ExtensionOverloadPrototype.Library;

/// <summary>
/// Interface representing a non-void method overload in a compositor.
/// TDelegate is the delegate type matching the method signature (including return type).
/// TArgs is the argument tuple type (or single type for 1-arg methods).
/// TReturn is the return type.
/// </summary>
/// <typeparam name="TDelegate">Delegate type for Return callbacks</typeparam>
/// <typeparam name="TArgs">Argument type (single value or tuple) for When matching</typeparam>
/// <typeparam name="TReturn">Return type of the method</typeparam>
public interface IMethodOverload<TDelegate, TArgs, TReturn> where TDelegate : Delegate
{
    /// <summary>
    /// Returns the interceptor backing this overload.
    /// </summary>
    object GetInterceptor();
}
