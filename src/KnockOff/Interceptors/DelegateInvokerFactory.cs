#pragma warning disable CA1062 // Validate arguments of public methods

using System.Linq.Expressions;
using System.Reflection;

namespace KnockOff.Interceptors;

/// <summary>
/// Builds and caches expression tree invokers for bridging between TDelegate and TArgs.
/// Used by TTuple interceptor types to invoke user-supplied delegates with args unpacked
/// from a ValueTuple (2+ params) or passed directly (1 param).
/// </summary>
internal static class DelegateInvokerFactory
{
    /// <summary>
    /// Sync non-void: builds <c>(del, args) => del(args.Item1, args.Item2, ...) : TReturn</c>.
    /// For 1-param delegates, TArgs is the raw type and is passed directly.
    /// For 2+ param delegates, TArgs is a ValueTuple and fields are extracted.
    /// </summary>
    public static Func<TDelegate, TArgs, TReturn> BuildInvoker<TDelegate, TArgs, TReturn>()
        where TDelegate : Delegate
    {
        return BuildInvokerCore<TDelegate, TArgs, TReturn>();
    }

    /// <summary>
    /// Sync void: builds <c>(del, args) => del(args.Item1, args.Item2, ...)</c>.
    /// Returns an Action since TDelegate returns void.
    /// </summary>
    public static Action<TDelegate, TArgs> BuildVoidInvoker<TDelegate, TArgs>()
        where TDelegate : Delegate
    {
        return BuildVoidInvokerCore<TDelegate, TArgs>();
    }

    /// <summary>
    /// Async non-void: builds <c>(del, args) => del(args.Item1, args.Item2, ...) : Task&lt;TReturn&gt;</c>.
    /// TReturn is the inner type (e.g., int for Task&lt;int&gt;). The delegate itself returns Task&lt;TReturn&gt;.
    /// The expression tree is identical to BuildInvoker -- only the type parameters differ.
    /// </summary>
    public static Func<TDelegate, TArgs, Task<TReturn>> BuildAsyncInvoker<TDelegate, TArgs, TReturn>()
        where TDelegate : Delegate
    {
        return BuildInvokerCore<TDelegate, TArgs, Task<TReturn>>();
    }

    /// <summary>
    /// Async void: builds <c>(del, args) => del(args.Item1, args.Item2, ...) : Task</c>.
    /// The delegate itself returns Task. The expression tree is identical to BuildInvoker.
    /// </summary>
    public static Func<TDelegate, TArgs, Task> BuildAsyncVoidInvoker<TDelegate, TArgs>()
        where TDelegate : Delegate
    {
        return BuildInvokerCore<TDelegate, TArgs, Task>();
    }

    /// <summary>
    /// Value delegate: creates a factory <c>(value) => new TDelegate((args...) => value)</c>.
    /// The returned TDelegate ignores all arguments and returns the fixed value.
    /// Each call to the returned Func compiles a small expression tree.
    /// This is acceptable because it is called once per Return(value) setup, not per test invocation.
    /// </summary>
    public static Func<TReturn, TDelegate> BuildValueDelegate<TDelegate, TReturn>()
        where TDelegate : Delegate
    {
        var invokeMethod = typeof(TDelegate).GetMethod("Invoke")!;
        var delegateParams = invokeMethod.GetParameters();

        return (TReturn value) =>
        {
            var paramExprs = new ParameterExpression[delegateParams.Length];
            for (int i = 0; i < delegateParams.Length; i++)
            {
                paramExprs[i] = Expression.Parameter(delegateParams[i].ParameterType, delegateParams[i].Name);
            }

            var body = Expression.Constant(value, typeof(TReturn));

            return Expression.Lambda<TDelegate>(body, paramExprs).Compile();
        };
    }

    /// <summary>
    /// Builds expressions to extract individual arguments from TArgs.
    /// For 1 param: TArgs is the raw type, passed directly.
    /// For 2-7 params: TArgs is ValueTuple, access .Item1 through .Item7.
    /// For 8 params: TArgs is ValueTuple with nested Rest, access .Item1-.Item7 and .Rest.Item1.
    /// </summary>
    private static Expression[] BuildArgExpressions(ParameterExpression argsParam, int paramCount)
    {
        var argExprs = new Expression[paramCount];

        if (paramCount == 1)
        {
            // TArgs is the raw type -- just pass it directly
            argExprs[0] = argsParam;
        }
        else
        {
            // TArgs is ValueTuple -- access .Item1, .Item2, etc.
            // For 8 params, ValueTuple nests: the 8th element is .Rest.Item1
            var directFields = Math.Min(paramCount, 7);
            for (int i = 0; i < directFields; i++)
            {
                argExprs[i] = Expression.Field(argsParam, $"Item{i + 1}");
            }

            // Handle the 8th parameter via .Rest.Item1
            if (paramCount == 8)
            {
                var restField = Expression.Field(argsParam, "Rest");
                argExprs[7] = Expression.Field(restField, "Item1");
            }
        }

        return argExprs;
    }

    /// <summary>
    /// Core invoker builder shared by sync, async, and async-void variants.
    /// Builds: <c>(TDelegate del, TArgs args) => del(unpack(args))</c>
    /// where unpack extracts ValueTuple fields for 2+ params or passes args directly for 1 param.
    /// </summary>
    private static Func<TDelegate, TArgs, TResult> BuildInvokerCore<TDelegate, TArgs, TResult>()
        where TDelegate : Delegate
    {
        var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke")!;
        var paramCount = delegateInvokeMethod.GetParameters().Length;

        // Parameters for the outer lambda: (TDelegate del, TArgs args)
        var delParam = Expression.Parameter(typeof(TDelegate), "del");
        var argsParam = Expression.Parameter(typeof(TArgs), "args");

        // Build argument expressions by extracting from TArgs
        var argExprs = BuildArgExpressions(argsParam, paramCount);

        // Build: del.Invoke(arg1, arg2, ...)
        var invokeExpr = Expression.Invoke(delParam, argExprs);

        // Compile to Func<TDelegate, TArgs, TResult>
        return Expression.Lambda<Func<TDelegate, TArgs, TResult>>(
            invokeExpr, delParam, argsParam).Compile();
    }

    /// <summary>
    /// Core void invoker builder. Builds: <c>(TDelegate del, TArgs args) => del(unpack(args))</c>
    /// where the delegate returns void.
    /// </summary>
    private static Action<TDelegate, TArgs> BuildVoidInvokerCore<TDelegate, TArgs>()
        where TDelegate : Delegate
    {
        var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke")!;
        var paramCount = delegateInvokeMethod.GetParameters().Length;

        // Parameters for the outer lambda: (TDelegate del, TArgs args)
        var delParam = Expression.Parameter(typeof(TDelegate), "del");
        var argsParam = Expression.Parameter(typeof(TArgs), "args");

        // Build argument expressions by extracting from TArgs
        var argExprs = BuildArgExpressions(argsParam, paramCount);

        // Build: del.Invoke(arg1, arg2, ...)
        var invokeExpr = Expression.Invoke(delParam, argExprs);

        // Compile to Action<TDelegate, TArgs>
        return Expression.Lambda<Action<TDelegate, TArgs>>(
            invokeExpr, delParam, argsParam).Compile();
    }
}
