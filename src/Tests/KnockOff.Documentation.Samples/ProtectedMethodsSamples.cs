using KnockOff;

namespace KnockOff.Documentation.Samples.ProtectedMethods;

// =============================================================================
// Base class for protected methods samples
// =============================================================================

#region protected-base-class
public abstract class ServiceBase
{
    protected virtual string Tag { get; set; } = "";
    protected abstract string GetInternalId();
    protected virtual string FormatTag() => $"[{Tag}]";

    public string GetDescription() => $"{GetInternalId()}: {FormatTag()}";
}
#endregion

// =============================================================================
// Inline class stub for ServiceBase
// =============================================================================

[KnockOff<ServiceBase>]
public partial class ProtectedMethodsTests
{
    [Fact]
    public void ProtectedAbstract_ConfigureAndVerify()
    {
        #region protected-configure-verify
        var stub = new Stubs.ServiceBase();

        // Configure protected abstract method via public interceptor
        stub.GetInternalId.Return("ID-42");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc == "ID-42: []"

        // Verify it was called
        stub.GetInternalId.Verify(Called.Once);
        #endregion

        Assert.Equal("ID-42: []", desc);
    }

    [Fact]
    public void ProtectedVirtual_FallbackToBase()
    {
        #region protected-virtual-fallback
        var stub = new Stubs.ServiceBase();
        stub.Tag.Get("myTag");
        stub.GetInternalId.Return("id");

        // FormatTag is NOT configured — falls back to base: $"[{Tag}]"
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();
        // desc == "id: [myTag]"
        #endregion

        Assert.Equal("id: [myTag]", desc);
    }

    [Fact]
    public void ProtectedVirtual_OverrideBase()
    {
        var stub = new Stubs.ServiceBase();
        stub.GetInternalId.Return("id");

        #region protected-override-base
        stub.FormatTag.Return("custom-format");
        #endregion

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("id: custom-format", desc);
    }

    [Fact]
    public void ProtectedMethod_Sequences()
    {
        var stub = new Stubs.ServiceBase();
        stub.Tag.Get("tag");

        #region protected-sequences
        stub.GetInternalId
            .Call(() => "first-id")
            .ThenReturn(() => "second-id");
        #endregion

        ServiceBase service = stub.Object;
        var desc1 = service.GetDescription();
        var desc2 = service.GetDescription();
        var desc3 = service.GetDescription();

        Assert.Equal("first-id: [tag]", desc1);
        Assert.Equal("second-id: [tag]", desc2);
        Assert.Equal("second-id: [tag]", desc3);
    }
}
