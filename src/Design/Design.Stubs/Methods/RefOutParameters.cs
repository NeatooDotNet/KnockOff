// -----------------------------------------------------------------------------
// Design.Stubs - Ref/Out Parameter Behavior
// -----------------------------------------------------------------------------
// This file documents how KnockOff handles ref and out parameters.
//
// GENERATOR BEHAVIOR: For methods with ref/out parameters, the generator
// creates custom delegates instead of using Func<>/Action<>:
//
//   public delegate bool TryGetValueDelegate(string key, out string value);
//   public delegate void IncrementDelegate(ref int value);
//   public delegate void SwapDelegate(ref int a, ref int b);
//
// The Return() and Call() APIs accept these custom delegates instead of
// Func<>/Action<>. There is NO Return(value) overload for ref/out methods.
//
// CONSEQUENCE: You must always provide a delegate that handles the ref/out
// parameters. There is no "just return true" shortcut for TryGet patterns.
//
// LASTARG/LASTARGS: Only non-ref/non-out parameters are captured.
// Ref/out parameters are excluded from argument tracking.
// -----------------------------------------------------------------------------

using System.Globalization;
using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// INLINE PATTERN: ref/out parameters
// =============================================================================

[KnockOff<IOutParameterService>]
[KnockOff<IRefParameterService>]
[KnockOff<IMixedRefOutService>]
public partial class RefOutDemo
{
    // =========================================================================
    // OUT PARAMETERS: TryGet Pattern
    // =========================================================================
    // DESIGN DECISION: Return() accepts a custom delegate, not a value.
    //
    // GENERATOR BEHAVIOR:
    //   public delegate bool TryGetValueDelegate(string key, out string value);
    //   public MethodCallBuilderImpl Return(TryGetValueDelegate callback)
    //
    // There is no Return(bool) overload. The delegate must set the out param.
    // =========================================================================

