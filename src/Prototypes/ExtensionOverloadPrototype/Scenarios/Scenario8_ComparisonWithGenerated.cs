using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 8: Comparison with Direct Generated Methods
// Creates a parallel compositor using traditional generated forwarding methods.
// This is what v0.50.0 does now. Compare behavior with extension approach.
//
// FINDING: Generated methods are SUPERIOR in every UX dimension:
// - Direct calls work without casts
// - IntelliSense shows all overloads immediately
// - Error messages are more specific
// - When() uses flat parameters (natural) instead of tuples
// - Implicit lambdas may resolve (generated) vs never resolve (extension)
// ============================================================================

// ---- Traditional generated compositor ----
public class GeneratedProcessCompositor
{
    /// <summary>Configures callback for Process(string data).</summary>
    public string Call(ProcessDelegate1 callback) => $"GenCall:ProcessDelegate1";

    /// <summary>Configures callback for Process(string data, int priority).</summary>
    public string Call(ProcessDelegate2 callback) => $"GenCall:ProcessDelegate2";

    /// <summary>Configures callback for Process(string data, int priority, bool flag).</summary>
    public string Call(ProcessDelegate3 callback) => $"GenCall:ProcessDelegate3";

    /// <summary>When match for Process(string data).</summary>
    public string When(string data) => $"GenWhen:String";

    /// <summary>When match for Process(string data, int priority).</summary>
    public string When(string data, int priority) => $"GenWhen:String+Int32";

    /// <summary>When match for Process(string data, int priority, bool flag).</summary>
    public string When(string data, int priority, bool flag) => $"GenWhen:String+Int32+Boolean";
}

public class GeneratedCalcCompositor
{
    public string Return(CalcDelegate1 callback) => $"GenReturn:CalcDelegate1";
    public string Return(CalcDelegate2 callback) => $"GenReturn:CalcDelegate2";
    public string ReturnValue(int value) => $"GenReturnValue:Int32={value}";
    public string When(int value) => $"GenWhen:Int32";
    public string When(int a, int b) => $"GenWhen:Int32+Int32";
}


public class Scenario8_ComparisonWithGenerated
{
    // =====================================================================
    // KEY DIFFERENCE: Generated methods work directly, extensions do not
    // =====================================================================

    [Fact]
    public void Generated_Call_WorksDirectly_NocastNeeded()
    {
        var gen = new GeneratedProcessCompositor();

        // NO cast needed. IntelliSense shows all 3 Call overloads.
        var r1 = gen.Call((string data) => { });
        var r2 = gen.Call((string data, int priority) => { });
        var r3 = gen.Call((string data, int priority, bool flag) => { });

        Assert.Equal("GenCall:ProcessDelegate1", r1);
        Assert.Equal("GenCall:ProcessDelegate2", r2);
        Assert.Equal("GenCall:ProcessDelegate3", r3);
    }

    // The extension approach requires a cast for every call:
    [Fact]
    public void Extension_Call_RequiresCast_Inconvenient()
    {
        var ext = new ProcessCompositor();

        // Must cast to each interface
        IVoidOverload<ProcessDelegate1, string> ov1 = ext;
        IVoidOverload<ProcessDelegate2, (string, int)> ov2 = ext;
        IVoidOverload<ProcessDelegate3, (string, int, bool)> ov3 = ext;

        var r1 = ov1.Call((string data) => { });
        var r2 = ov2.Call((string data, int priority) => { });
        var r3 = ov3.Call((string data, int priority, bool flag) => { });

        Assert.Equal("VoidCall:ProcessDelegate1", r1);
        Assert.Equal("VoidCall:ProcessDelegate2", r2);
        Assert.Equal("VoidCall:ProcessDelegate3", r3);
    }

    // =====================================================================
    // When resolution comparison
    // =====================================================================

    [Fact]
    public void Generated_When_UsesFlatParams_Natural()
    {
        var gen = new GeneratedProcessCompositor();

        // Flat params - natural syntax
        var r1 = gen.When("hello");
        var r2 = gen.When("hello", 5);
        var r3 = gen.When("hello", 5, true);

        Assert.Equal("GenWhen:String", r1);
        Assert.Equal("GenWhen:String+Int32", r2);
        Assert.Equal("GenWhen:String+Int32+Boolean", r3);
    }

    [Fact]
    public void Extension_When_RequiresTuples_LessNatural()
    {
        var ext = new ProcessCompositor();

        // Must cast AND use tuples for multi-arg
        IVoidOverload<ProcessDelegate1, string> ov1 = ext;
        IVoidOverload<ProcessDelegate2, (string, int)> ov2 = ext;

        var r1 = ov1.When("hello");
        var r2 = ov2.When(("hello", 5)); // Tuple wrapping - less natural

        Assert.Contains("String", r1);
        Assert.Contains("Tuple", r2);
    }

    // =====================================================================
    // ReturnValue comparison
    // =====================================================================

    [Fact]
    public void Generated_ReturnValue_SingleMethod_NoAmbiguity()
    {
        var gen = new GeneratedCalcCompositor();

        // One ReturnValue method - unambiguous
        var result = gen.ReturnValue(42);
        Assert.Equal("GenReturnValue:Int32=42", result);
    }

    // Extension: ReturnValue(42) is ambiguous when multiple interfaces
    // share the same TReturn type (both return int). Must cast.
    [Fact]
    public void Extension_ReturnValue_RequiresCast_WhenSameReturnType()
    {
        var calc = new CalcCompositor();

        // Must cast to specific interface to disambiguate
        var asOv1 = (IMethodOverload<CalcDelegate1, int, int>)calc;
        var result = asOv1.ReturnValue(42);
        Assert.Equal("MethodReturnValue:Int32=42", result);
    }

    // =====================================================================
    // Summary
    // =====================================================================

    [Fact]
    public void ComparisonSummary()
    {
        // ================================================================
        // EXTENSION METHODS vs GENERATED METHODS: DEFINITIVE COMPARISON
        // ================================================================
        //
        // CATEGORY                          | EXTENSION | GENERATED | WINNER
        // ----------------------------------|-----------|-----------|--------
        // Direct calls (no cast)            | NO        | YES       | GENERATED
        // IntelliSense on compositor.       | NO        | YES       | GENERATED
        // Call() with explicit lambda       | cast req. | direct    | GENERATED
        // Call() with implicit lambda       | NO        | maybe     | GENERATED
        // When() single arg                 | cast req. | direct    | GENERATED
        // When() multi arg                  | tuples    | flat      | GENERATED
        // ReturnValue() same return type    | ambiguous | works     | GENERATED
        // Error messages                    | CS0411    | CS1503    | GENERATED
        // Method groups                     | cast req. | direct    | GENERATED
        // Library code size                 | small     | large     | EXTENSION
        // Build time (code gen)             | less      | more      | EXTENSION
        //
        // VERDICT: Extension methods on generic interfaces CANNOT provide
        // the user experience needed. The compiler's inability to discover
        // extension methods on concrete types that implement generic interfaces
        // is a fundamental limitation that makes this approach unusable.
        //
        // The only advantages (smaller library, less generated code) are
        // overwhelmingly outweighed by the terrible UX.
        //
        // RECOMMENDATION: Continue with generated forwarding methods.
        // The extension method approach is DEAD for compositor patterns.

        Assert.True(true, "See comparison table in comments");
    }
}
