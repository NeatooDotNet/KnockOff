using Moq;
using NSubstitute;
using KnockOff;

namespace KnockOff.Documentation.Samples.PartialSetup;

public interface IPartialSetupCalc
{
    int Calculate(int a, int b);
}

[KnockOff]
public partial class PartialSetupCalcStub : IPartialSetupCalc { }

// =============================================================================
// The Partial Setup Trap — Moq
// =============================================================================

public class MoqPartialSetupTests
{
    [Fact]
    public void Moq_Strict_SetupWithoutReturns_ThrowsAtRuntime()
    {
        #region partial-setup-moq-strict
        // Moq Strict — Setup without .Returns() throws at runtime
        var mock = new Mock<IPartialSetupCalc>(MockBehavior.Strict);

        // You set the method up...
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
        // ...but forgot .Returns()

        // MockException at RUNTIME — Moq acts as if the method was never set up
        IPartialSetupCalc calc = mock.Object;
        Assert.Throws<MockException>(() => calc.Calculate(1, 2));
        #endregion
    }

    [Fact]
    public void Moq_Loose_SetupWithoutReturns_ReturnsSilentDefault()
    {
        #region partial-setup-moq-loose
        // Moq Loose — Setup without .Returns() silently returns default
        var mock = new Mock<IPartialSetupCalc>();

        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
        // No .Returns() — no error, no warning

        IPartialSetupCalc calc = mock.Object;
        var result = calc.Calculate(1, 2);
        Assert.Equal(0, result); // silently returns 0 instead of a meaningful value
        #endregion
    }
}

// =============================================================================
// NSubstitute — same silent default, no strict mode to catch it
// =============================================================================

public class NSubPartialSetupTests
{
    [Fact]
    public void NSub_NoReturns_SilentlyReturnsDefault()
    {
        #region partial-setup-nsub-silent
        // NSubstitute — no strict mode, silently returns default
        var calc = Substitute.For<IPartialSetupCalc>();

        // No .Returns() configured — returns default(int) = 0
        // No error, no warning — your test may pass for the wrong reason
        var result = calc.Calculate(1, 2);
        Assert.Equal(0, result);
        #endregion
    }
}

// =============================================================================
// KnockOff — no partial setup possible
// =============================================================================

public class KnockOffPartialSetupTests
{
    [Fact]
    public void KnockOff_OnCall_IsSetupAndReturn()
    {
        #region partial-setup-knockoff-oncall
        // KnockOff — OnCall IS the setup AND the return value
        var stub = new PartialSetupCalcStub();

        // One call does both: configures the method AND defines the return value
        // There is no second step to forget
        stub.Calculate.Returns((a, b) => a + b);

        IPartialSetupCalc calc = stub;
        Assert.Equal(3, calc.Calculate(1, 2));
        #endregion
    }

    [Fact]
    public void KnockOff_Returns_IsAlsoComplete()
    {
        #region partial-setup-knockoff-returns
        // KnockOff — Returns is also a single complete call
        var stub = new PartialSetupCalcStub();

        stub.Calculate.Returns(42);

        IPartialSetupCalc calc = stub;
        Assert.Equal(42, calc.Calculate(1, 2));
        #endregion
    }
}
