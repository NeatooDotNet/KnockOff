namespace ExtensionOverloadPrototype.Library;

/// <summary>
/// Interface representing a void method overload in a compositor.
/// TDelegate is the delegate type matching the method signature.
/// TArgs is the argument tuple type (or single type for 1-arg methods).
/// </summary>
/// <typeparam name="TDelegate">Delegate type for Call callbacks</typeparam>
/// <typeparam name="TArgs">Argument type (single value or tuple) for When matching</typeparam>
public interface IVoidOverload<TDelegate, TArgs> where TDelegate : Delegate
{
    /// <summary>
    /// Returns the interceptor backing this overload.
    /// In real KnockOff, this would return the actual interceptor instance.
    /// </summary>
    object GetInterceptor();
}
