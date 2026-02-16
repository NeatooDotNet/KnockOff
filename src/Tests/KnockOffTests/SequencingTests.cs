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
        var tracking = stub.Add.Return((a, b) => a + b);

        ISequenceTestService svc = stub;
        Assert.Equal(3, svc.Add(1, 2));
        Assert.Equal(7, svc.Add(3, 4));
        Assert.Equal(11, svc.Add(5, 6));

        tracking.Verify(Called.Exactly(3));
    }

    [Fact]
    public void OnCall_ThenCall_AdvancesAfterEachCall()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .Return((a, b) => 100)
            .ThenReturn((a, b) => 200)
            .ThenReturn((a, b) => 300);

        ISequenceTestService svc = stub;
        Assert.Equal(100, svc.Add(1, 2));  // First call - uses first callback
        Assert.Equal(200, svc.Add(1, 2));  // Second call - advances to second
        Assert.Equal(300, svc.Add(1, 2));  // Third call - advances to third
    }

    [Fact]
    public void OnCall_ThenCall_TwoCallbacks_BothExecute()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .Return((a, b) => 100)
            .ThenReturn((a, b) => 200);

        ISequenceTestService svc = stub;
        Assert.Equal(100, svc.Add(0, 0));  // First
        Assert.Equal(200, svc.Add(0, 0));  // Second
    }

    [Fact]
    public void ExhaustedSequence_InStrictMode_Throws()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = true;
        stub.Add
            .Return((a, b) => 100)
            .ThenReturn((a, b) => 200);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Throws<StubException>(() => svc.Add(1, 2));  // Third - exhausted in strict mode
    }

    [Fact]
    public void ExhaustedSequence_InNonStrictMode_RepeatsLastValue()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = false;
        stub.Add
            .Return((a, b) => 100)
            .ThenReturn((a, b) => 200);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Equal(200, svc.Add(1, 2));  // Third - exhausted, repeats last value in non-strict
        Assert.Equal(200, svc.Add(1, 2));  // Fourth - still repeats last value
    }

    [Fact]
    public void ExhaustedSequence_WithThenDefault_ReturnsDefault()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = false;
        stub.Add
            .Return((a, b) => 100)
            .ThenReturn((a, b) => 200)
            .ThenDefault();  // Explicitly request default after exhaustion

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Equal(0, svc.Add(1, 2));  // Third - exhausted, returns default due to ThenDefault()
    }

    [Fact]
    public void OnCall_VoidMethod_Works()
    {
        var stub = new SequenceTestKnockOff();
        var callCount = 0;
        var tracking = stub.DoWork.Call(() => callCount++);

        ISequenceTestService svc = stub;
        svc.DoWork();
        svc.DoWork();

        Assert.Equal(2, callCount);
        tracking.Verify(Called.Exactly(2));
    }

    [Fact]
    public void OnCall_TrackingReturnsCorrectLastArgs()
    {
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.Return((a, b) => a + b);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        Assert.Equal((1, 2), tracking.LastArgs);

        svc.Add(10, 20);
        Assert.Equal((10, 20), tracking.LastArgs);
    }

    [Fact]
    public void Sequence_Verify_SucceedsWhenComplete()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .Return((a, b) => 1)
            .ThenReturn((a, b) => 2);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);
        svc.Add(0, 0);

        // Should not throw - sequence completed
        sequence.Verify();
    }

    [Fact]
    public void Sequence_Verify_ThrowsWhenIncomplete()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .Return((a, b) => 1)
            .ThenReturn((a, b) => 2);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);  // Only called once, but two callbacks in sequence

        Assert.Throws<VerificationException>(() => sequence.Verify());
    }

    [Fact]
    public void Sequence_AllCallbacks_ExecuteInOrder()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .Return((a, b) => 1)
            .ThenReturn((a, b) => 2)
            .ThenReturn((a, b) => 3);

        ISequenceTestService svc = stub;
        Assert.Equal(1, svc.Add(0, 0)); // First callback
        Assert.Equal(2, svc.Add(0, 0)); // Second callback
        Assert.Equal(3, svc.Add(0, 0)); // Third callback

        // Verify sequence completed (all 3 callbacks were invoked)
        sequence.Verify();
    }

    [Fact]
    public void Sequence_Reset_ClearsTracking()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .Return((a, b) => 1)
            .ThenReturn((a, b) => 2);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);
        svc.Add(0, 0);
        // Verify sequence completed
        sequence.Verify();

        sequence.Reset();
        // After reset, sequence is incomplete again - Verify should throw
        Assert.Throws<VerificationException>(() => sequence.Verify());

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
        var tracking = stub.GetMessage.Return((name) => $"Hello {name}");

        ISequenceTestService svc = stub;
        svc.GetMessage("Alice");
        Assert.Equal("Alice", tracking.LastArgs);

        svc.GetMessage("Bob");
        Assert.Equal("Bob", tracking.LastArgs);
    }
}

