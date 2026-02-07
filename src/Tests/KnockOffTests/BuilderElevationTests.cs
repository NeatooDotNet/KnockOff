using KnockOff;
using Xunit;

namespace KnockOff.Tests;

#region Test Interfaces and Stubs

public interface IBuilderElevationService
{
    int Calculate(int x);
    void Process(string value);
    string Name { get; set; }
}

[KnockOff]
public partial class BuilderElevationStub : IBuilderElevationService
{
}

public interface IBuilderElevationIndexer
{
    string this[string key] { get; set; }
}

[KnockOff]
public partial class BuilderElevationIndexerStub : IBuilderElevationIndexer
{
}

#endregion

/// <summary>
/// Tests for the builder pattern that enables lazy elevation from repeating callbacks to sequences.
/// These tests verify that OnCall().ThenReturns() works correctly for sequence elevation.
/// </summary>
public class BuilderElevationTests
{
    #region Method Tests

    [Fact]
    public void OnCall_WithoutThenCall_RepeatsIndefinitely()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Calculate.Returns(x => x * 2);

        IBuilderElevationService svc = stub;

        // Act & Assert - should repeat the same callback
        Assert.Equal(2, svc.Calculate(1));
        Assert.Equal(4, svc.Calculate(2));
        Assert.Equal(6, svc.Calculate(3));
        Assert.Equal(8, svc.Calculate(4));
        Assert.Equal(10, svc.Calculate(5));
    }

    [Fact]
    public void OnCall_ThenCall_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Calculate
            .Returns(x => 100)
            .ThenReturns(x => 200)
            .ThenReturns(x => 300);

        IBuilderElevationService svc = stub;

        // Act & Assert - should execute in sequence order
        Assert.Equal(100, svc.Calculate(0));
        Assert.Equal(200, svc.Calculate(0));
        Assert.Equal(300, svc.Calculate(0));
    }

    [Fact]
    public void OnCall_ThenCall_PreservesTrackingInstance()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        var builder = stub.Calculate.Returns(x => x * 10);

        IBuilderElevationService svc = stub;

        // First call - builder tracks it (in repeating mode)
        Assert.Equal(50, svc.Calculate(5));
        Assert.Equal(5, builder.LastArg);

        // Elevate to sequence - this converts to sequence mode with fresh index
        // Sequence becomes: [x*10 (original), x*100 (new)]
        var sequence = builder.ThenReturns(x => x * 100);

        // After elevation, sequence starts at index 0, so next call uses first callback (x*10)
        Assert.Equal(70, svc.Calculate(7));  // Uses sequence[0] = x*10

        // Third call uses second callback (x*100)
        Assert.Equal(900, svc.Calculate(9));  // Uses sequence[1] = x*100

        // The original builder should still have tracked (builder is reused as sequence[0]'s tracking)
        Assert.Equal(7, builder.LastArg);  // Last call that used the first callback
    }

    [Fact]
    public void OnCall_WithoutThenCall_TrackingWorks()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        var tracking = stub.Calculate.Returns(x => x);

        IBuilderElevationService svc = stub;

        // Act
        svc.Calculate(42);
        svc.Calculate(99);

        // Assert - tracking should work
        Assert.Equal(99, tracking.LastArg);
        tracking.Verify(Times.Exactly(2));
    }

    [Fact]
    public void OnCall_MultipleThenCall_CreatesLongerSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Calculate
            .Returns(x => 1)
            .ThenReturns(x => 2)
            .ThenReturns(x => 3)
            .ThenReturns(x => 4)
            .ThenReturns(x => 5);

        IBuilderElevationService svc = stub;

        // Act & Assert - all 5 callbacks should execute in order
        Assert.Equal(1, svc.Calculate(0));
        Assert.Equal(2, svc.Calculate(0));
        Assert.Equal(3, svc.Calculate(0));
        Assert.Equal(4, svc.Calculate(0));
        Assert.Equal(5, svc.Calculate(0));
    }

    [Fact]
    public void OnCall_ThenCall_ExhaustedInStrictMode_Throws()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Strict = true;
        stub.Calculate
            .Returns(x => 100)
            .ThenReturns(x => 200);

        IBuilderElevationService svc = stub;

        // Act - exhaust the sequence
        svc.Calculate(0);
        svc.Calculate(0);

        // Assert - third call should throw
        var ex = Assert.Throws<StubException>(() => svc.Calculate(0));
        Assert.Contains("exhausted", ex.Message.ToLower());
    }

    [Fact]
    public void OnCall_ThenCall_ExhaustedInNonStrictMode_RepeatsLastValue()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Strict = false;
        stub.Calculate
            .Returns(x => 100)
            .ThenReturns(x => 200);

        IBuilderElevationService svc = stub;

        // Act - exhaust the sequence
        svc.Calculate(0);
        svc.Calculate(0);
        var result = svc.Calculate(0);  // Beyond sequence

        // Assert - should repeat last value (NSubstitute behavior)
        Assert.Equal(200, result);
    }

    [Fact]
    public void OnCall_ThenCall_WithThenDefault_ReturnsDefault()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Strict = false;
        stub.Calculate
            .Returns(x => 100)
            .ThenReturns(x => 200)
            .ThenDefault();  // Explicitly request default after exhaustion

        IBuilderElevationService svc = stub;

        // Act - exhaust the sequence
        svc.Calculate(0);
        svc.Calculate(0);
        var result = svc.Calculate(0);  // Beyond sequence

        // Assert - should return default (0 for int) due to ThenDefault()
        Assert.Equal(0, result);
    }

    [Fact]
    public void OnCall_AfterSequence_ClearsSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();

        // First, configure a sequence
        stub.Calculate
            .Returns(x => 100)
            .ThenReturns(x => 200);

        IBuilderElevationService svc = stub;
        Assert.Equal(100, svc.Calculate(0));
        Assert.Equal(200, svc.Calculate(0));

        // Now reconfigure with OnCall (should clear the sequence)
        stub.Calculate.Returns(x => 999);

        // Act & Assert - should now be repeating mode again
        Assert.Equal(999, svc.Calculate(0));
        Assert.Equal(999, svc.Calculate(0));
        Assert.Equal(999, svc.Calculate(0));
    }

    [Fact]
    public void OnCall_Verifiable_ThenCall_Works()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Calculate
            .Returns(x => 1).Verifiable()
            .ThenReturns(x => 2);

        IBuilderElevationService svc = stub;

        // Act
        svc.Calculate(0);
        svc.Calculate(0);

        // Assert - Stub.Verify() should not throw (verifiable was satisfied)
        stub.Verify();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void OnGet_ThenGet_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Name
            .Get(() => "first")
            .ThenGet(() => "second")
            .ThenGet(() => "third");

        IBuilderElevationService svc = stub;

        // Act & Assert
        Assert.Equal("first", svc.Name);
        Assert.Equal("second", svc.Name);
        Assert.Equal("third", svc.Name);
    }

    [Fact]
    public void OnGet_ThenGetValue_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Name
            .Get("alpha")
            .ThenGet("beta")
            .ThenGet("gamma");

        IBuilderElevationService svc = stub;

        // Act & Assert
        Assert.Equal("alpha", svc.Name);
        Assert.Equal("beta", svc.Name);
        Assert.Equal("gamma", svc.Name);
    }

    [Fact]
    public void OnSet_ThenSet_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        var captured = new System.Collections.Generic.List<string>();

        stub.Name
            .Set(v => captured.Add($"first:{v}"))
            .ThenSet(v => captured.Add($"second:{v}"));

        IBuilderElevationService svc = stub;

        // Act
        svc.Name = "A";
        svc.Name = "B";

        // Assert
        Assert.Equal(new[] { "first:A", "second:B" }, captured);
    }

    [Fact]
    public void OnGet_WithoutThenGet_Repeats()
    {
        // Arrange
        var stub = new BuilderElevationStub();
        stub.Name.Get(() => "constant");

        IBuilderElevationService svc = stub;

        // Act & Assert - should return same value each time
        Assert.Equal("constant", svc.Name);
        Assert.Equal("constant", svc.Name);
        Assert.Equal("constant", svc.Name);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void IndexerOnGet_ThenGet_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationIndexerStub();
        stub.Indexer
            .Get(k => $"first-{k}")
            .ThenGet(k => $"second-{k}")
            .ThenGet(k => $"third-{k}");

        IBuilderElevationIndexer svc = stub;

        // Act & Assert
        Assert.Equal("first-key", svc["key"]);
        Assert.Equal("second-key", svc["key"]);
        Assert.Equal("third-key", svc["key"]);
    }

    [Fact]
    public void IndexerOnSet_ThenSet_CreatesSequence()
    {
        // Arrange
        var stub = new BuilderElevationIndexerStub();
        var captured = new System.Collections.Generic.List<string>();

        stub.Indexer
            .Set((k, v) => captured.Add($"first:{k}={v}"))
            .ThenSet((k, v) => captured.Add($"second:{k}={v}"));

        IBuilderElevationIndexer svc = stub;

        // Act
        svc["a"] = "1";
        svc["b"] = "2";

        // Assert
        Assert.Equal(new[] { "first:a=1", "second:b=2" }, captured);
    }

    [Fact]
    public void IndexerOnGet_WithoutThenGet_Repeats()
    {
        // Arrange
        var stub = new BuilderElevationIndexerStub();
        stub.Indexer.Get(k => k.ToUpper());

        IBuilderElevationIndexer svc = stub;

        // Act & Assert - should apply callback every time
        Assert.Equal("ABC", svc["abc"]);
        Assert.Equal("XYZ", svc["xyz"]);
        Assert.Equal("TEST", svc["test"]);
    }

    #endregion
}
