using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 6: IntelliSense Simulation
// This is the MOST IMPORTANT scenario.
//
// CRITICAL FINDING: Extension methods on generic interfaces are NOT discoverable
// when calling them on a concrete class that implements those interfaces.
//
// This means:
// - Typing "compositor." will NOT show Call/When/WhenPredicate in IntelliSense
// - Users must explicitly cast to the interface to see extension methods
// - This KILLS the approach for user experience purposes
//
// The extension methods ARE discoverable on the interface type directly:
// - Typing "((IVoidOverload<ProcessDelegate1, string>)compositor)." DOES show them
// - But this requires knowing the full generic interface type, which is worse
//   than the current generated-method approach
// ============================================================================

public class Scenario6_IntelliSenseSimulation
{
    // -------------------------------------------------------------------
    // CRITICAL TEST: Extension methods are NOT discoverable on compositor
    // -------------------------------------------------------------------

    // DOES NOT COMPILE: CS1061
    // [Fact]
    // public void ExtensionMethods_AreNOTDiscoverable_OnCompositorType()
    // {
    //     var compositor = new ProcessCompositor();
    //     // NONE of these compile:
    //     string r1 = compositor.Call((string data) => { });      // CS1061
    //     string r2 = compositor.When("hello");                    // CS1061
    //     string r3 = compositor.WhenPredicate((string data) => data.Length > 0); // CS1061
    // }

    // -------------------------------------------------------------------
    // Extension methods ARE discoverable on interface references
    // -------------------------------------------------------------------

    [Fact]
    public void ExtensionMethods_AreDiscoverable_OnInterfaceType()
    {
        var compositor = new ProcessCompositor();

        // Cast to the interface - now extension methods ARE available
        IVoidOverload<ProcessDelegate1, string> asInterface = compositor;

        // These compile and IntelliSense would show Call, When, WhenPredicate
        // when typing "asInterface."
        string r1 = asInterface.Call((string data) => { });
        string r2 = asInterface.When("hello");
        string r3 = asInterface.WhenPredicate((string data) => data.Length > 0);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.NotNull(r3);
    }

    // -------------------------------------------------------------------
    // All overloads resolve when using explicit interface references
    // -------------------------------------------------------------------

    [Fact]
    public void MultipleOverloads_AllResolve_WhenExplicitlyCast()
    {
        var compositor = new ProcessCompositor();

        IVoidOverload<ProcessDelegate1, string> ov1 = compositor;
        IVoidOverload<ProcessDelegate2, (string, int)> ov2 = compositor;
        IVoidOverload<ProcessDelegate3, (string, int, bool)> ov3 = compositor;

        var r1 = ov1.Call((string data) => { });
        var r2 = ov2.Call((string data, int priority) => { });
        var r3 = ov3.Call((string data, int priority, bool flag) => { });

        Assert.Equal("VoidCall:ProcessDelegate1", r1);
        Assert.Equal("VoidCall:ProcessDelegate2", r2);
        Assert.Equal("VoidCall:ProcessDelegate3", r3);
    }

    // -------------------------------------------------------------------
    // Explicit generic type args as escape hatch
    // -------------------------------------------------------------------

    [Fact]
    public void ExplicitGenericTypeArgs_WorksButIsVerbose()
    {
        var compositor = new ProcessCompositor();

        // Users could specify type args explicitly, but this is extremely verbose
        var result = VoidOverloadExtensions
            .Call<ProcessDelegate2, (string, int)>(compositor, (data, priority) => { });

        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }

    // -------------------------------------------------------------------
    // Method groups
    // -------------------------------------------------------------------

    private static void HandleString(string data) { }
    private static void HandleStringAndInt(string data, int priority) { }

    // DOES NOT COMPILE: CS1061 - method groups do not help
    // [Fact]
    // public void MethodGroup_AlsoDoesNotResolve()
    // {
    //     var compositor = new ProcessCompositor();
    //     var result = compositor.Call(HandleString);
    // }

    [Fact]
    public void MethodGroup_WorksWithExplicitCast()
    {
        var compositor = new ProcessCompositor();

        IVoidOverload<ProcessDelegate1, string> ov1 = compositor;
        var result = ov1.Call(HandleString);

        Assert.Equal("VoidCall:ProcessDelegate1", result);
    }

    [Fact]
    public void MethodGroup_DifferentOverload_WorksWithExplicitCast()
    {
        var compositor = new ProcessCompositor();

        IVoidOverload<ProcessDelegate2, (string, int)> ov2 = compositor;
        var result = ov2.Call(HandleStringAndInt);

        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }
}
