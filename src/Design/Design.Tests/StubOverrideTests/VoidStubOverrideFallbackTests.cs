// -----------------------------------------------------------------------------
// Design.Tests - Void Stub Override Fallback Bug Tests
// -----------------------------------------------------------------------------
// These tests demonstrate a bug where stub override overrides are not called
// as fallback when the method parameters use custom types (non-primitive).
//
// ROOT CAUSE:
// The generator detects stub overrides syntactically (type name as written in
// source, e.g., "Order") and builds a signature key like "SaveOrder_(Order)".
// The builder later checks for stub overrides using the semantic model's
// fully-qualified type name, producing "SaveOrder_(Design.Domain.Services.Order)".
// These keys don't match, so HasStubOverride is false.
//
// SCOPE:
// This bug affects ALL stub overrides with custom-type parameters, not just void
// ones. The void case is more visible because the method silently does nothing,
// while non-void methods return default (null for reference types).
//
// The bug is in the FlatModelBuilder pipeline (patterns 1 and 2) and
// StandaloneClassModelBuilder pipeline (patterns 3 and 4).
//
// TESTS IN THIS FILE:
// - Tests 1-2: BUG CASES - custom-type parameters (should fail today)
// - Tests 3-4: CONTROL CASES - primitive parameters (should pass today)
// - Test 5: Call still works for custom-type parameters (interceptor is fine)
// - Test 6: Verify the stub override IS callable (it's defined, just not wired)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.StubOverrides;
using KnockOff;

namespace Design.Tests.StubOverrideTests;

/// <summary>
/// Tests for the void stub override fallback bug. Methods with custom-type
/// parameters do not call stub override overrides as fallback.
/// </summary>
public class VoidStubOverrideFallbackTests
{
    // =========================================================================
    // Test 1: BUG - Void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no Call is configured, calling SaveOrder should invoke
    //           the stub override SaveOrder_ as fallback, setting SaveOrderCalled
    //           to true and capturing the order.
    // ACTUAL:   SaveOrder_ is never called. SaveOrderCalled remains false.
    //           The void method silently does nothing.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_StubOverrideShouldBeCalledAsFallback()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        var order = new Order { Id = 1, Description = "Test", Amount = 99.99m };

        // Call the void method with no Call configured
        service.SaveOrder(order);

        // BUG: SaveOrder_ stub override is NOT called as fallback.
        // The following assertions should pass but FAIL due to the bug.
        Assert.True(stub.SaveOrderCalled, "SaveOrder_ stub override should have been called as fallback");
        Assert.Same(order, stub.LastSavedOrder);
    }

    // =========================================================================
    // Test 2: BUG - Non-void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no Call/Return is configured, calling FormatOrder
    //           should invoke the stub override FormatOrder_ as fallback and
    //           return its formatted string.
    // ACTUAL:   FormatOrder_ is never called. FormatOrder returns null (default!).
    // =========================================================================

    [Fact]
    public void NonVoidMethod_CustomType_StubOverrideShouldBeCalledAsFallback()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        var order = new Order { Id = 42, Description = "Widget", Amount = 19.99m };

        // Call the non-void method with no Call/Return configured
        var result = service.FormatOrder(order);

        // BUG: FormatOrder_ stub override is NOT called as fallback.
        // The following assertion should pass but FAILS due to the bug.
        // Instead of the formatted string, result is null (default!).
        Assert.NotNull(result);
        Assert.Equal("Order #42: Widget ($19.99)", result);
    }

    // =========================================================================
    // Test 3: CONTROL - Void method with primitive parameter (should pass)
    // =========================================================================
    // This test passes because "string" type normalizes the same way in both
    // syntactic and semantic analysis, so HasStubOverride is correctly true.
    // =========================================================================

    [Fact]
    public void VoidMethod_PrimitiveType_StubOverrideIsCalledAsFallback()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        // Call the void method with no Call configured
        service.LogMessage("Hello, World!");

        // WORKS: LogMessage_ stub override IS called because "string" matches
        Assert.Single(stub.LoggedMessages);
        Assert.Equal("Hello, World!", stub.LoggedMessages[0]);
    }

    // =========================================================================
    // Test 4: CONTROL - Non-void method with primitive parameter (should pass)
    // =========================================================================
    // This test passes because "int" type normalizes the same way in both
    // syntactic and semantic analysis, so HasStubOverride is correctly true.
    // =========================================================================

    [Fact]
    public void NonVoidMethod_PrimitiveType_StubOverrideIsCalledAsFallback()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        // Call the non-void method with no Call/Return configured
        var result = service.GetStatus(200);

        // WORKS: GetStatus_ stub override IS called because "int" matches
        Assert.Equal("OK", result);
    }

    // =========================================================================
    // Test 5: Call still works for custom-type parameters
    // =========================================================================
    // Even though the stub override fallback is broken, Call should still work
    // because it's configured on the interceptor directly, not via the user
    // method detection path.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_OnCallStillWorks()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        var captured = false;
        stub.SaveOrder.Call(order => captured = true);

        service.SaveOrder(new Order { Id = 1 });

        // Call works regardless of the stub override detection bug
        Assert.True(captured);
        stub.SaveOrder.Verify(Called.Once);
    }

    [Fact]
    public void NonVoidMethod_CustomType_OnCallStillWorks()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        stub.FormatOrder.Call(order => $"Custom: {order.Id}");

        var result = service.FormatOrder(new Order { Id = 99 });

        // Call works regardless of the stub override detection bug
        Assert.Equal("Custom: 99", result);
        stub.FormatOrder.Verify(Called.Once);
    }

    // =========================================================================
    // Test 6: Verify tracking still works for custom-type parameters
    // =========================================================================
    // Tracking (Verify, LastArg) should work for all methods, even when the
    // stub override fallback is broken.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_TrackingWorks()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        var order = new Order { Id = 1, Description = "Test" };
        service.SaveOrder(order);
        service.SaveOrder(new Order { Id = 2 });

        // Tracking works even though stub override fallback is broken
        stub.SaveOrder.Verify(Called.Exactly(2));
    }

    // =========================================================================
    // Test 7: Multiple calls - void method should accumulate via stub override
    // =========================================================================
    // EXPECTED: Each call to SaveOrder should invoke SaveOrder_, updating the
    //           tracked state for each call.
    // ACTUAL:   SaveOrder_ is never called, so state is never updated.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_MultipleCalls_StubOverrideCalledEachTime()
    {
        var stub = new VoidStubOverrideFallbackStub();
        IVoidStubOverrideService service = stub;

        var order1 = new Order { Id = 1, Description = "First" };
        var order2 = new Order { Id = 2, Description = "Second" };

        service.SaveOrder(order1);
        service.SaveOrder(order2);

        // BUG: SaveOrder_ is never called, so LastSavedOrder is null
        // EXPECTED: LastSavedOrder should be order2 (the last call)
        Assert.True(stub.SaveOrderCalled, "SaveOrder_ should have been called");
        Assert.Same(order2, stub.LastSavedOrder);
    }
}
