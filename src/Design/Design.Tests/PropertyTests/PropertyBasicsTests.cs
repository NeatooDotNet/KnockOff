// -----------------------------------------------------------------------------
// Design.Tests - Property Basics Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Properties;
using KnockOff;

namespace Design.Tests.PropertyTests;

/// <summary>
/// Tests for basic property stubbing: Get, Set, LastSetValue.
/// </summary>
public class PropertyBasicsTests
{
    [Fact]
    public void Get_SetsConstantGetterValue()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        stub.Id.Get(42);

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal(42, entity.Id);
    }

    [Fact]
    public void Get_WithCallback()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        var counter = 0;
        stub.Id.Get(() => ++counter);

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.Id);
        Assert.Equal(3, entity.Id);
    }

    [Fact]
    public void Set_InterceptsSetter()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        string? captured = null;
        stub.Description.Set(v => captured = v);

        Design.Domain.Entities.IEntity entity = stub;
        entity.Description = "Test value";

        Assert.Equal("Test value", captured);
    }

    [Fact]
    public void LastSetValue_CapturesSetterArgument()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();

        Design.Domain.Entities.IEntity entity = stub;
        entity.Description = "First";
        entity.Description = "Second";

        Assert.Equal("Second", stub.Description.LastSetValue);
    }

    [Fact]
    public void BackingStore_WithClosurePattern()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        string backingStore = "Initial";

        stub.Description.Get(() => backingStore);
        stub.Description.Set(v => backingStore = v);

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Equal("Initial", entity.Description);
        entity.Description = "Changed";
        Assert.Equal("Changed", entity.Description);
    }

    [Fact]
    public void VerifyGet_ChecksGetterAccess()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        stub.Id.Get(42);

        Design.Domain.Entities.IEntity entity = stub;
        _ = entity.Id;
        _ = entity.Id;

        stub.Id.VerifyGet(Called.Exactly(2));
    }

    [Fact]
    public void VerifySet_ChecksSetterAccess()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();

        Design.Domain.Entities.IEntity entity = stub;
        entity.Description = "One";
        entity.Description = "Two";

        stub.Description.VerifySet(Called.Exactly(2));
    }

    [Fact]
    public void UnconfiguredProperty_ReturnsDefault()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();

        Design.Domain.Entities.IEntity entity = stub;

        Assert.Null(entity.Name); // default(string) = null
        Assert.Equal(0, entity.Id); // default(int) = 0
    }

    [Fact]
    public void Reset_ClearsTrackingPreservesConfig()
    {
        var stub = new PropertyBasicsDemo.Stubs.IEntity();
        stub.Description.Get("Test");

        Design.Domain.Entities.IEntity entity = stub;
        _ = entity.Description;
        entity.Description = "Changed";

        stub.Description.Reset();

        // Configuration preserved
        Assert.Equal("Test", entity.Description);

        // Tracking cleared
        Assert.Throws<VerificationException>(() => stub.Description.VerifyGet(Called.Exactly(2)));
    }
}
