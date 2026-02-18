// -----------------------------------------------------------------------------
// Design.Tests - Protected Method Behavior Tests
// -----------------------------------------------------------------------------
// Tests verifying protected virtual/abstract method behavior across class stub
// patterns. Protected members apply ONLY to class stubs (Patterns 3 & 6) —
// interface stubs have no access modifiers.
//
// KEY INSIGHT: Protected methods can't be called directly from tests. Instead,
// we call public methods that internally call the protected methods, verifying
// that the interceptor configuration flows through correctly.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Stubs.ProtectedMembers;
using KnockOff;

namespace Design.Tests.ProtectedMemberTests;

public class ProtectedMethodBehaviorTests
{
    // =========================================================================
    // PROTECTED ABSTRACT METHOD
    // =========================================================================

    [Fact]
    public void ProtectedAbstractMethod_ConfiguredReturn_FlowsThroughPublicMethod()
    {
        var stub = new ProtectedMemberServiceStub();

        stub.GetInternalId.Return("ID-42");
        stub.FormatTag.Return("tag");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("ID-42: tag", desc);
    }

    [Fact]
    public void ProtectedAbstractMethod_ConfiguredCallback_FlowsThroughPublicMethod()
    {
        var stub = new ProtectedMemberServiceStub();

        var callCount = 0;
        stub.GetInternalId.Call(() =>
        {
            callCount++;
            return $"ID-{callCount}";
        });
        stub.FormatTag.Return("tag");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc1 = service.GetDescription();
        var desc2 = service.GetDescription();

        Assert.Equal("ID-1: tag", desc1);
        Assert.Equal("ID-2: tag", desc2);
    }

