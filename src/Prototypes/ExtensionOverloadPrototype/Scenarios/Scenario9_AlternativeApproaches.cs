using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 9: Alternative Approaches
// Since direct extension methods on generic interfaces fail, explore whether
// any workaround could make extension methods viable.
// ============================================================================

// --- Alternative A: Non-generic marker interface with generated extension class ---
// Instead of IVoidOverload<TDelegate, TArgs>, use a non-generic marker and
// generate a SMALL extension class specific to the compositor. The extension
// class has concrete overloads that delegate to the interfaces.
//
// This is a "hybrid" approach: the behavioral logic is in the library (via the
// generic interfaces), but the user-facing API is in a thin generated extension class.

public interface ICompositorMarker { }

// "Generated" extension class (small, per-compositor)
public static class ProcessCompositorExtensions
{
    // These are thin forwarding methods. The logic lives in the library extension methods.
    public static string Call(this ProcessCompositorV2 self, ProcessDelegate1 callback)
        => ((IVoidOverload<ProcessDelegate1, string>)self).Call(callback);

    public static string Call(this ProcessCompositorV2 self, ProcessDelegate2 callback)
        => ((IVoidOverload<ProcessDelegate2, (string, int)>)self).Call(callback);

    public static string Call(this ProcessCompositorV2 self, ProcessDelegate3 callback)
        => ((IVoidOverload<ProcessDelegate3, (string, int, bool)>)self).Call(callback);

    // When with flat parameters (like generated approach)
    public static string When(this ProcessCompositorV2 self, string data)
        => ((IVoidOverload<ProcessDelegate1, string>)self).When(data);

    public static string When(this ProcessCompositorV2 self, string data, int priority)
        => ((IVoidOverload<ProcessDelegate2, (string, int)>)self).When((data, priority));

    public static string When(this ProcessCompositorV2 self, string data, int priority, bool flag)
        => ((IVoidOverload<ProcessDelegate3, (string, int, bool)>)self).When((data, priority, flag));
}

// "Generated" compositor V2 with marker interface
public class ProcessCompositorV2 : ICompositorMarker,
    IVoidOverload<ProcessDelegate1, string>,
    IVoidOverload<ProcessDelegate2, (string data, int priority)>,
    IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>
{
    object IVoidOverload<ProcessDelegate1, string>.GetInterceptor() => "interceptor1";
    object IVoidOverload<ProcessDelegate2, (string data, int priority)>.GetInterceptor() => "interceptor2";
    object IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>.GetInterceptor() => "interceptor3";
}

// --- Alternative B: Property accessors returning typed interfaces ---
// Instead of extension methods, the compositor exposes properties that return
// typed interfaces. This enables fluent API without casts.

public class ProcessCompositorV3
{
    public IVoidOverload<ProcessDelegate1, string> Process1Arg { get; }
    public IVoidOverload<ProcessDelegate2, (string, int)> Process2Args { get; }
    public IVoidOverload<ProcessDelegate3, (string, int, bool)> Process3Args { get; }

    public ProcessCompositorV3()
    {
        Process1Arg = new OverloadAdapter<ProcessDelegate1, string>("interceptor1");
        Process2Args = new OverloadAdapter<ProcessDelegate2, (string, int)>("interceptor2");
        Process3Args = new OverloadAdapter<ProcessDelegate3, (string, int, bool)>("interceptor3");
    }

    private class OverloadAdapter<TDelegate, TArgs> : IVoidOverload<TDelegate, TArgs>
        where TDelegate : Delegate
    {
        private readonly string _interceptorId;
        public OverloadAdapter(string interceptorId) => _interceptorId = interceptorId;
        public object GetInterceptor() => _interceptorId;
    }
}


public class Scenario9_AlternativeApproaches
{
    // =====================================================================
    // Alternative A: Generated thin extension class
    // =====================================================================

