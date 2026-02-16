using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

// ============================================================================
// Edge Case: What happens when two overloads have the SAME TArgs type?
//
// Example: void Overload(string x) and int Overload(string x)
// Both have TArgs = string, so When("hello") would be ambiguous.
//
// This tests whether void slots and method slots with the same TArgs type
// can coexist (different interface families), and what happens when two
// slots in the SAME family have the same TArgs type.
// ============================================================================

// Scenario: void overload and non-void overload with same args (different families)
// void DoWork(string input) -> IVoidOverloadSlot1
// int DoWork(string input)  -> IMethodOverloadSlot1
// These are different interface families, so When("hello") should be ambiguous.
public delegate void DoWorkVoidDelegate(string input);
public delegate int DoWorkIntDelegate(string input);

public class SameArgsDifferentFamiliesCompositor
    : IVoidOverloadSlot1<DoWorkVoidDelegate, string>,
      IMethodOverloadSlot1<DoWorkIntDelegate, string, int>
{
    private readonly VoidMethodInterceptor<DoWorkVoidDelegate, string> _voidInterceptor = new();
    private readonly MethodInterceptor<DoWorkIntDelegate, string, int> _methodInterceptor = new();

    VoidMethodInterceptor<DoWorkVoidDelegate, string>
        IVoidOverloadSlot1<DoWorkVoidDelegate, string>.VoidSlot1Interceptor => _voidInterceptor;

    MethodInterceptor<DoWorkIntDelegate, string, int>
        IMethodOverloadSlot1<DoWorkIntDelegate, string, int>.MethodSlot1Interceptor => _methodInterceptor;
}

// Scenario: Two void overloads with same parameter types but different delegate names
// This would be unusual but tests the slot system's limits.
// void Process(string data)    -> mapped to ProcessA
// void Transform(string data)  -> mapped to TransformA
// Both have TArgs = string.
public delegate void ProcessADelegate(string data);
public delegate void TransformADelegate(string data);

public class SameArgsSameFamilyCompositor
    : IVoidOverloadSlot1<ProcessADelegate, string>,
      IVoidOverloadSlot2<TransformADelegate, string>
{
    private readonly VoidMethodInterceptor<ProcessADelegate, string> _interceptor1 = new();
    private readonly VoidMethodInterceptor<TransformADelegate, string> _interceptor2 = new();

    VoidMethodInterceptor<ProcessADelegate, string>
        IVoidOverloadSlot1<ProcessADelegate, string>.VoidSlot1Interceptor => _interceptor1;

    VoidMethodInterceptor<TransformADelegate, string>
        IVoidOverloadSlot2<TransformADelegate, string>.VoidSlot2Interceptor => _interceptor2;
}

public class EdgeCaseTests
{
    // ---- Different families, same TArgs ----

    [Fact]
    public void Call_VoidSlot_DifferentFamily_ResolvesCorrectly()
    {
        var compositor = new SameArgsDifferentFamiliesCompositor();
        // Call with void lambda -> resolves to void family
        var result = compositor.Call((string input) => { });
        Assert.Equal("VoidCall:DoWorkVoidDelegate", result);
    }

    [Fact]
    public void Return_MethodSlot_DifferentFamily_ResolvesCorrectly()
    {
        var compositor = new SameArgsDifferentFamiliesCompositor();
        // Return with int-returning lambda -> resolves to method family
        var result = compositor.Return((string input) => 42);
        Assert.Equal("MethodReturn:DoWorkIntDelegate", result);
    }

    // When("hello") is ambiguous here because both VoidSlot1.When(string)
    // and MethodSlot1.When(string) match. Let's see what the compiler says.
#if false
    // VERIFIED: CS0121 - The call is ambiguous between the following methods or properties:
    // 'MethodSlotExtensions.When<TDelegate, TArgs, TReturn>(IMethodOverloadSlot1<TDelegate, TArgs, TReturn>, TArgs)'
    // and
    // 'VoidSlotExtensions.When<TDelegate, TArgs>(IVoidOverloadSlot1<TDelegate, TArgs>, TArgs)'
    [Fact]
    public void When_SameArgs_DifferentFamily_IsAmbiguous()
    {
        var compositor = new SameArgsDifferentFamiliesCompositor();
        var result = compositor.When("hello"); // CS0121 - ambiguous
    }
#endif

    [Fact]
    public void When_SameArgs_DifferentFamily_CanResolveWithCast()
    {
        var compositor = new SameArgsDifferentFamiliesCompositor();

        // Can resolve by casting to the specific slot interface
        var asVoid = (IVoidOverloadSlot1<DoWorkVoidDelegate, string>)compositor;
        var result = asVoid.When("hello");
        Assert.Equal("VoidWhen:String:hello", result);
    }

    // ---- Same family, same TArgs, different TDelegate ----

    // Call() should NOT be ambiguous because each slot has a different TDelegate type.
    // The compiler picks the slot whose TDelegate matches the lambda signature.
    // BUT: both delegates have the same signature (string -> void), so this IS ambiguous.
#if false
    // VERIFIED: CS0121 - ambiguous because both ProcessADelegate and TransformADelegate
    // have identical signatures: (string) -> void
    // 'VoidSlotExtensions.Call<TDelegate, TArgs>(IVoidOverloadSlot1<TDelegate, TArgs>, TDelegate)'
    // and
    // 'VoidSlotExtensions.Call<TDelegate, TArgs>(IVoidOverloadSlot2<TDelegate, TArgs>, TDelegate)'
    [Fact]
    public void Call_SameArgsSameFamily_IsAmbiguous()
    {
        var compositor = new SameArgsSameFamilyCompositor();
        var result = compositor.Call((string data) => { }); // CS0121 - ambiguous
    }
#endif

    [Fact]
    public void Call_SameArgsSameFamily_CanResolveWithExplicitDelegate()
    {
        var compositor = new SameArgsSameFamilyCompositor();

        // Explicit delegate type disambiguates
        ProcessADelegate callback = (data) => { };
        var result = compositor.Call(callback);
        Assert.Equal("VoidCall:ProcessADelegate", result);
    }

    [Fact]
    public void Call_SameArgsSameFamily_CanResolveWithCast()
    {
        var compositor = new SameArgsSameFamilyCompositor();

        var asSlot2 = (IVoidOverloadSlot2<TransformADelegate, string>)compositor;
        var result = asSlot2.Call((string data) => { });
        Assert.Equal("VoidCall:TransformADelegate", result);
    }

    // When() with same TArgs in same family IS ambiguous (both slots match).
#if false
    // VERIFIED: CS0121 - ambiguous between VoidSlot1.When and VoidSlot2.When
    // 'VoidSlotExtensions.When<TDelegate, TArgs>(IVoidOverloadSlot1<TDelegate, TArgs>, TArgs)'
    // and
    // 'VoidSlotExtensions.When<TDelegate, TArgs>(IVoidOverloadSlot2<TDelegate, TArgs>, TArgs)'
    [Fact]
    public void When_SameArgsSameFamily_IsAmbiguous()
    {
        var compositor = new SameArgsSameFamilyCompositor();
        var result = compositor.When("hello"); // CS0121 - ambiguous
    }
#endif

    [Fact]
    public void When_SameArgsSameFamily_CanResolveWithCast()
    {
        var compositor = new SameArgsSameFamilyCompositor();

        var asSlot1 = (IVoidOverloadSlot1<ProcessADelegate, string>)compositor;
        var result = asSlot1.When("hello");
        Assert.Equal("VoidWhen:String:hello", result);
    }
}
