using KnockOff;
using Xunit;

namespace KnockOff.Tests;

#region Test Interface and Stub

public interface ISequenceTestService
{
    int Add(int a, int b);
    void DoWork();
    string GetMessage(string name);
}

[KnockOff]
public partial class SequenceTestKnockOff : ISequenceTestService
{
}

public interface IIndexerTestService
{
    string this[string key] { get; set; }
    int this[int index] { get; }
}

[KnockOff]
public partial class IndexerTestKnockOff : IIndexerTestService
{
}

#endregion

#region Method Overload Test Types

public interface IOverloadTestService
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}

[KnockOff]
public partial class OverloadTestKnockOff : IOverloadTestService
{
}

#endregion

public class SequencingTests
{
    [Fact]
    public void OnCall_WithoutTimes_RepeatsForever()
    {
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((ko, a, b) => a + b);

        ISequenceTestService svc = stub;
        Assert.Equal(3, svc.Add(1, 2));
        Assert.Equal(7, svc.Add(3, 4));
        Assert.Equal(11, svc.Add(5, 6));

        Assert.Equal(3, tracking.CallCount);
        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void OnCall_WithTimes_AdvancesToNext()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Forever);

        ISequenceTestService svc = stub;
        Assert.Equal(100, svc.Add(1, 2));  // First call - uses first callback
        Assert.Equal(200, svc.Add(1, 2));  // Second call - advances to second
        Assert.Equal(200, svc.Add(1, 2));  // Third call - stays on Forever
    }

    [Fact]
    public void OnCall_WithTimesTwice_AdvancesAfterTwo()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Twice)
            .ThenCall((ko, a, b) => 200, Times.Forever);

        ISequenceTestService svc = stub;
        Assert.Equal(100, svc.Add(0, 0));  // First
        Assert.Equal(100, svc.Add(0, 0));  // Second (still first callback)
        Assert.Equal(200, svc.Add(0, 0));  // Third (advances)
        Assert.Equal(200, svc.Add(0, 0));  // Fourth (repeats)
    }

    [Fact]
    public void OnCall_WithExactly_AdvancesAfterCount()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 1, Times.Exactly(3))
            .ThenCall((ko, a, b) => 2, Times.Forever);

        ISequenceTestService svc = stub;
        Assert.Equal(1, svc.Add(0, 0));
        Assert.Equal(1, svc.Add(0, 0));
        Assert.Equal(1, svc.Add(0, 0));
        Assert.Equal(2, svc.Add(0, 0));  // Advances after 3
    }

    [Fact]
    public void ExhaustedSequence_Throws()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Once);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Throws<StubException>(() => svc.Add(1, 2));  // Third - exhausted
    }

    [Fact]
    public void OnCall_VoidMethod_Works()
    {
        var stub = new SequenceTestKnockOff();
        var callCount = 0;
        var tracking = stub.DoWork.OnCall(ko => callCount++);

        ISequenceTestService svc = stub;
        svc.DoWork();
        svc.DoWork();

        Assert.Equal(2, callCount);
        Assert.Equal(2, tracking.CallCount);
    }

    [Fact]
    public void OnCall_TrackingReturnsCorrectLastArgs()
    {
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((ko, a, b) => a + b);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        Assert.Equal((1, 2), tracking.LastArgs);

        svc.Add(10, 20);
        Assert.Equal((10, 20), tracking.LastArgs);
    }

    [Fact]
    public void Sequence_Verify_ReturnsTrueWhenSatisfied()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((ko, a, b) => 1, Times.Once)
            .ThenCall((ko, a, b) => 2, Times.Once);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);
        svc.Add(0, 0);

        Assert.True(sequence.Verify());
    }

    [Fact]
    public void Sequence_Verify_ReturnsFalseWhenNotSatisfied()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((ko, a, b) => 1, Times.Twice)
            .ThenCall((ko, a, b) => 2, Times.Once);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);  // Only called once, but Twice was expected

        Assert.False(sequence.Verify());
    }

    [Fact]
    public void Sequence_TotalCallCount_TracksAllCalls()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((ko, a, b) => 1, Times.Twice)
            .ThenCall((ko, a, b) => 2, Times.Forever);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);
        svc.Add(0, 0);
        svc.Add(0, 0);

        Assert.Equal(3, sequence.TotalCallCount);
    }

    [Fact]
    public void Sequence_Reset_ClearsTracking()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((ko, a, b) => 1, Times.Once)
            .ThenCall((ko, a, b) => 2, Times.Forever);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);
        svc.Add(0, 0);
        Assert.Equal(2, sequence.TotalCallCount);

        sequence.Reset();
        Assert.Equal(0, sequence.TotalCallCount);

        // After reset, should start from beginning
        Assert.Equal(1, svc.Add(0, 0));  // First callback again
    }

    [Fact]
    public void NoCallback_NonStrict_ReturnsDefault()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = false;

        ISequenceTestService svc = stub;
        var result = svc.Add(1, 2);

        Assert.Equal(0, result);  // default(int)
    }

    [Fact]
    public void NoCallback_Strict_Throws()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = true;

        ISequenceTestService svc = stub;
        Assert.Throws<StubException>(() => svc.Add(1, 2));
    }

    [Fact]
    public void OnCall_SingleArgMethod_TracksLastArg()
    {
        var stub = new SequenceTestKnockOff();
        var tracking = stub.GetMessage.OnCall((ko, name) => $"Hello {name}");

        ISequenceTestService svc = stub;
        svc.GetMessage("Alice");
        Assert.Equal("Alice", tracking.LastArg);

        svc.GetMessage("Bob");
        Assert.Equal("Bob", tracking.LastArg);
    }
}

