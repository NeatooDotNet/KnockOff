using Moq;
using NSubstitute;
using NSubstitute.Exceptions;
using KnockOff;

namespace KnockOff.Documentation.Samples.TypeSafety;

// =============================================================================
// Interfaces for Type Safety Samples
// =============================================================================

public interface ITypeSafeCalc
{
    int Add(int a, int b);
}

public interface ITypeSafeValidator
{
    bool Validate(string firstName, string lastName);
}

// =============================================================================
// KnockOff Stubs
// =============================================================================

[KnockOff]
public partial class TypeSafeCalcStub : ITypeSafeCalc { }

[KnockOff]
public partial class TypeSafeValidatorStub : ITypeSafeValidator { }

// =============================================================================
// Moq Type Safety Gaps
// =============================================================================

public class MoqTypeSafetyTests
{
    [Fact]
    public void Moq_Returns_WrongTypeParameters_CompilesButFailsAtRuntime()
    {
        #region type-safety-moq-returns-mismatch
        // Moq — .Returns<T1, T2> type parameters are NOT checked at compile time
        var mock = new Mock<ITypeSafeCalc>();

        // Add(int, int) but we specify <string, string> — COMPILES FINE
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
            .Returns<string, string>((a, b) => 0);

        // ArgumentException at RUNTIME: "Object of type 'Int32' cannot be converted to type 'String'"
        ITypeSafeCalc calc = mock.Object;
        Assert.Throws<ArgumentException>(() => calc.Add(1, 2));
        #endregion
    }

    [Fact]
    public void Moq_Callback_WrongTypeParameters_CompilesButFailsAtRuntime()
    {
        #region type-safety-moq-callback-mismatch
        // Moq — .Callback<T1, T2> type parameters are NOT checked at compile time
        var mock = new Mock<ITypeSafeValidator>();
        var setup = mock.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<string>()));

        // Validate(string, string) but we specify <int, string> — COMPILES FINE
        // ArgumentException at RUNTIME: "Invalid callback. Setup on method with parameters
        // (string, string) cannot invoke callback with parameters (int, string)."
        Assert.Throws<ArgumentException>(() => setup.Callback<int, string>((a, b) => { }));
        #endregion
    }
}

// =============================================================================
// NSubstitute Type Safety Gaps
// =============================================================================

public class NSubstituteTypeSafetyTests
{
    [Fact]
    public void NSub_CallInfo_IndexCast_CompilesButFailsAtRuntime()
    {
        #region type-safety-nsub-callinfo-cast
        // NSubstitute — callInfo[0] returns object, cast is unchecked at compile time
        var calc = Substitute.For<ITypeSafeCalc>();

        // Add(int, int) but we cast callInfo[0] to string — COMPILES FINE
        calc.Add(Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo =>
            {
                var a = (string)callInfo[0]; // InvalidCastException at RUNTIME
                return 0;
            });

        Assert.Throws<InvalidCastException>(() => calc.Add(1, 2));
        #endregion
    }

    [Fact]
    public void NSub_Arg_Ambiguous_WithSameTypedParams_CompilesButFailsAtRuntime()
    {
        #region type-safety-nsub-arg-ambiguous
        // NSubstitute — Arg<T>() is ambiguous when multiple parameters share the same type
        var validator = Substitute.For<ITypeSafeValidator>();

        // Validate(string, string) — both params are string
        validator.Validate(Arg.Any<string>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                // callInfo.Arg<string>() throws because there are TWO string params
                var name = callInfo.Arg<string>(); // AmbiguousArgumentsException at RUNTIME
                return true;
            });

        Assert.Throws<AmbiguousArgumentsException>(() => validator.Validate("Jane", "Doe"));
        #endregion
    }
}

// =============================================================================
// KnockOff — Fully Typed (no gaps)
// =============================================================================

public class KnockOffTypeSafetyTests
{
    [Fact]
    public void KnockOff_Return_ParametersAreTyped()
    {
        #region type-safety-knockoff-oncall-typed
        // KnockOff — Return parameters are generated from the method signature
        var stub = new TypeSafeCalcStub();

        // (int a, int b) — types and names come from Add(int a, int b)
        // Wrong types here cause a COMPILE error, not a runtime error
        stub.Add.Return((a, b) => a + b);

        ITypeSafeCalc calc = stub;
        Assert.Equal(3, calc.Add(1, 2));
        #endregion
    }

    [Fact]
    public void KnockOff_When_ParametersAreTyped()
    {
        #region type-safety-knockoff-when-typed
        // KnockOff — When predicate parameters are generated from the method signature
        var stub = new TypeSafeValidatorStub();

        // (string firstName, string lastName) — both parameters are named and typed
        // No ambiguity: each parameter is a separate lambda argument
        stub.Validate.When((firstName, lastName) => firstName.Length > 0).Return(true);

        ITypeSafeValidator validator = stub;
        Assert.True(validator.Validate("Jane", "Doe"));
        #endregion
    }
}
