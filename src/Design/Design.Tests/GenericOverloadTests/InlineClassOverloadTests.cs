// -----------------------------------------------------------------------------
// Design.Tests - Tests for Inline Class Stubs with Overloaded Methods
// -----------------------------------------------------------------------------
// These tests verify that the overload API works correctly with:
// - Inline class stubs: [KnockOff<ProcessorBase>] (non-generic class)
// - Overloads with same or different return types share single interceptor
// - C# overload resolution distinguishes OnCall delegate types
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using KnockOff;

namespace Design.Tests.GenericOverloadTests;

// =============================================================================
// INLINE CLASS WITH COMPATIBLE OVERLOADS
// =============================================================================

[KnockOff<ProcessorBase>]
public partial class InlineClassOverloadTests
{
    // =========================================================================
    // Process() - Compatible overloads (same return type, different params)
    // =========================================================================

    [Fact]
    public void InlineClass_Process_OverloadsUseSingleInterceptor()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        // All Process overloads share one interceptor with multiple OnCall overloads
        stub.Process.Returns(() => "no-params");
        stub.Process.Returns((string message) => $"message: {message}");
        stub.Process.Returns((string message, int priority) => $"message-priority: {message}, {priority}");

        ProcessorBase processor = stub.Object;

        // Act & Assert
        Assert.Equal("no-params", processor.Process());
        Assert.Equal("message: hello", processor.Process("hello"));
        Assert.Equal("message-priority: urgent, 1", processor.Process("urgent", 1));
    }

    [Fact]
    public void InlineClass_Process_TrackingWorksPerOverload()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        var tracking0 = stub.Process.Returns(() => "0");
        var tracking1 = stub.Process.Returns((string message) => "1");
        var tracking2 = stub.Process.Returns((string message, int priority) => "2");

        ProcessorBase processor = stub.Object;

        // Act
        processor.Process();
        processor.Process();
        processor.Process("a");
        processor.Process("b", 1);
        processor.Process("c", 2);
        processor.Process("d", 3);

        // Assert - per-overload tracking
        tracking0.Verify(Times.Exactly(2));
        tracking1.Verify(Times.Once);
        tracking2.Verify(Times.Exactly(3));
    }

    [Fact]
    public void InlineClass_Process_TotalTrackingAcrossAllOverloads()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        stub.Process.Returns(() => "0");
        stub.Process.Returns((string message) => "1");
        stub.Process.Returns((string message, int priority) => "2");

        ProcessorBase processor = stub.Object;

        // Act
        processor.Process();
        processor.Process("a");
        processor.Process("b", 1);

        // Assert - interceptor-level verify counts all overloads
        stub.Process.Verify(Times.Exactly(3));
    }

    [Fact]
    public void InlineClass_Process_WhenMatchingWorks()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        stub.Process.Returns((string message) => "default");
        stub.Process.When("special").Returns("SPECIAL");

        ProcessorBase processor = stub.Object;

        // Act & Assert
        Assert.Equal("SPECIAL", processor.Process("special"));
        Assert.Equal("default", processor.Process("other"));
    }

    [Fact]
    public void InlineClass_Process_SequenceWorksPerOverload()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        stub.Process
            .Returns(() => "first")
            .ThenReturns("second")
            .ThenReturns("third");

        stub.Process
            .Returns((string message) => "A")
            .ThenReturns("B");

        ProcessorBase processor = stub.Object;

        // Act & Assert - sequences are independent per overload
        Assert.Equal("first", processor.Process());
        Assert.Equal("A", processor.Process("x"));
        Assert.Equal("second", processor.Process());
        Assert.Equal("B", processor.Process("y"));
        Assert.Equal("third", processor.Process());
    }

    // =========================================================================
    // Transform() - Different return types share single interceptor
    // =========================================================================

    [Fact]
    public void InlineClass_Transform_DifferentReturnTypesShareSingleInterceptor()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        // Transform(int) returns string, Transform(string) returns int
        // Different return types with different param types - C# can distinguish the delegates
        // OnCall(Func<int,string>) vs OnCall(Func<string,int>) are distinguishable
        stub.Transform.Returns((int value) => $"int-to-string: {value}");
        stub.Transform.Returns((string text) => text.Length * 10);

        ProcessorBase processor = stub.Object;

        // Act
        string result1 = processor.Transform(42);
        int result2 = processor.Transform("hello");

        // Assert
        Assert.Equal("int-to-string: 42", result1);
        Assert.Equal(50, result2); // "hello".Length * 10
    }

    [Fact]
    public void InlineClass_Transform_OverloadsTrackIndependently()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        var tracking1 = stub.Transform.Returns((int value) => "x");
        var tracking2 = stub.Transform.Returns((string text) => 0);

        ProcessorBase processor = stub.Object;

        // Act
        processor.Transform(1);
        processor.Transform(2);
        processor.Transform(3);
        processor.Transform("a");

        // Assert
        tracking1.Verify(Times.Exactly(3));
        tracking2.Verify(Times.Once);
    }

    // =========================================================================
    // Calculate() - Additional compatible overloads
    // =========================================================================

    [Fact]
    public void InlineClass_Calculate_CompatibleOverloadsWork()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        // Both return int, different param counts - compatible
        stub.Calculate.Returns((int x) => x * 2);
        stub.Calculate.Returns((int x, int y) => x * y);

        ProcessorBase processor = stub.Object;

        // Act & Assert
        Assert.Equal(10, processor.Calculate(5));      // 5 * 2
        Assert.Equal(12, processor.Calculate(3, 4));   // 3 * 4
    }

    // =========================================================================
    // Unconfigured behavior - falls through to base
    // =========================================================================

    [Fact]
    public void InlineClass_UnconfiguredOverload_FallsThroughToBase()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();

        // Only configure one overload
        stub.Process.Returns(() => "configured");
        // Other overloads are NOT configured

        ProcessorBase processor = stub.Object;

        // Act
        var configured = processor.Process();
        var unconfigured = processor.Process("test");  // Falls through to base

        // Assert
        Assert.Equal("configured", configured);
        Assert.Equal("base-message: test", unconfigured);  // Base implementation
    }

    // =========================================================================
    // Strict mode behavior
    // =========================================================================

    [Fact]
    public void InlineClass_StrictMode_UnconfiguredOverloadThrows()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase().Strict();

        // Configure only one overload
        stub.Process.Returns(() => "configured");

        ProcessorBase processor = stub.Object;

        // Act & Assert
        Assert.Equal("configured", processor.Process());  // This works
        Assert.Throws<StubException>(() => processor.Process("test"));  // Unconfigured throws
    }

    // =========================================================================
    // Property stubbing (unrelated to overloads, but verifies class stub works)
    // =========================================================================

    [Fact]
    public void InlineClass_AbstractProperty_CanBeStubbed()
    {
        // Arrange
        var stub = new Stubs.ProcessorBase();
        stub.Name.OnGet("TestProcessor");

        ProcessorBase processor = stub.Object;

        // Act & Assert
        Assert.Equal("TestProcessor", processor.Name);
    }
}

// Note: Tests for inline class with closed generic (e.g., [KnockOff<RepositoryBase<T>>])
// are covered in OpenGenericClassOverloadTests using the open generic pattern with TestEntity.
// The closed generic pattern has the same behavior but requires the type argument to be public,
// which conflicts with the CA1515 analyzer rule. The open generic pattern is more flexible
// and is the recommended approach for generic class stubs.
