// -----------------------------------------------------------------------------
// Design.Tests - Routing coverage for shadowed properties (C# `new` modifier).
//
// Each shadowed hierarchy exposes ONE interceptor with the UNION of accessors.
// Explicit interface implementations still use per-face accessor sets; these
// tests assert that reading via the narrow face hits the interceptor's getter
// and writing via the wide face hits the interceptor's setter.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using Design.Stubs.Properties;
using KnockOff;

namespace Design.Tests.PropertyTests;

public class NarrowingPropertyTests
{
    [Fact]
    public void InlineNarrowing_GetViaNarrowFace_HitsInterceptor()
    {
        var stub = new NarrowingInlineStub.Stubs.IInterfaceNarrow();
        stub.Prop.Get(42);

        IInterfaceNarrow narrow = stub;
        Assert.Equal(42, narrow.Prop);
    }

    [Fact]
    public void InlineNarrowing_SetViaWideFace_HitsInterceptor()
    {
        var stub = new NarrowingInlineStub.Stubs.IInterfaceNarrow();
        IInterfaceWide wide = stub;
        wide.Prop = 7;

        Assert.Equal(7, stub.Prop.LastSetValue);
        stub.Prop.VerifySet(Called.Once);
    }

    [Fact]
    public void InlineNarrowing_GetThenSetRouting_BothFaces()
    {
        var stub = new NarrowingInlineStub.Stubs.IInterfaceNarrow();
        stub.Prop.Get(100);

        IInterfaceNarrow narrow = stub;
        IInterfaceWide wide = stub;

        Assert.Equal(100, narrow.Prop);
        wide.Prop = 9;
        Assert.Equal(9, stub.Prop.LastSetValue);
        stub.Prop.VerifyGet(Called.Once);
        stub.Prop.VerifySet(Called.Once);
    }

    [Fact]
    public void StandaloneNarrowing_GetViaNarrowFace_HitsInterceptor()
    {
        var stub = new NarrowingStandaloneStub();
        stub.Prop.Get(5);

        IInterfaceNarrow narrow = stub;
        Assert.Equal(5, narrow.Prop);
    }

    [Fact]
    public void StandaloneNarrowing_SetViaWideFace_HitsInterceptor()
    {
        var stub = new NarrowingStandaloneStub();
        IInterfaceWide wide = stub;
        wide.Prop = 11;

        Assert.Equal(11, stub.Prop.LastSetValue);
        stub.Prop.VerifySet(Called.Once);
    }

    [Fact]
    public void WideningStandalone_SetViaDerivedFace_HitsInterceptor()
    {
        var stub = new WideningStandaloneStub();
        IGetSetFromGetOnly wide = stub;
        wide.Value = 17;

        Assert.Equal(17, stub.Value.LastSetValue);
        stub.Value.VerifySet(Called.Once);
    }

    [Fact]
    public void WideningStandalone_GetViaBaseFace_HitsInterceptor()
    {
        var stub = new WideningStandaloneStub();
        stub.Value.Get(23);

        IGetOnly baseFace = stub;
        Assert.Equal(23, baseFace.Value);
        stub.Value.VerifyGet(Called.Once);
    }

    [Fact]
    public void WideningInline_BothAccessors_Compile()
    {
        var stub = new WideningInlineStub.Stubs.IGetSetFromGetOnly();
        stub.Value.Get(1);
        IGetOnly getOnly = stub;
        Assert.Equal(1, getOnly.Value);

        IGetSetFromGetOnly wide = stub;
        wide.Value = 2;
        Assert.Equal(2, stub.Value.LastSetValue);
    }

    [Fact]
    public void OpenGeneric_Pattern8_Compiles_AndRoutes()
    {
        var stub = new ShadowedOpenGenericInlineStub.Stubs.IShadowed<int>();
        stub.Item.Get(3);

        IShadowed<int> narrow = stub;
        IShadowedBase<int> wide = stub;

        Assert.Equal(3, narrow.Item);
        wide.Item = 8;
        Assert.Equal(8, stub.Item.LastSetValue);
    }

    [Fact]
    public void GenericStandalone_Pattern2_Compiles_AndRoutes()
    {
        var stub = new ShadowedGenericStandaloneStub<int>();
        stub.Item.Get(4);

        IShadowed<int> narrow = stub;
        IShadowedBase<int> wide = stub;

        Assert.Equal(4, narrow.Item);
        wide.Item = 12;
        Assert.Equal(12, stub.Item.LastSetValue);
    }
}