/// <summary>
/// Tests for the Indexer pattern with multiple key types.
/// Multi-indexers use C# indexer overloads on the interceptor and type-suffixed tracking properties.
/// </summary>
public class MultiIndexerTests
{
    [Fact]
    public void Indexer_String_AccessesStringIndexer()
    {
        var stub = new IndexerTestKnockOff();

        // Configure via per-key Returns
        stub.Indexer["key1"].Returns("value1");

        IIndexerTestService svc = stub;
        var result = svc["key1"];

        Assert.Equal("value1", result);
        stub.Indexer.VerifyGet(Called.Once);
        Assert.Equal("key1", stub.Indexer.LastStringGetKey);
    }

    [Fact]
    public void Indexer_Int32_AccessesIntIndexer()
    {
        var stub = new IndexerTestKnockOff();

        // Configure via per-key Returns
        stub.Indexer[0].Returns(100);
        stub.Indexer[1].Returns(200);

        IIndexerTestService svc = stub;
        Assert.Equal(100, svc[0]);
        Assert.Equal(200, svc[1]);

        stub.Indexer.VerifyGet(Called.Exactly(2));
        Assert.Equal(1, stub.Indexer.LastInt32GetKey);
    }

    [Fact]
    public void Indexer_String_OnGet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();

        stub.Indexer.Get((string key) => $"Value for {key}");

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

        stub.Indexer.VerifySet(Called.Exactly(2));
        Assert.Equal(("key2", "value2"), stub.Indexer.LastStringSetEntry);
    }

    [Fact]
    public void Indexer_String_OnSet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();
        var callbackCalls = new System.Collections.Generic.List<(string key, string value)>();

        stub.Indexer.Set((string key, string value) =>
        {
            callbackCalls.Add((key, value));
        });

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
        stub.Indexer["test"].Returns("string value");
        stub.Indexer[42].Returns(42);

        IIndexerTestService svc = stub;

        // Access both
        Assert.Equal("string value", svc["test"]);
        Assert.Equal(42, svc[42]);

        // Verify total tracking
        stub.Indexer.VerifyGet(Called.Exactly(2));
        Assert.Equal("test", stub.Indexer.LastStringGetKey);
        Assert.Equal(42, stub.Indexer.LastInt32GetKey);
    }
}

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
        var tracking1 = stub.Format.Return((input) => input.ToUpper());
        // Two-param overloads need explicit delegate types because (input, x) is ambiguous
        var tracking2 = stub.Format.Return((OverloadTestKnockOff.FormatDelegate_String_Boolean_String)((input, uppercase) => uppercase ? input.ToUpper() : input));
        var tracking3 = stub.Format.Return((OverloadTestKnockOff.FormatDelegate_String_Int32_String)((input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength))));

        IOverloadTestService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        tracking1.Verify(Called.Once);
        tracking2.Verify(Called.Once);
        tracking3.Verify(Called.Once);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new OverloadTestKnockOff();

        var tracking1 = stub.Format.Return((input) => "1");
        var tracking2 = stub.Format.Return((OverloadTestKnockOff.FormatDelegate_String_Boolean_String)((input, uppercase) => "2"));

        IOverloadTestService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        tracking1.Verify(Called.Exactly(2));
        Assert.Equal("b", tracking1.LastArgs);

        tracking2.Verify(Called.Once);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
