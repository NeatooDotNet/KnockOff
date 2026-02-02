// -----------------------------------------------------------------------------
// Design.Tests - Method Overload Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for method overload stubbing: OnCall disambiguation, When targeting,
/// per-overload tracking, and sequences.
/// </summary>
public class MethodOverloadTests
{
    [Fact]
    public void OnCall_DifferentParamCounts_ResolvesAutomatically()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        stub.Format.OnCall((input) => "one-param");
        stub.Format.OnCall((input, options) => "two-params");
        stub.Format.OnCall((input, options, maxLength) => "three-params");

        IFormatter formatter = stub;

        Assert.Equal("one-param", formatter.Format("a"));
        Assert.Equal("two-params", formatter.Format("b", new FormatOptions()));
        Assert.Equal("three-params", formatter.Format("c", new FormatOptions(), 10));
    }

    [Fact]
    public void OnCall_CanReturnConstantValue()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        // All overloads return same constant
        stub.Format.OnCall((input) => "constant");
        stub.Format.OnCall((input, options) => "constant");

        IFormatter formatter = stub;

        Assert.Equal("constant", formatter.Format("a"));
        Assert.Equal("constant", formatter.Format("b", new FormatOptions()));
    }

    [Fact]
    public void When_ParameterSignature_ResolvesOverload()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        // Defaults
        stub.Format.OnCall((input) => "default-1");
        stub.Format.OnCall((input, options) => "default-2");

        // Specific matches
        stub.Format.When("special").Returns("SPECIAL-1");
        stub.Format.When("special", new FormatOptions(Uppercase: true)).Returns("SPECIAL-2");

        IFormatter formatter = stub;

        Assert.Equal("SPECIAL-1", formatter.Format("special"));
        Assert.Equal("SPECIAL-2", formatter.Format("special", new FormatOptions(Uppercase: true)));
        Assert.Equal("default-1", formatter.Format("other"));
        Assert.Equal("default-2", formatter.Format("other", new FormatOptions()));
    }

    [Fact]
    public void When_Predicate_ResolvesOverload()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        stub.Format.OnCall((input) => "default");
        stub.Format.When((input) => input.StartsWith("X", StringComparison.Ordinal)).Returns("X-PREFIX");

        IFormatter formatter = stub;

        Assert.Equal("X-PREFIX", formatter.Format("X-file"));
        Assert.Equal("default", formatter.Format("other"));
    }

    [Fact]
    public void Tracking_EachOverload_TrackedSeparately()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        var tracking1 = stub.Format.OnCall((input) => input);
        var tracking2 = stub.Format.OnCall((input, options) => input);

        IFormatter formatter = stub;

        formatter.Format("a");
        formatter.Format("b");
        formatter.Format("c", new FormatOptions());

        tracking1.Verify(Times.Exactly(2));
        Assert.Equal("b", tracking1.LastArg);

        tracking2.Verify(Times.Once);
        Assert.Equal(("c", new FormatOptions()), tracking2.LastArgs);
    }

    [Fact]
    public void Verify_OnInterceptor_CountsAllOverloads()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        stub.Format.OnCall((input) => input);
        stub.Format.OnCall((input, options) => input);

        IFormatter formatter = stub;

        formatter.Format("a");
        formatter.Format("b", new FormatOptions());
        formatter.Format("c", new FormatOptions());

        stub.Format.Verify(Times.Exactly(3));
    }

    [Fact]
    public void VoidOverloads_ConfiguredSeparately()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();
        var logs = new List<string>();

        stub.Log.OnCall((msg) => logs.Add($"1:{msg}"));
        stub.Log.OnCall((msg, level) => logs.Add($"2:{msg}:{level}"));
        stub.Log.OnCall((msg, level, cat) => logs.Add($"3:{msg}:{level}:{cat}"));

        IFormatter formatter = stub;

        formatter.Log("a");
        formatter.Log("b", 2);
        formatter.Log("c", 3, "SYS");

        Assert.Equal(["1:a", "2:b:2", "3:c:3:SYS"], logs);
    }

    [Fact]
    public void VoidOverloads_TrackedSeparately()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        var tracking1 = stub.Log.OnCall((msg) => { });
        var tracking2 = stub.Log.OnCall((msg, level) => { });

        IFormatter formatter = stub;

        formatter.Log("a");
        formatter.Log("b");
        formatter.Log("c", 1);

        tracking1.Verify(Times.Exactly(2));
        tracking2.Verify(Times.Once);
    }

#pragma warning disable xUnit1051 // Testing CancellationToken overload specifically
    [Fact]
    public async Task AsyncOverloads_WithAndWithoutCancellation()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        var tracking1 = stub.TransformAsync.OnCall((input) => $"[{input}]");
        var tracking2 = stub.TransformAsync.OnCall((input, ct) => $"[{input}:ct]");

        IFormatter formatter = stub;

        var r1 = await formatter.TransformAsync("a");
        var r2 = await formatter.TransformAsync("b", CancellationToken.None);

        Assert.Equal("[a]", r1);
        Assert.Equal("[b:ct]", r2);

        tracking1.Verify(Times.Once);
        tracking2.Verify(Times.Once);
    }
#pragma warning restore xUnit1051

    [Fact]
    public void Sequences_PerOverload()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        stub.Format
            .OnCall((input) => "first")
            .ThenReturns("second");

        stub.Format
            .OnCall((input, options) => "A")
            .ThenReturns("B");

        IFormatter formatter = stub;

        // Single-param sequence
        Assert.Equal("first", formatter.Format("x"));
        Assert.Equal("second", formatter.Format("x"));

        // Two-param sequence (independent)
        Assert.Equal("A", formatter.Format("x", new FormatOptions()));
        Assert.Equal("B", formatter.Format("x", new FormatOptions()));

        // Back to single-param
        Assert.Equal("second", formatter.Format("x")); // repeats last
    }

    [Fact]
    public void Reset_ClearsAllOverloads()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        var tracking1 = stub.Format.OnCall((input) => input);
        var tracking2 = stub.Format.OnCall((input, options) => input);

        IFormatter formatter = stub;

        formatter.Format("a");
        formatter.Format("b", new FormatOptions());

        tracking1.Verify(Times.Once);
        tracking2.Verify(Times.Once);

        stub.Format.Reset();

        tracking1.Verify(Times.Never);
        tracking2.Verify(Times.Never);
    }
}