/// <summary>
/// Tests for the Indexer pattern with multiple key types.
/// Indexers are accessed via IndexerString and IndexerInt32 properties.
/// </summary>
public class IndexerOfXxxTests
{
    [Fact]
    public void Indexer_String_AccessesStringIndexer()
    {
        var stub = new IndexerTestKnockOff();

        // Configure via IndexerString
        stub.IndexerString.Backing["key1"] = "value1";

        IIndexerTestService svc = stub;
        var result = svc["key1"];

        Assert.Equal("value1", result);
        Assert.Equal(1, stub.IndexerString.GetCount);
        Assert.Equal("key1", stub.IndexerString.LastGetKey);
    }

    [Fact]
    public void Indexer_Int32_AccessesIntIndexer()
    {
        var stub = new IndexerTestKnockOff();

        // Configure via IndexerInt32
        stub.IndexerInt32.Backing[0] = 100;
        stub.IndexerInt32.Backing[1] = 200;

        IIndexerTestService svc = stub;
        Assert.Equal(100, svc[0]);
        Assert.Equal(200, svc[1]);

        Assert.Equal(2, stub.IndexerInt32.GetCount);
        Assert.Equal(1, stub.IndexerInt32.LastGetKey);
    }

    [Fact]
    public void Indexer_String_OnGet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();

        stub.IndexerString.OnGet = (ko, key) => $"Value for {key}";

        IIndexerTestService svc = stub;
        Assert.Equal("Value for foo", svc["foo"]);
        Assert.Equal("Value for bar", svc["bar"]);
    }

    [Fact]
    public void Indexer_String_Setter_TracksAccess()
    {
        var stub = new IndexerTestKnockOff();

        IIndexerTestService svc = stub;
        svc["key1"] = "value1";
        svc["key2"] = "value2";

        Assert.Equal(2, stub.IndexerString.SetCount);
        Assert.Equal(("key2", "value2"), stub.IndexerString.LastSetEntry);
    }

    [Fact]
    public void Indexer_String_OnSet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();
        var callbackCalls = new System.Collections.Generic.List<(string key, string value)>();

        stub.IndexerString.OnSet = (ko, key, value) =>
        {
            callbackCalls.Add((key, value));
        };

        IIndexerTestService svc = stub;
        svc["a"] = "1";
        svc["b"] = "2";

        Assert.Equal(2, callbackCalls.Count);
        Assert.Equal(("a", "1"), callbackCalls[0]);
        Assert.Equal(("b", "2"), callbackCalls[1]);
    }

    [Fact]
    public void Indexer_MultipleKeyTypes_AreIndependent()
    {
        var stub = new IndexerTestKnockOff();

        // Configure both indexers
        stub.IndexerString.Backing["test"] = "string value";
        stub.IndexerInt32.Backing[42] = 42;

        IIndexerTestService svc = stub;

        // Access both
        Assert.Equal("string value", svc["test"]);
        Assert.Equal(42, svc[42]);

        // Verify tracking is independent
        Assert.Equal(1, stub.IndexerString.GetCount);
        Assert.Equal(1, stub.IndexerInt32.GetCount);
    }
}

// Note: Stub-level Verify() and VerifyAll() were removed from the API.
// Verification is done via the sequence.Verify() method returned by OnCall(..., Times).
// See SequencingTests for sequence verification examples.

/// <summary>
/// Tests for method overload support via compiler resolution on OnCall() delegates.
/// For overloads with same number of parameters, explicit delegate types are required.
/// </summary>
public class MethodOverloadTests
{
    [Fact]
    public void OnCall_DifferentOverloads_CompilerResolvesCorrectly()
    {
        var stub = new OverloadTestKnockOff();

        // Single-param overload can be inferred
        var tracking1 = stub.Format.OnCall((ko, input) => input.ToUpper());
        // Two-param overloads need explicit delegate types because (ko, input, x) is ambiguous
        var tracking2 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)((ko, input, uppercase) => uppercase ? input.ToUpper() : input));
        var tracking3 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Int32_String)((ko, input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength))));

        IOverloadTestService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        Assert.Equal(1, tracking1.CallCount);
        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(1, tracking3.CallCount);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new OverloadTestKnockOff();

        var tracking1 = stub.Format.OnCall((ko, input) => "1");
        var tracking2 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)((ko, input, uppercase) => "2"));

        IOverloadTestService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        Assert.Equal(2, tracking1.CallCount);
        Assert.Equal("b", tracking1.LastArg);

        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
