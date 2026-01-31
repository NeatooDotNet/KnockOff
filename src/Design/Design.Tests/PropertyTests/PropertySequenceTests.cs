// -----------------------------------------------------------------------------
// Design.Tests - Property Sequence Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Properties;

namespace Design.Tests.PropertyTests;

/// <summary>
/// Tests for property sequences: OnGet().ThenGet() and OnSet().ThenSet() chains.
/// </summary>
public class PropertySequenceTests
{
    [Fact]
    public void ThenGet_CreatesGetterSequence()
    {
        var stub = new PropertySequencesDemo.Stubs.IEntity();
        stub.Name.OnGet("First")
            .ThenGet("Second")
            .ThenGet("Third");

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal("First", entity.Name);
        Assert.Equal("Second", entity.Name);
        Assert.Equal("Third", entity.Name);
    }

    [Fact]
    public void ThenGet_ExhaustsAfterAllCallbacks()
    {
        // ACTUAL BEHAVIOR: Sequences do NOT repeat the last value.
        // After all values are consumed, unconfigured gets return default.
        var stub = new PropertySequencesDemo.Stubs.IEntity();
        stub.Name.OnGet("First")
            .ThenGet("Final");

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal("First", entity.Name);
        Assert.Equal("Final", entity.Name);
        Assert.Null(entity.Name); // Exhausted - returns default(string)
    }

    [Fact]
    public void ThenGet_WithCallbacks()
    {
        var stub = new PropertySequencesDemo.Stubs.IEntity();
        var counter = 0;

        stub.Name.OnGet("Static")
            .ThenGet(() => $"Dynamic {++counter}")
            .ThenGet("Final");

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal("Static", entity.Name);
        Assert.Equal("Dynamic 1", entity.Name);
        Assert.Equal("Final", entity.Name);
    }

    [Fact]
    public void ThenSet_CreatesSetterSequence()
    {
        var stub = new PropertySequencesDemo.Stubs.IEntity();
        var log = new List<string>();

        stub.Description.OnSet(v => log.Add($"First: {v}"))
            .ThenSet(v => log.Add($"Second: {v}"))
            .ThenSet(v => log.Add($"Third: {v}"));

        Design.Domain.Entities.IEntity entity = stub;

        entity.Description = "A";
        entity.Description = "B";
        entity.Description = "C";

        Assert.Equal(["First: A", "Second: B", "Third: C"], log);
    }

    [Fact]
    public void ThenSet_ExhaustsAfterAllCallbacks()
    {
        // ACTUAL BEHAVIOR: Sequences do NOT repeat the last callback.
        // After all callbacks are consumed, sets are not tracked.
        var stub = new PropertySequencesDemo.Stubs.IEntity();
        var log = new List<string>();

        stub.Description.OnSet(v => log.Add($"First: {v}"))
            .ThenSet(v => log.Add($"Final: {v}"));

        Design.Domain.Entities.IEntity entity = stub;

        entity.Description = "A";
        entity.Description = "B";
        entity.Description = "C"; // Exhausted - no callback
        entity.Description = "D"; // Exhausted - no callback

        Assert.Equal(["First: A", "Final: B"], log);
    }
}
