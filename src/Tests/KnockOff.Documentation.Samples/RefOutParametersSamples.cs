using KnockOff;

namespace KnockOff.Documentation.Samples.RefOutParameters;

// =============================================================================
// Interface definitions for ref/out parameter samples
// =============================================================================

#region refout-interfaces
public interface IOutParameterService
{
    bool TryGetValue(string key, out string value);
    void GetDefaults(out int width, out int height);
}

public interface IRefParameterService
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
    void Swap(ref int a, ref int b);
}
#endregion

public interface IMixedRefOutService
{
    bool Process(string input, out string output, ref int counter);
}

#region refout-simple-interface
public interface IRefOutService
{
    void RefArgument(ref int a);
    void OutArgument(out int a);
    void InArgument(in int a);
}
#endregion

public interface IGenericRefOutService
{
    void OutArgumentsWithGenerics<T1, T2>(T1 a, out T2 b);
}

// =============================================================================
// Inline stubs
// =============================================================================

[KnockOff<IOutParameterService>]
[KnockOff<IRefParameterService>]
[KnockOff<IMixedRefOutService>]
[KnockOff<IRefOutService>]
[KnockOff<IGenericRefOutService>]
public partial class RefOutTests { }

public partial class RefOutTests
{
    // =========================================================================
    // Out parameter: TryGet pattern
    // =========================================================================

    [Fact]
    public void OutParameter_TryGetPattern()
    {
        #region refout-tryget
        var stub = new Stubs.IOutParameterService();

        stub.TryGetValue.Call((string key, out string value) =>
        {
            value = "found-" + key;
            return true;
        });

        IOutParameterService service = stub;
        service.TryGetValue("myKey", out string result);
        // result == "found-myKey"
        #endregion

        Assert.Equal("found-myKey", result);
    }

    // =========================================================================
    // Out parameter: Backed by dictionary
    // =========================================================================

    [Fact]
    public void OutParameter_BackedByDictionary()
    {
        var stub = new Stubs.IOutParameterService();

        #region refout-dictionary
        var data = new Dictionary<string, string> { ["key1"] = "value1" };

        stub.TryGetValue.Call((string key, out string value) =>
            data.TryGetValue(key, out value!));
        #endregion

        IOutParameterService service = stub;

        Assert.True(service.TryGetValue("key1", out var v1));
        Assert.Equal("value1", v1);

        Assert.False(service.TryGetValue("key2", out var v2));
        Assert.Null(v2);
    }

    // =========================================================================
    // Out parameter: Void method with multiple outs
    // =========================================================================

    [Fact]
    public void OutParameter_VoidWithMultipleOuts()
    {
        var stub = new Stubs.IOutParameterService();

        #region refout-void-multiple-outs
        stub.GetDefaults.Call((out int w, out int h) =>
        {
            w = 1920;
            h = 1080;
        });
        #endregion

        IOutParameterService service = stub;
        service.GetDefaults(out int width, out int height);

        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    // =========================================================================
    // Ref parameter: Simple
    // =========================================================================

    [Fact]
    public void RefParameter_Simple()
    {
        #region refout-ref-simple
        var stub = new Stubs.IRefParameterService();

        stub.Increment.Call((ref int v) => v++);

        IRefParameterService service = stub;
        int value = 5;
        service.Increment(ref value);
        // value == 6
        #endregion

        Assert.Equal(6, value);
    }

    // =========================================================================
    // Ref parameter: Swap
    // =========================================================================

    [Fact]
    public void RefParameter_Swap()
    {
        var stub = new Stubs.IRefParameterService();

        #region refout-ref-swap
        stub.Swap.Call((ref int a, ref int b) => (a, b) = (b, a));
        #endregion

        IRefParameterService service = stub;
        int x = 1, y = 2;
        service.Swap(ref x, ref y);

        Assert.Equal(2, x);
        Assert.Equal(1, y);
    }

    // =========================================================================
    // Mixed ref/out
    // =========================================================================

    [Fact]
    public void MixedRefOut()
    {
        var stub = new Stubs.IMixedRefOutService();

        #region refout-mixed
        stub.Process.Call((string input, out string output, ref int counter) =>
        {
            output = input.ToUpperInvariant();
            counter++;
            return true;
        });
        #endregion

        IMixedRefOutService service = stub;
        int counter = 10;
        var result = service.Process("hello", out string output, ref counter);

        Assert.True(result);
        Assert.Equal("HELLO", output);
        Assert.Equal(11, counter);
    }

    // =========================================================================
    // Sequences with ref/out
    // =========================================================================

    [Fact]
    public void OutParameter_Sequences()
    {
        var stub = new Stubs.IOutParameterService();

        #region refout-sequences
        stub.TryGetValue
            .Call((string key, out string value) => { value = "first"; return true; })
            .ThenReturn((string key, out string value) => { value = "second"; return true; });
        #endregion

        IOutParameterService service = stub;
        service.TryGetValue("a", out var v1);
        service.TryGetValue("b", out var v2);
        service.TryGetValue("c", out var v3);

        Assert.Equal("first", v1);
        Assert.Equal("second", v2);
        Assert.Equal("second", v3);
    }

    // =========================================================================
    // Verification with ref/out
    // =========================================================================

    [Fact]
    public void RefOut_Verification()
    {
        var stub = new Stubs.IOutParameterService();

        stub.TryGetValue.Call((string key, out string value) =>
        {
            value = default!;
            return false;
        });

        IOutParameterService service = stub;
        service.TryGetValue("key1", out _);
        service.TryGetValue("key2", out _);

        #region refout-verification
        stub.TryGetValue.Verify(Called.Exactly(2));
        var lastKey = stub.TryGetValue.LastArg; // captures non-out params only
        #endregion

        Assert.Equal("key2", lastKey);
    }

    // =========================================================================
    // Configuring ref/out callbacks (concise combined example)
    // =========================================================================

    [Fact]
    public void RefOut_ConfiguringCallbacks()
    {
        var stub = new Stubs.IRefOutService();

        #region refout-configuring-callbacks
        // out parameter -- callback must use 'out'
        stub.OutArgument.Call((out int a) => { a = 42; });

        // ref parameter -- callback must use 'ref'
        stub.RefArgument.Call((ref int a) => { a = a + 1; });
        #endregion

        IRefOutService service = stub;

        service.OutArgument(out int outResult);
        Assert.Equal(42, outResult);

        int refValue = 10;
        service.RefArgument(ref refValue);
        Assert.Equal(11, refValue);
    }

    // =========================================================================
    // Generic methods with ref/out parameters
    // =========================================================================

    [Fact]
    public void GenericMethod_WithOutParameter()
    {
        var stub = new Stubs.IGenericRefOutService();

        #region refout-generic-methods
        // Generic methods with ref/out use custom delegates via Of<T>()
        stub.OutArgumentsWithGenerics.Of<string, int>().Call((string a, out int b) => { b = 99; });
        #endregion

        IGenericRefOutService service = stub;
        service.OutArgumentsWithGenerics("hello", out int result);

        Assert.Equal(99, result);
    }
}
