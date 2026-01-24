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
        var tracking = stub.Add.OnCall((a, b) => a + b);

        ISequenceTestService svc = stub;
        Assert.Equal(3, svc.Add(1, 2));
        Assert.Equal(7, svc.Add(3, 4));
        Assert.Equal(11, svc.Add(5, 6));

        tracking.Verify(Times.Exactly(3));
    }

    [Fact]
    public void OnCallSequence_AdvancesAfterEachCall()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCallSequence((a, b) => 100)
            .ThenCall((a, b) => 200)
            .ThenCall((a, b) => 300);

        ISequenceTestService svc = stub;
        Assert.Equal(100, svc.Add(1, 2));  // First call - uses first callback
        Assert.Equal(200, svc.Add(1, 2));  // Second call - advances to second
        Assert.Equal(300, svc.Add(1, 2));  // Third call - advances to third
    }

    [Fact]
    public void OnCallSequence_TwoCallbacks_BothExecute()
    {
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCallSequence((a, b) => 100)
            .ThenCall((a, b) => 200);

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
            .OnCallSequence((a, b) => 100)
            .ThenCall((a, b) => 200);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Throws<StubException>(() => svc.Add(1, 2));  // Third - exhausted in strict mode
    }

    [Fact]
    public void ExhaustedSequence_InNonStrictMode_ReturnsDefault()
    {
        var stub = new SequenceTestKnockOff();
        stub.Strict = false;
        stub.Add
            .OnCallSequence((a, b) => 100)
            .ThenCall((a, b) => 200);

        ISequenceTestService svc = stub;
        svc.Add(1, 2);  // First - OK
        svc.Add(1, 2);  // Second - OK

        Assert.Equal(0, svc.Add(1, 2));  // Third - exhausted, returns default in non-strict
    }

    [Fact]
    public void OnCall_VoidMethod_Works()
    {
        var stub = new SequenceTestKnockOff();
        var callCount = 0;
        var tracking = stub.DoWork.OnCall(() => callCount++);

        ISequenceTestService svc = stub;
        svc.DoWork();
        svc.DoWork();

        Assert.Equal(2, callCount);
        tracking.Verify(Times.Exactly(2));
    }

    [Fact]
    public void OnCall_TrackingReturnsCorrectLastArgs()
    {
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

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
            .OnCallSequence((a, b) => 1)
            .ThenCall((a, b) => 2);

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
            .OnCallSequence((a, b) => 1)
            .ThenCall((a, b) => 2);

        ISequenceTestService svc = stub;
        svc.Add(0, 0);  // Only called once, but two callbacks in sequence

        Assert.Throws<VerificationException>(() => sequence.Verify());
    }

    [Fact]
    public void Sequence_AllCallbacks_ExecuteInOrder()
    {
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCallSequence((a, b) => 1)
            .ThenCall((a, b) => 2)
            .ThenCall((a, b) => 3);

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
            .OnCallSequence((a, b) => 1)
            .ThenCall((a, b) => 2);

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
        var tracking = stub.GetMessage.OnCall((name) => $"Hello {name}");

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
        stub.Indexer.OfString.Backing["key1"] = "value1";

        IIndexerTestService svc = stub;
        var result = svc["key1"];

        Assert.Equal("value1", result);
        stub.Indexer.OfString.VerifyGet(Times.Once);
        Assert.Equal("key1", stub.Indexer.OfString.LastGetKey);
    }

    [Fact]
    public void Indexer_Int32_AccessesIntIndexer()
    {
        var stub = new IndexerTestKnockOff();

        // Configure via IndexerInt32
        stub.Indexer.OfInt32.Backing[0] = 100;
        stub.Indexer.OfInt32.Backing[1] = 200;

        IIndexerTestService svc = stub;
        Assert.Equal(100, svc[0]);
        Assert.Equal(200, svc[1]);

        stub.Indexer.OfInt32.VerifyGet(Times.Exactly(2));
        Assert.Equal(1, stub.Indexer.OfInt32.LastGetKey);
    }

    [Fact]
    public void Indexer_String_OnGet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();

        stub.Indexer.OfString.OnGet((key) => $"Value for {key}");

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

        stub.Indexer.OfString.VerifySet(Times.Exactly(2));
        Assert.Equal(("key2", "value2"), stub.Indexer.OfString.LastSetEntry);
    }

    [Fact]
    public void Indexer_String_OnSet_UsesCallback()
    {
        var stub = new IndexerTestKnockOff();
        var callbackCalls = new System.Collections.Generic.List<(string key, string value)>();

        stub.Indexer.OfString.OnSet((key, value) =>
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
        stub.Indexer.OfString.Backing["test"] = "string value";
        stub.Indexer.OfInt32.Backing[42] = 42;

        IIndexerTestService svc = stub;

        // Access both
        Assert.Equal("string value", svc["test"]);
        Assert.Equal(42, svc[42]);

        // Verify tracking is independent
        stub.Indexer.OfString.VerifyGet(Times.Once);
        stub.Indexer.OfInt32.VerifyGet(Times.Once);
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
        var tracking1 = stub.Format.OnCall((input) => input.ToUpper());
        // Two-param overloads need explicit delegate types because (input, x) is ambiguous
        var tracking2 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)((input, uppercase) => uppercase ? input.ToUpper() : input));
        var tracking3 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Int32_String)((input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength))));

        IOverloadTestService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        tracking1.Verify(Times.Once);
        tracking2.Verify(Times.Once);
        tracking3.Verify(Times.Once);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new OverloadTestKnockOff();

        var tracking1 = stub.Format.OnCall((input) => "1");
        var tracking2 = stub.Format.OnCall((OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)((input, uppercase) => "2"));

        IOverloadTestService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        tracking1.Verify(Times.Exactly(2));
        Assert.Equal("b", tracking1.LastArg);

        tracking2.Verify(Times.Once);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
