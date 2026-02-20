using KnockOff;

namespace KnockOff.Documentation.Samples.OverloadSameTypes;

// =============================================================================
// CS0121 Regression Test Interface
// =============================================================================
//
// This interface triggers CS0121 (ambiguous overload) with the current v0.52.0
// tuple-based delegates. Both overloads have the same element types (all string),
// differing only in tuple element count. C# lambda type inference cannot
// distinguish Func<(string, string), bool> from Func<(string, string, string), bool>.
//
// The interface and stubs compile fine. The CS0121 bug only manifests when
// attempting to use stub.Check.Call(args => ...) with a bare lambda, because
// the compiler cannot infer which Func<tuple, bool> the lambda targets.
//
// After the delegate-based Call API fix (Phase 3), Call usage will be added here
// to prove the CS0121 bug is resolved.
// =============================================================================

public interface IOverloadSameTypes
{
    bool Check(string a, string b);
    bool Check(string a, string b, string c);
}

// =============================================================================
// Standalone Stub
// =============================================================================

[KnockOff]
public partial class OverloadSameTypesStub : IOverloadSameTypes { }

// =============================================================================
// Inline Stub
// =============================================================================

[KnockOff<IOverloadSameTypes>]
public partial class OverloadSameTypesInlineTests
{
}

public partial class OverloadSameTypesInlineTests
{
    // -----------------------------------------------------------------
    // Basic tests using Return(value) -- these work fine today because
    // Return(value) takes a concrete type (bool), not a delegate/lambda,
    // so there is no overload ambiguity.
    // -----------------------------------------------------------------

    [Fact]
    public void Standalone_Return_TwoParam_Works()
    {
        var stub = new OverloadSameTypesStub();

        // Return(value) uses exact-match When, which disambiguates by parameter count
        stub.Check.When("a", "b").Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("a", "b"));
    }

    [Fact]
    public void Standalone_Return_ThreeParam_Works()
    {
        var stub = new OverloadSameTypesStub();

        stub.Check.When("a", "b", "c").Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("a", "b", "c"));
    }

    [Fact]
    public void Inline_Return_TwoParam_Works()
    {
        var stub = new Stubs.IOverloadSameTypes();

        stub.Check.When("a", "b").Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("a", "b"));
    }

    [Fact]
    public void Inline_Return_ThreeParam_Works()
    {
        var stub = new Stubs.IOverloadSameTypes();

        stub.Check.When("a", "b", "c").Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("a", "b", "c"));
    }

    // -----------------------------------------------------------------
    // CS0121 regression tests:
    //
    // Before the delegate-based Call API fix (v0.52.0), the following
    // Call usage would trigger CS0121 (ambiguous overload) because
    // Func<(string, string), bool> and Func<(string, string, string), bool>
    // are indistinguishable to C# lambda type inference.
    //
    // After the fix, each overload gets its own named delegate type:
    //   CheckDelegate for Check(string a, string b)
    //   CheckDelegate2 for Check(string a, string b, string c)
    //
    // The compiler now resolves the correct overload by matching the
    // lambda parameter count and types to the delegate signature.
    // -----------------------------------------------------------------

    [Fact]
    public void Standalone_Call_TwoParam_CS0121Regression()
    {
        // CS0121 regression test -- this would not compile with tuple-based delegates
        var stub = new OverloadSameTypesStub();

        stub.Check.Call((string a, string b) => a == "x");

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("x", "y"));
        Assert.False(svc.Check("y", "x"));
    }

    [Fact]
    public void Standalone_Call_ThreeParam_CS0121Regression()
    {
        // CS0121 regression test -- this would not compile with tuple-based delegates
        var stub = new OverloadSameTypesStub();

        stub.Check.Call((string a, string b, string c) => a == "x" && c == "z");

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("x", "y", "z"));
        Assert.False(svc.Check("x", "y", "w"));
    }

    [Fact]
    public void Inline_Call_TwoParam_CS0121Regression()
    {
        // CS0121 regression test -- inline pattern
        var stub = new Stubs.IOverloadSameTypes();

        stub.Check.Call((string a, string b) => a == "x");

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("x", "y"));
        Assert.False(svc.Check("y", "x"));
    }

    [Fact]
    public void Inline_Call_ThreeParam_CS0121Regression()
    {
        // CS0121 regression test -- inline pattern
        var stub = new Stubs.IOverloadSameTypes();

        stub.Check.Call((string a, string b, string c) => a == "x" && c == "z");

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("x", "y", "z"));
        Assert.False(svc.Check("x", "y", "w"));
    }

    [Fact]
    public void Standalone_WhenPredicate_TwoParam_CS0121Regression()
    {
        // CS0121 regression test -- When predicate also had the same ambiguity
        var stub = new OverloadSameTypesStub();

        stub.Check.When((string a, string b) => a.StartsWith("V", StringComparison.Ordinal)).Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("VIP", "user"));
        Assert.False(svc.Check("regular", "user")); // no match, default false
    }

    [Fact]
    public void Standalone_WhenPredicate_ThreeParam_CS0121Regression()
    {
        // CS0121 regression test -- When predicate with 3-param overload
        var stub = new OverloadSameTypesStub();

        stub.Check.When((string a, string b, string c) => c == "admin").Return(true);

        IOverloadSameTypes svc = stub;
        Assert.True(svc.Check("any", "any", "admin"));
        Assert.False(svc.Check("any", "any", "user")); // no match, default false
    }
}