    [Fact]
    public void AlternativeA_Call_WorksDirectly_LikeGenerated()
    {
        var compositor = new ProcessCompositorV2();

        // Extension methods are found because they target the CONCRETE type
        var r1 = compositor.Call((string data) => { });
        var r2 = compositor.Call((string data, int priority) => { });
        var r3 = compositor.Call((string data, int priority, bool flag) => { });

        Assert.Equal("VoidCall:ProcessDelegate1", r1);
        Assert.Equal("VoidCall:ProcessDelegate2", r2);
        Assert.Equal("VoidCall:ProcessDelegate3", r3);
    }

    [Fact]
    public void AlternativeA_When_FlatParams_LikeGenerated()
    {
        var compositor = new ProcessCompositorV2();

        // Flat params - the extension method handles the tuple wrapping internally
        var r1 = compositor.When("hello");
        var r2 = compositor.When("hello", 5);
        var r3 = compositor.When("hello", 5, true);

        Assert.Equal("VoidWhen:String", r1);
        Assert.Contains("Tuple", r2);
        Assert.Contains("Tuple", r3);
    }

    [Fact]
    public void AlternativeA_IntelliSense_ShowsAllOverloads()
    {
        var compositor = new ProcessCompositorV2();

        // Typing "compositor." shows Call and When with multiple overloads
        // because ProcessCompositorExtensions targets the concrete type.
        var call1 = compositor.Call((string data) => { });
        var call2 = compositor.Call((string data, int priority) => { });
        var when1 = compositor.When("hello");
        var when2 = compositor.When("hello", 5);

        Assert.NotNull(call1);
        Assert.NotNull(call2);
        Assert.NotNull(when1);
        Assert.NotNull(when2);
    }

    // =====================================================================
    // Alternative B: Property accessors
    // =====================================================================

    [Fact]
    public void AlternativeB_PropertyAccess_ThenExtensionMethod()
    {
        var compositor = new ProcessCompositorV3();

        // Access the typed property, then call extension method
        var result = compositor.Process1Arg.Call((string data) => { });

        Assert.Equal("VoidCall:ProcessDelegate1", result);
    }

    [Fact]
    public void AlternativeB_When_OnTypedProperty()
    {
        var compositor = new ProcessCompositorV3();

        var result = compositor.Process1Arg.When("hello");

        Assert.Equal("VoidWhen:String", result);
    }

    // =====================================================================
    // Comparison: What does each approach generate?
    // =====================================================================

    [Fact]
    public void GenerationComparison()
    {
        // ================================================================
        // WHAT NEEDS TO BE GENERATED PER COMPOSITOR
        // ================================================================
        //
        // Approach                  | Generated per compositor
        // --------------------------|------------------------------------------
        // Current (v0.50.0)         | Full Call/When/Return/Verify methods
        //                           | with all logic inlined
        //
        // Pure Extension (failed)   | Just interface list (tiny)
        //                           | BUT user experience is terrible
        //
        // Alt A (thin extensions)   | Interface list + thin forwarding extensions
        //                           | Extensions are ~2 lines each
        //                           | Logic stays in library
        //                           | UX identical to current approach
        //
        // Alt B (properties)        | Property per overload
        //                           | UX is compositor.Process1Arg.Call(...)
        //                           | NOT ideal - users need to know property names
        //
        // ================================================================
        // VERDICT ON ALTERNATIVES
        // ================================================================
        //
        // Alternative A (thin generated extensions) is VIABLE and worth
        // further investigation. It provides:
        // - Same UX as fully generated methods
        // - Much less generated code (forwarding stubs only)
        // - All behavioral logic in library (pre-compiled)
        // - IntelliSense works correctly
        // - Flat params for When() (extension handles tuple wrapping)
        //
        // Alternative B (properties) has WORSE UX than current approach.
        // Users must know the property name for each overload.
        //
        // RECOMMENDATION:
        // The PURE extension method approach is dead.
        // Alternative A (thin generated extensions forwarding to library
        // extension methods on generic interfaces) may be worth a
        // separate prototype focused on build time measurement.

        Assert.True(true, "See generation comparison in comments");
    }
}
