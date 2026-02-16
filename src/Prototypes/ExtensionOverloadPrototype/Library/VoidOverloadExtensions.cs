namespace ExtensionOverloadPrototype.Library;

/// <summary>
/// Extension methods for IVoidOverload that provide Call and When APIs.
/// These are library-defined (pre-compiled) and do not need to be generated per-stub.
/// </summary>
public static class VoidOverloadExtensions
{
    /// <summary>
    /// Configures a callback for this void method overload.
    /// The compiler resolves the correct overload based on the delegate signature.
    /// </summary>
    /// <typeparam name="TDelegate">Inferred from the lambda/delegate passed</typeparam>
    /// <typeparam name="TArgs">Inferred from the interface implementation</typeparam>
    /// <param name="self">The compositor cast to the matching interface</param>
    /// <param name="callback">The callback to invoke when the method is called</param>
    /// <returns>Description of what was configured (for prototype verification)</returns>
    public static string Call<TDelegate, TArgs>(
        this IVoidOverload<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => $"VoidCall:{typeof(TDelegate).Name}";

    /// <summary>
    /// Configures a When match using specific argument values.
    /// For single-arg methods, TArgs is the arg type directly.
    /// For multi-arg methods, TArgs is a ValueTuple.
    /// </summary>
    public static string When<TDelegate, TArgs>(
        this IVoidOverload<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => $"VoidWhen:{typeof(TArgs).Name}";

    /// <summary>
    /// Configures a When match using a predicate on the arguments.
    /// </summary>
    public static string WhenPredicate<TDelegate, TArgs>(
        this IVoidOverload<TDelegate, TArgs> self, Func<TArgs, bool> predicate)
        where TDelegate : Delegate
        => $"VoidWhenPredicate:{typeof(TArgs).Name}";
}
