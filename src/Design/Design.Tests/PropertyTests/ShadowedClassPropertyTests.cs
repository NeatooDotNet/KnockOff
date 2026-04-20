// -----------------------------------------------------------------------------
// Design.Tests - Shadowed virtual properties in CLASS hierarchies (patterns
// 3, 4, 6, 9).
//
// Unlike interface shadowing (which produced uncompilable code — fixed in
// v0.56.0), class `new virtual` creates a genuinely separate v-table slot.
// The stub's target type is the DERIVED class, so the stub intercepts the
// `new` slot. Access via the BASE type is asking for the base slot — by
// design of C# `new`, it bypasses the shadow. This is correct behavior,
// not a bug: if the caller wants to intercept the base slot they should
// target the base class with its own stub.
//
// These tests pin the contract: interception works on the derived (`new`)
// member; access through a base-typed reference hits the base's own slot
// unchanged.
// -----------------------------------------------------------------------------

using Design.Domain.Abstractions;
using Design.Stubs.Properties;
using KnockOff;

namespace Design.Tests.PropertyTests;

public class ShadowedClassPropertyTests
{
    // -------------------------------------------------------------------------
    // Narrow-first: base is get/set, derived shadows with get-only.
    // -------------------------------------------------------------------------

    [Fact]
    public void NarrowClassStandalone_GetViaDerivedFace_HitsInterceptor()
    {
        var stub = new NarrowClassStandaloneStub();
        stub.Prop.Get(42);

        NarrowClassDerived derived = stub.Object;
        Assert.Equal(42, derived.Prop);
        stub.Prop.VerifyGet(Called.Once);
    }

    [Fact]
    public void NarrowClassStandalone_BaseFaceUsesBaseSlot_ByDesign()
    {
        // `new virtual` creates a distinct v-table slot. Access through
        // WideClassBase targets the base slot, which is outside the stub's
        // target type. The stub intentionally does not intercept it.
        var stub = new NarrowClassStandaloneStub();
        stub.Prop.Get(42);

        WideClassBase wide = stub.Object;
        Assert.Equal(0, wide.Prop);
        stub.Prop.VerifyGet(Called.Never);
    }

    // -------------------------------------------------------------------------
    // Widening: base is get-only, derived adds a setter (`new virtual`).
    // -------------------------------------------------------------------------

    [Fact]
    public void WideningClassStandalone_SetViaDerivedFace_HitsInterceptor()
    {
        var stub = new WideningClassStandaloneStub();
        GetSetFromGetOnlyClass derived = stub.Object;
        derived.Value = 17;

        Assert.Equal(17, stub.Value.LastSetValue);
        stub.Value.VerifySet(Called.Once);
    }

    [Fact]
    public void WideningClassStandalone_BaseFaceUsesBaseSlot_ByDesign()
    {
        // Reads through GetOnlyClassBase target the base's own virtual slot.
        var stub = new WideningClassStandaloneStub();
        stub.Value.Get(23);

        GetOnlyClassBase baseFace = stub.Object;
        Assert.Equal(0, baseFace.Value);
        stub.Value.VerifyGet(Called.Never);
    }

    // -------------------------------------------------------------------------
    // Pattern 4 — open generic class standalone.
    // -------------------------------------------------------------------------

    [Fact]
    public void ShadowedGenericClassStandalone_GetViaDerivedFace_HitsInterceptor()
    {
        var stub = new ShadowedGenericClassStandaloneStub<string>();
        stub.Item.Get("hello");

        ShadowedClassDerived<string> derived = stub.Object;
        Assert.Equal("hello", derived.Item);
        stub.Item.VerifyGet(Called.Once);
    }

    [Fact]
    public void ShadowedGenericClassStandalone_BaseFaceUsesBaseSlot_ByDesign()
    {
        var stub = new ShadowedGenericClassStandaloneStub<string>();
        stub.Item.Get("configured");

        ShadowedClassBase<string> baseFace = stub.Object;
        Assert.Null(baseFace.Item);
        stub.Item.VerifyGet(Called.Never);
    }
}
