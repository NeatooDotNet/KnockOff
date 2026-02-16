namespace ExtensionOverloadPrototype.Library;

/// <summary>
/// Extension methods for IMethodOverload that provide Return and When APIs.
/// </summary>
public static class MethodOverloadExtensions
{
    /// <summary>
    /// Configures a return callback for this non-void method overload.
    /// </summary>
    public static string Return<TDelegate, TArgs, TReturn>(
        this IMethodOverload<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => $"MethodReturn:{typeof(TDelegate).Name}";

    /// <summary>
    /// Configures a fixed return value for this overload.
    /// NOTE: This takes TReturn, which could cause ambiguity when multiple overloads
    /// share the same return type. This is a key edge case to test.
    /// </summary>
    public static string ReturnValue<TDelegate, TArgs, TReturn>(
        this IMethodOverload<TDelegate, TArgs, TReturn> self, TReturn value)
        where TDelegate : Delegate
        => $"MethodReturnValue:{typeof(TReturn).Name}={value}";

    /// <summary>
    /// Configures a When match using specific argument values.
    /// </summary>
    public static string When<TDelegate, TArgs, TReturn>(
        this IMethodOverload<TDelegate, TArgs, TReturn> self, TArgs args)
        where TDelegate : Delegate
        => $"MethodWhen:{typeof(TArgs).Name}";

    /// <summary>
    /// Configures a When match using a predicate on the arguments.
    /// </summary>
    public static string WhenPredicate<TDelegate, TArgs, TReturn>(
        this IMethodOverload<TDelegate, TArgs, TReturn> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => $"MethodWhenPredicate:{typeof(TArgs).Name}";

    /// <summary>
    /// Configures a Call callback for this non-void method overload.
    /// (Some users prefer Call over Return for non-void methods.)
    /// </summary>
    public static string Call<TDelegate, TArgs, TReturn>(
        this IMethodOverload<TDelegate, TArgs, TReturn> self, TDelegate callback)
        where TDelegate : Delegate
        => $"MethodCall:{typeof(TDelegate).Name}";
}
