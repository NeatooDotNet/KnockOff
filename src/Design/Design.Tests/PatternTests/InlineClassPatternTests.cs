// -----------------------------------------------------------------------------
// Design.Tests - Inline Class Pattern Tests
// -----------------------------------------------------------------------------

using Design.Stubs.StubPatterns;

namespace Design.Tests.PatternTests;

/// <summary>
/// Tests for the inline class pattern: [KnockOff{T}] where T is a class
/// with virtual/abstract members.
/// </summary>
public class InlineClassPatternTests
{
    [Fact]
    public void InlineClass_GeneratesNestedStubsClass()
    {
        var stub = new InlineClassExample.Stubs.ServiceBase();

        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineClass_StubImplementsIKnockOffStub()
    {
        var stub = new InlineClassExample.Stubs.ServiceBase();

        Assert.IsAssignableFrom<KnockOff.IKnockOffStub>(stub);
    }

    [Fact]
    public void InlineClass_VirtualProperty_CanBeConfigured()
    {
        var stub = new InlineClassExample.Stubs.ServiceBase();
        stub.Name.OnGet("TestService");

        var name = stub.Object.Name;

        Assert.Equal("TestService", name);
    }

    [Fact]
    public void InlineClass_VirtualMethod_CanBeConfigured()
    {
        var stub = new InlineClassExample.Stubs.ServiceBase();
        var initializeCalled = false;
        stub.Initialize.OnCall(() => initializeCalled = true);

        stub.Object.Initialize();

        Assert.True(initializeCalled);
    }

    [Fact]
    public void InlineClass_UnconfiguredVirtualMember_ReturnsDefault()
    {
        var stub = new InlineClassExample.Stubs.ServiceBase();

        var name = stub.Object.Name;

        Assert.Null(name); // default(string) = null
    }
}