    [Fact]
    public void ProtectedAbstractMethod_Unconfigured_ReturnsDefault()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        // GetInternalId returns null (default string), FormatTag returns base "[]"
        // Result: ": []" (null coerced to empty in interpolation)
        Assert.Equal(": []", desc);
    }

    // =========================================================================
    // PROTECTED VIRTUAL METHOD: Base Fallback
    // =========================================================================

    [Fact]
    public void ProtectedVirtualMethod_Configured_OverridesBase()
    {
        var stub = new ProtectedMemberServiceStub();

        stub.FormatTag.Return("custom-format");
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("id: custom-format", desc);
    }

    [Fact]
    public void ProtectedVirtualMethod_Unconfigured_FallsBackToBase()
    {
        var stub = new ProtectedMemberServiceStub();

        // Configure Tag (protected property) so base FormatTag returns "[myTag]"
        stub.Tag.Get("myTag");
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        // Do NOT configure FormatTag — it should call base: $"[{Tag}]"
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("id: [myTag]", desc);
    }

    [Fact]
    public void ProtectedVirtualMethod_CallbackOverride_ReplacesBase()
    {
        var stub = new ProtectedMemberServiceStub();

        stub.FormatTag.Call(() => "<<override>>");
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("id: <<override>>", desc);
    }

    // =========================================================================
    // PROTECTED PROPERTY
    // =========================================================================

    [Fact]
    public void ProtectedProperty_ConfiguredGet_AffectsBaseFallback()
    {
        var stub = new ProtectedMemberServiceStub();

        // Tag is a protected property. Configure its getter.
        stub.Tag.Get("configured-tag");
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        // FormatTag (unconfigured) calls base which reads Tag
        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        // Base FormatTag returns $"[{Tag}]" => "[configured-tag]"
        Assert.Equal("id: [configured-tag]", desc);
    }

    // =========================================================================
    // PROTECTED INDEXER
    // =========================================================================

    [Fact]
    public void ProtectedIndexer_ConfiguredCallback_FlowsThroughPublicMethod()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        stub.Indexer.Get((index) => $"item-{index}");

        ServiceBase service = stub.Object;
        var item = service.GetItemAt(3);

        Assert.Equal("item-3", item);
    }

    [Fact]
    public void ProtectedIndexer_PerKeyReturns_WorksViaPublicMethod()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        stub.Indexer[0].Returns("first");
        stub.Indexer[1].Returns("second");

        ServiceBase service = stub.Object;

        Assert.Equal("first", service.GetItemAt(0));
        Assert.Equal("second", service.GetItemAt(1));
    }

    [Fact]
    public void ProtectedIndexer_SetTracking_FlowsThroughPublicMethod()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        ServiceBase service = stub.Object;
        service.SetItemAt(5, "five");

        Assert.Equal((5, "five"), stub.Indexer.LastSetEntry);
    }

    // =========================================================================
    // PROTECTED EVENT
    // =========================================================================
    // Note: Protected events cannot be subscribed from outside the class
    // hierarchy. We can still verify the interceptor is accessible and
    // that Raise doesn't throw.

    [Fact]
    public void ProtectedEvent_HasSubscribers_FalseByDefault()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        // Protected event: no external subscribers possible
        Assert.False(stub.InternalStateChanged.HasSubscribers);
    }

    [Fact]
    public void ProtectedEvent_Raise_DoesNotThrowWithNoSubscribers()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        // Raise on a protected event with no subscribers should not throw
        stub.InternalStateChanged.Raise(stub.Object, EventArgs.Empty);
    }

    // =========================================================================
    // VERIFICATION: Protected method call tracking
    // =========================================================================

    [Fact]
    public void ProtectedAbstractMethod_Verify_TracksCallCount()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        service.GetDescription(); // calls GetInternalId once
        service.GetDescription(); // calls GetInternalId again

        stub.GetInternalId.Verify(Called.Exactly(2));
    }

    [Fact]
    public void ProtectedVirtualMethod_Verify_TracksEvenWhenUnconfigured()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        service.GetDescription(); // calls FormatTag (unconfigured, falls to base)

        // FormatTag was still called (and tracked) even though it fell back to base
        stub.FormatTag.Verify(Called.Once);
    }

    [Fact]
    public void ProtectedMethod_Verify_ThrowsWhenNotSatisfied()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        service.GetDescription(); // 1 call

        Assert.Throws<VerificationException>(() =>
            stub.GetInternalId.Verify(Called.Exactly(2)));
    }

    // =========================================================================
    // SEQUENCES: Protected methods support ThenReturn
    // =========================================================================

    [Fact]
    public void ProtectedAbstractMethod_Sequence_ThenReturn()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.Name.Get("test");

        // Note: Use callback form for sequences — Return(value).ThenReturn()
        // has a known bug where the first sequence entry gets a null callback.
        stub.GetInternalId
            .Call(() => "first-id")
            .ThenReturn(() => "second-id");

        // FormatTag unconfigured -> falls back to base -> $"[{Tag}]"
        stub.Tag.Get("tag");

        ServiceBase service = stub.Object;
        var desc1 = service.GetDescription();
        var desc2 = service.GetDescription();
        var desc3 = service.GetDescription(); // repeats last

        Assert.Equal("first-id: [tag]", desc1);
        Assert.Equal("second-id: [tag]", desc2);
        Assert.Equal("second-id: [tag]", desc3);
    }

    [Fact]
    public void ProtectedVirtualMethod_Sequence_ThenReturn()
    {
        var stub = new ProtectedMemberServiceStub();
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        // Note: Use callback form for sequences (see note above)
        stub.FormatTag
            .Call(() => "first-format")
            .ThenReturn(() => "second-format");

        ServiceBase service = stub.Object;
        var desc1 = service.GetDescription();
        var desc2 = service.GetDescription();

        Assert.Equal("id: first-format", desc1);
        Assert.Equal("id: second-format", desc2);
    }

    // =========================================================================
    // INLINE PATTERN: Same behavior as standalone
    // =========================================================================

    [Fact]
    public void InlineClass_ProtectedAbstractMethod_WorksIdentically()
    {
        var stub = new ProtectedMethodBehaviorDemo.Stubs.ServiceBase();

        stub.GetInternalId.Return("inline-id");
        stub.FormatTag.Return("tag");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("inline-id: tag", desc);
    }

    [Fact]
    public void InlineClass_ProtectedVirtualMethod_UnconfiguredFallsToBase()
    {
        var stub = new ProtectedMethodBehaviorDemo.Stubs.ServiceBase();

        stub.Tag.Get("inlineTag");
        stub.GetInternalId.Return("id");
        stub.Name.Get("test");

        ServiceBase service = stub.Object;
        var desc = service.GetDescription();

        Assert.Equal("id: [inlineTag]", desc);
    }

    [Fact]
    public void InlineClass_ProtectedIndexer_WorksViaPublicMethod()
    {
        var stub = new ProtectedMethodBehaviorDemo.Stubs.ServiceBase();
        stub.Name.Get("test");
        stub.GetInternalId.Return("id");

        stub.Indexer.Get((index) => $"inline-item-{index}");

        ServiceBase service = stub.Object;
        var item = service.GetItemAt(7);

        Assert.Equal("inline-item-7", item);
    }
}