    public void OutParameter_TryGetPattern_ReturnTrue()
    {
        var stub = new Stubs.IOutParameterService();

        // Must provide delegate that handles both return value AND out parameter
        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = "found-" + key;
            return true;
        });

        IOutParameterService service = stub;
        service.TryGetValue("myKey", out string result);
        // result == "found-myKey"
    }

    public void OutParameter_TryGetPattern_ReturnFalse()
    {
        var stub = new Stubs.IOutParameterService();

        // Simulate "not found" - must still set the out parameter
        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = default!;
            return false;
        });

        IOutParameterService service = stub;
        var found = service.TryGetValue("missing", out string result);
        // found == false, result == null
    }

    // =========================================================================
    // OUT PARAMETERS: Backed by Dictionary (Realistic Scenario)
    // =========================================================================

    public void OutParameter_TryGetPattern_BackedByDictionary()
    {
        var stub = new Stubs.IOutParameterService();
        var data = new Dictionary<string, string> { ["key1"] = "value1" };

        stub.TryGetValue.Return((string key, out string value) =>
            data.TryGetValue(key, out value!));

        IOutParameterService service = stub;
        service.TryGetValue("key1", out var v1); // v1 == "value1"
        service.TryGetValue("key2", out var v2); // v2 == null
    }

    // =========================================================================
    // OUT PARAMETERS: Void Method with Multiple Outs
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate void GetDefaultsDelegate(out int width, out int height);
    //   public MethodCallBuilderImpl Call(GetDefaultsDelegate callback)
    // =========================================================================

    public void OutParameter_VoidMethodWithMultipleOuts()
    {
        var stub = new Stubs.IOutParameterService();

        stub.GetDefaults.Call((out int w, out int h) =>
        {
            w = 1920;
            h = 1080;
        });

        IOutParameterService service = stub;
        service.GetDefaults(out int width, out int height);
        // width == 1920, height == 1080
    }

    // =========================================================================
    // OUT PARAMETERS: Return Value + Out Parameter
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate int ParseDelegate(string input, out string error);
    //   public MethodCallBuilderImpl Return(ParseDelegate callback)
    // =========================================================================

    public void OutParameter_ReturnValueWithOut()
    {
        var stub = new Stubs.IOutParameterService();

        stub.Parse.Return((string input, out string error) =>
        {
            if (int.TryParse(input, out var parsed))
            {
                error = null!;
                return parsed;
            }
            error = $"Cannot parse '{input}'";
            return -1;
        });

        IOutParameterService service = stub;
        var result = service.Parse("42", out var err1);   // result == 42, err1 == null
        var bad = service.Parse("abc", out var err2);     // bad == -1, err2 == "Cannot parse 'abc'"
    }

    // =========================================================================
    // REF PARAMETERS: Simple Ref (Void)
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate void IncrementDelegate(ref int value);
    //   public MethodCallBuilderImpl Call(IncrementDelegate callback)
    // =========================================================================

    public void RefParameter_SimpleRef()
    {
        var stub = new Stubs.IRefParameterService();

        stub.Increment.Call((ref int v) => v++);

        IRefParameterService service = stub;
        int value = 5;
        service.Increment(ref value);
        // value == 6
    }

    // =========================================================================
    // REF PARAMETERS: Ref with Return Value
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate bool TryUpdateDelegate(string key, ref string value);
    //   public MethodCallBuilderImpl Return(TryUpdateDelegate callback)
    // =========================================================================

    public void RefParameter_WithReturnValue()
    {
        var stub = new Stubs.IRefParameterService();

        stub.TryUpdate.Return((string key, ref string value) =>
        {
            value = value.ToUpperInvariant();
            return true;
        });

        IRefParameterService service = stub;
        string val = "hello";
        var result = service.TryUpdate("key", ref val);
        // result == true, val == "HELLO"
    }

    // =========================================================================
    // REF PARAMETERS: Multiple Refs (Swap)
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate void SwapDelegate(ref int a, ref int b);
    //   public MethodCallBuilderImpl Call(SwapDelegate callback)
    // =========================================================================

    public void RefParameter_MultipleRefs_Swap()
    {
        var stub = new Stubs.IRefParameterService();

        stub.Swap.Call((ref int a, ref int b) => (a, b) = (b, a));

        IRefParameterService service = stub;
        int x = 1, y = 2;
        service.Swap(ref x, ref y);
        // x == 2, y == 1
    }

    // =========================================================================
    // MIXED: Normal + Out + Ref Parameters
    // =========================================================================
    // GENERATOR BEHAVIOR:
    //   public delegate bool ProcessDelegate(string input, out string output, ref int counter);
    //   public MethodCallBuilderImpl Return(ProcessDelegate callback)
    // =========================================================================

    public void MixedRefOut_AllParameterKinds()
    {
        var stub = new Stubs.IMixedRefOutService();

        stub.Process.Return((string input, out string output, ref int counter) =>
        {
            output = input.ToUpperInvariant();
            counter++;
            return true;
        });

        IMixedRefOutService service = stub;
        int counter = 10;
        var result = service.Process("hello", out string output, ref counter);
        // result == true, output == "HELLO", counter == 11
    }

    public void MixedRefOut_MultipleOuts()
    {
        var stub = new Stubs.IMixedRefOutService();

        stub.Transform.Return((string input, out string transformed, out bool wasModified) =>
        {
            transformed = input.Trim();
            wasModified = transformed != input;
            return transformed.Length;
        });

        IMixedRefOutService service = stub;
        var len = service.Transform("  hi  ", out string transformed, out bool wasModified);
        // len == 2, transformed == "hi", wasModified == true
    }

    // =========================================================================
    // VERIFICATION: Call counting works with ref/out methods
    // =========================================================================
    // Verify() and call counting work normally regardless of ref/out parameters.
    // =========================================================================

    public void RefOut_Verification()
    {
        var stub = new Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = default!;
            return false;
        });

        IOutParameterService service = stub;
        service.TryGetValue("key1", out _);
        service.TryGetValue("key2", out _);

        stub.TryGetValue.Verify(Times.Exactly(2));
    }

    // =========================================================================
    // LASTARG: Only non-ref/out parameters are captured
    // =========================================================================
    // DESIGN DECISION: LastArg/LastArgs exclude ref/out parameters.
    // Only regular (by-value) parameters are tracked.
    //
    // For TryGetValue(string key, out string value):
    //   LastArg captures 'key' only (string), not 'value'
    //
    // For TryUpdate(string key, ref string value):
    //   LastArgs captures (key, value) as a tuple — but 'value' is the
    //   value AT CALL TIME, not after the delegate modifies it
    //
    // For Increment(ref int value):
    //   LastArg captures 'value' AT CALL TIME
    // =========================================================================

    public void OutParameter_LastArg_CapturesNonOutOnly()
    {
        var stub = new Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = "found";
            return true;
        });

        IOutParameterService service = stub;
        service.TryGetValue("myKey", out _);

        // LastArg only captures the non-out parameter 'key'
        var lastKey = stub.TryGetValue.LastArg;
        // lastKey == "myKey"
    }

    public void RefParameter_LastArgs_CapturesRefValue()
    {
        var stub = new Stubs.IRefParameterService();

        stub.TryUpdate.Return((string key, ref string value) =>
        {
            value = value.ToUpperInvariant();
            return true;
        });

        IRefParameterService service = stub;
        string val = "hello";
        service.TryUpdate("key", ref val);

        // LastArgs captures both parameters
        var args = stub.TryUpdate.LastArgs;
        // args == ("key", "hello") — captures value AT CALL TIME
    }

    // =========================================================================
    // UNCONFIGURED BEHAVIOR: ref/out params get default values
    // =========================================================================
    // When no delegate is configured (loose mode), ref params are unchanged
    // and out params get default(T). The return value is default(T).
    // =========================================================================

    public void OutParameter_Unconfigured_ReturnsDefaults()
    {
        var stub = new Stubs.IOutParameterService();

        // No configuration — loose mode defaults
        IOutParameterService service = stub;
        var found = service.TryGetValue("key", out string value);
        // found == false (default bool)
        // value == null (default string, set by generated Invoke: @value = default!)
    }

    public void RefParameter_Unconfigured_RefUnchanged()
    {
        var stub = new Stubs.IRefParameterService();

        // No configuration
        IRefParameterService service = stub;
        int value = 42;
        service.Increment(ref value);
        // value == 42 (unchanged — no delegate to modify it)
    }

    // =========================================================================
    // SEQUENCES: ref/out methods support ThenReturn/ThenCall
    // =========================================================================

    public void OutParameter_Sequence()
    {
        var stub = new Stubs.IOutParameterService();

        stub.TryGetValue
            .Return((string key, out string value) => { value = "first"; return true; })
            .ThenReturn((string key, out string value) => { value = "second"; return true; });

        IOutParameterService service = stub;
        service.TryGetValue("a", out var v1); // v1 == "first"
        service.TryGetValue("b", out var v2); // v2 == "second"
        service.TryGetValue("c", out var v3); // v3 == "second" (repeats last)
    }

    public void RefParameter_Sequence()
    {
        var stub = new Stubs.IRefParameterService();

        stub.Increment
            .Call((ref int v) => v += 10)
            .ThenCall((ref int v) => v += 100);

        IRefParameterService service = stub;
        int a = 0;
        service.Increment(ref a); // a == 10
        int b = 0;
        service.Increment(ref b); // b == 100
    }
}
