// -----------------------------------------------------------------------------
// Design.Tests - Ref/Out Parameter Tests
// -----------------------------------------------------------------------------
// Tests verifying ref/out parameter behavior across inline and standalone
// patterns. The generator creates custom delegates for ref/out methods,
// enabling full configuration via Return(delegate) and Call(delegate).
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Methods;
using Design.Stubs.StubPatterns;
using KnockOff;

namespace Design.Tests.MethodTests;

public class RefOutParameterTests
{
    // =========================================================================
    // OUT PARAMETERS: TryGet Pattern
    // =========================================================================

    [Fact]
    public void OutParameter_TryGet_ReturnsTrue_SetsOutValue()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = "found-" + key;
            return true;
        });

        IOutParameterService service = stub;
        var found = service.TryGetValue("test", out string result);

        Assert.True(found);
        Assert.Equal("found-test", result);
    }

    [Fact]
    public void OutParameter_TryGet_ReturnsFalse_SetsOutDefault()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = default!;
            return false;
        });

        IOutParameterService service = stub;
        var found = service.TryGetValue("missing", out string result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void OutParameter_TryGet_BackedByDictionary()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();
        var data = new Dictionary<string, string> { ["key1"] = "value1" };

        stub.TryGetValue.Return((string key, out string value) =>
            data.TryGetValue(key, out value!));

        IOutParameterService service = stub;

        Assert.True(service.TryGetValue("key1", out var v1));
        Assert.Equal("value1", v1);

        Assert.False(service.TryGetValue("key2", out var v2));
        Assert.Null(v2);
    }

    // =========================================================================
    // OUT PARAMETERS: Void Method with Multiple Outs
    // =========================================================================

    [Fact]
    public void OutParameter_VoidMethod_MultiplOuts()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.GetDefaults.Call((out int w, out int h) =>
        {
            w = 1920;
            h = 1080;
        });

        IOutParameterService service = stub;
        service.GetDefaults(out int width, out int height);

        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    // =========================================================================
    // OUT PARAMETERS: Return Value + Out
    // =========================================================================

    [Fact]
    public void OutParameter_ReturnValueWithOut_Success()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

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
        var result = service.Parse("42", out var err);

        Assert.Equal(42, result);
        Assert.Null(err);
    }

    [Fact]
    public void OutParameter_ReturnValueWithOut_Failure()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

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
        var result = service.Parse("abc", out var err);

        Assert.Equal(-1, result);
        Assert.Equal("Cannot parse 'abc'", err);
    }

    // =========================================================================
    // REF PARAMETERS
    // =========================================================================

    [Fact]
    public void RefParameter_VoidMethod_ModifiesRefValue()
    {
        var stub = new RefOutDemo.Stubs.IRefParameterService();

        stub.Increment.Call((ref int v) => v++);

        IRefParameterService service = stub;
        int value = 5;
        service.Increment(ref value);

        Assert.Equal(6, value);
    }

    [Fact]
    public void RefParameter_WithReturnValue_ModifiesRefAndReturns()
    {
        var stub = new RefOutDemo.Stubs.IRefParameterService();

        stub.TryUpdate.Return((string key, ref string value) =>
        {
            value = value.ToUpperInvariant();
            return true;
        });

        IRefParameterService service = stub;
        string val = "hello";
        var result = service.TryUpdate("key", ref val);

        Assert.True(result);
        Assert.Equal("HELLO", val);
    }

    [Fact]
    public void RefParameter_Swap_ExchangesValues()
    {
        var stub = new RefOutDemo.Stubs.IRefParameterService();

        stub.Swap.Call((ref int a, ref int b) => (a, b) = (b, a));

        IRefParameterService service = stub;
        int x = 1, y = 2;
        service.Swap(ref x, ref y);

        Assert.Equal(2, x);
        Assert.Equal(1, y);
    }

    // =========================================================================
    // MIXED: Normal + Out + Ref
    // =========================================================================

    [Fact]
    public void MixedRefOut_AllParameterKinds()
    {
        var stub = new RefOutDemo.Stubs.IMixedRefOutService();

        stub.Process.Return((string input, out string output, ref int counter) =>
        {
            output = input.ToUpperInvariant();
            counter++;
            return true;
        });

        IMixedRefOutService service = stub;
        int counter = 10;
        var result = service.Process("hello", out string output, ref counter);

        Assert.True(result);
        Assert.Equal("HELLO", output);
        Assert.Equal(11, counter);
    }

    [Fact]
    public void MixedRefOut_MultipleOuts()
    {
        var stub = new RefOutDemo.Stubs.IMixedRefOutService();

        stub.Transform.Return((string input, out string transformed, out bool wasModified) =>
        {
            transformed = input.Trim();
            wasModified = transformed != input;
            return transformed.Length;
        });

        IMixedRefOutService service = stub;
        var len = service.Transform("  hi  ", out string transformed, out bool wasModified);

        Assert.Equal(2, len);
        Assert.Equal("hi", transformed);
        Assert.True(wasModified);
    }

    // =========================================================================
    // VERIFICATION: Call counting works with ref/out
    // =========================================================================

    [Fact]
    public void RefOut_Verify_TracksCallCount()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

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

    [Fact]
    public void RefOut_Verify_ThrowsWhenNotSatisfied()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = default!;
            return false;
        });

        IOutParameterService service = stub;
        service.TryGetValue("key1", out _);

        Assert.Throws<VerificationException>(() =>
            stub.TryGetValue.Verify(Times.Exactly(2)));
    }

    // =========================================================================
    // LASTARG: Captures non-ref/out parameters only
    // =========================================================================

    [Fact]
    public void OutParameter_LastArg_CapturesNonOutOnly()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = "found";
            return true;
        });

        IOutParameterService service = stub;
        service.TryGetValue("myKey", out _);

        Assert.Equal("myKey", stub.TryGetValue.LastArg);
    }

    // =========================================================================
    // UNCONFIGURED BEHAVIOR
    // =========================================================================

    [Fact]
    public void OutParameter_Unconfigured_ReturnsDefault()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        IOutParameterService service = stub;
        var found = service.TryGetValue("key", out string value);

        Assert.False(found); // default(bool)
        Assert.Null(value);  // default(string) via @value = default!
    }

    [Fact]
    public void RefParameter_Unconfigured_LeavesRefUnchanged()
    {
        var stub = new RefOutDemo.Stubs.IRefParameterService();

        IRefParameterService service = stub;
        int value = 42;
        service.Increment(ref value);

        Assert.Equal(42, value); // unchanged — no delegate to modify it
    }

    // =========================================================================
    // SEQUENCES: ref/out methods support ThenReturn/ThenCall
    // =========================================================================

    [Fact]
    public void OutParameter_Sequence_ThenReturn()
    {
        var stub = new RefOutDemo.Stubs.IOutParameterService();

        stub.TryGetValue
            .Return((string key, out string value) => { value = "first"; return true; })
            .ThenReturn((string key, out string value) => { value = "second"; return true; });

        IOutParameterService service = stub;
        service.TryGetValue("a", out var v1);
        service.TryGetValue("b", out var v2);
        service.TryGetValue("c", out var v3); // repeats last

        Assert.Equal("first", v1);
        Assert.Equal("second", v2);
        Assert.Equal("second", v3);
    }

    [Fact]
    public void RefParameter_Sequence_ThenCall()
    {
        var stub = new RefOutDemo.Stubs.IRefParameterService();

        stub.Increment
            .Call((ref int v) => v += 10)
            .ThenCall((ref int v) => v += 100);

        IRefParameterService service = stub;
        int a = 0;
        service.Increment(ref a);
        int b = 0;
        service.Increment(ref b);

        Assert.Equal(10, a);
        Assert.Equal(100, b);
    }

    // =========================================================================
    // STANDALONE PATTERN: Same behavior as inline
    // =========================================================================

    [Fact]
    public void Standalone_OutParameter_WorksIdentically()
    {
        var stub = new OutParameterStub();

        stub.TryGetValue.Return((string key, out string value) =>
        {
            value = "standalone-" + key;
            return true;
        });

        IOutParameterService service = stub;
        var found = service.TryGetValue("test", out string result);

        Assert.True(found);
        Assert.Equal("standalone-test", result);
    }

    [Fact]
    public void Standalone_RefParameter_WorksIdentically()
    {
        var stub = new RefParameterStub();

        stub.Increment.Call((ref int v) => v *= 2);

        IRefParameterService service = stub;
        int value = 5;
        service.Increment(ref value);

        Assert.Equal(10, value);
    }

    [Fact]
    public void Standalone_MixedRefOut_WorksIdentically()
    {
        var stub = new MixedRefOutStub();

        stub.Process.Return((string input, out string output, ref int counter) =>
        {
            output = input + "!";
            counter += 10;
            return true;
        });

        IMixedRefOutService service = stub;
        int counter = 0;
        var result = service.Process("hi", out string output, ref counter);

        Assert.True(result);
        Assert.Equal("hi!", output);
        Assert.Equal(10, counter);
    }
}
