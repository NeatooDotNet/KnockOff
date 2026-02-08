// -----------------------------------------------------------------------------
// Design.Tests - Void User Method Fallback Bug Tests
// -----------------------------------------------------------------------------
// These tests demonstrate a bug where user method overrides are not called
// as fallback when the method parameters use custom types (non-primitive).
//
// ROOT CAUSE:
// The generator detects user overrides syntactically (type name as written in
// source, e.g., "Order") and builds a signature key like "SaveOrder_(Order)".
// The builder later checks for user overrides using the semantic model's
// fully-qualified type name, producing "SaveOrder_(Design.Domain.Services.Order)".
// These keys don't match, so HasUserOverride is false.
//
// SCOPE:
// This bug affects ALL user methods with custom-type parameters, not just void
// ones. The void case is more visible because the method silently does nothing,
// while non-void methods return default (null for reference types).
//
// The bug is in the FlatModelBuilder pipeline (patterns 1 and 2) and
// StandaloneClassModelBuilder pipeline (patterns 3 and 4).
//
// TESTS IN THIS FILE:
// - Tests 1-2: BUG CASES - custom-type parameters (should fail today)
// - Tests 3-4: CONTROL CASES - primitive parameters (should pass today)
// - Test 5: OnCall still works for custom-type parameters (interceptor is fine)
// - Test 6: Verify the user method IS callable (it's defined, just not wired)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.UserMethods;
using KnockOff;

namespace Design.Tests.UserMethodTests;

/// <summary>
/// Tests for the void user method fallback bug. Methods with custom-type
/// parameters do not call user method overrides as fallback.
/// </summary>
public class VoidUserMethodFallbackTests
{
    // =========================================================================
    // Test 1: BUG - Void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no OnCall is configured, calling SaveOrder should invoke
    //           the user method SaveOrder_ as fallback, setting SaveOrderCalled
    //           to true and capturing the order.
    // ACTUAL:   SaveOrder_ is never called. SaveOrderCalled remains false.
    //           The void method silently does nothing.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_UserMethodShouldBeCalledAsFallback()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        var order = new Order { Id = 1, Description = "Test", Amount = 99.99m };

        // Call the void method with no OnCall configured
        service.SaveOrder(order);

        // BUG: SaveOrder_ user method is NOT called as fallback.
        // The following assertions should pass but FAIL due to the bug.
        Assert.True(stub.SaveOrderCalled, "SaveOrder_ user method should have been called as fallback");
        Assert.Same(order, stub.LastSavedOrder);
    }

    // =========================================================================
    // Test 2: BUG - Non-void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no OnCall/Returns is configured, calling FormatOrder
    //           should invoke the user method FormatOrder_ as fallback and
    //           return its formatted string.
    // ACTUAL:   FormatOrder_ is never called. FormatOrder returns null (default!).
    // =========================================================================

    [Fact]
    public void NonVoidMethod_CustomType_UserMethodShouldBeCalledAsFallback()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        var order = new Order { Id = 42, Description = "Widget", Amount = 19.99m };

        // Call the non-void method with no OnCall/Returns configured
        var result = service.FormatOrder(order);

        // BUG: FormatOrder_ user method is NOT called as fallback.
        // The following assertion should pass but FAILS due to the bug.
        // Instead of the formatted string, result is null (default!).
        Assert.NotNull(result);
        Assert.Equal("Order #42: Widget ($19.99)", result);
    }

    // =========================================================================
    // Test 3: CONTROL - Void method with primitive parameter (should pass)
    // =========================================================================
    // This test passes because "string" type normalizes the same way in both
    // syntactic and semantic analysis, so HasUserOverride is correctly true.
    // =========================================================================

    [Fact]
    public void VoidMethod_PrimitiveType_UserMethodIsCalledAsFallback()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        // Call the void method with no OnCall configured
        service.LogMessage("Hello, World!");

        // WORKS: LogMessage_ user method IS called because "string" matches
        Assert.Single(stub.LoggedMessages);
        Assert.Equal("Hello, World!", stub.LoggedMessages[0]);
    }

    // =========================================================================
    // Test 4: CONTROL - Non-void method with primitive parameter (should pass)
    // =========================================================================
    // This test passes because "int" type normalizes the same way in both
    // syntactic and semantic analysis, so HasUserOverride is correctly true.
    // =========================================================================

    [Fact]
    public void NonVoidMethod_PrimitiveType_UserMethodIsCalledAsFallback()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        // Call the non-void method with no OnCall/Returns configured
        var result = service.GetStatus(200);

        // WORKS: GetStatus_ user method IS called because "int" matches
        Assert.Equal("OK", result);
    }

    // =========================================================================
    // Test 5: OnCall still works for custom-type parameters
    // =========================================================================
    // Even though the user method fallback is broken, OnCall should still work
    // because it's configured on the interceptor directly, not via the user
    // method detection path.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_OnCallStillWorks()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        var captured = false;
        stub.SaveOrder.Call(order => captured = true);

        service.SaveOrder(new Order { Id = 1 });

        // OnCall works regardless of the user method detection bug
        Assert.True(captured);
        stub.SaveOrder.Verify(Called.Once);
    }

    [Fact]
    public void NonVoidMethod_CustomType_OnCallStillWorks()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        stub.FormatOrder.Return(order => $"Custom: {order.Id}");

        var result = service.FormatOrder(new Order { Id = 99 });

        // OnCall works regardless of the user method detection bug
        Assert.Equal("Custom: 99", result);
        stub.FormatOrder.Verify(Called.Once);
    }

    // =========================================================================
    // Test 6: Verify tracking still works for custom-type parameters
    // =========================================================================
    // Tracking (Verify, LastArg) should work for all methods, even when the
    // user method fallback is broken.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_TrackingWorks()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

        var order = new Order { Id = 1, Description = "Test" };
        service.SaveOrder(order);
        service.SaveOrder(new Order { Id = 2 });

        // Tracking works even though user method fallback is broken
        stub.SaveOrder.Verify(Called.Exactly(2));
    }

    // =========================================================================
    // Test 7: Multiple calls - void method should accumulate via user method
    // =========================================================================
    // EXPECTED: Each call to SaveOrder should invoke SaveOrder_, updating the
    //           tracked state for each call.
    // ACTUAL:   SaveOrder_ is never called, so state is never updated.
    // =========================================================================

    [Fact]
    public void VoidMethod_CustomType_MultipleCalls_UserMethodCalledEachTime()
    {
        var stub = new VoidUserMethodFallbackStub();
        IVoidUserMethodService service = stub;

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
