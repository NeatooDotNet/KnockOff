/// <summary>
/// Code samples for docs/why-knockoff/compile-time-safety.md
/// </summary>

using Moq;

namespace KnockOff.Documentation.Samples.WhyKnockOff;

// ============================================================================
// Domain Types
// ============================================================================

public class CtsUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface ICtsUserService
{
    CtsUser GetUser(int id);
}

public interface ICtsEmailService
{
    void SendEmail(string to, string subject, string body);
}

// ============================================================================
// KnockOff Stubs
// ============================================================================

[KnockOff]
public partial class CtsUserServiceKnockOff : ICtsUserService { }

[KnockOff]
public partial class CtsEmailServiceKnockOff : ICtsEmailService { }

// ============================================================================
// Sample code
// ============================================================================

public static class CompileTimeSafetySamples
{
    #region compile-time-moq-runtime-problem
    public static void MoqRuntimeProblem()
    {
        var mock = new Mock<ICtsUserService>();
        mock.Setup(x => x.GetUser(1)).Returns(new CtsUser());

        // If interface adds a new method, this compiles but may fail at runtime
        // if Moq strict mode is enabled
    }
    #endregion

    #region compile-time-practical-before
    public static void PracticalExampleBefore()
    {
        var stub = new CtsEmailServiceKnockOff();
        ICtsEmailService service = stub;

        stub.SendEmail.OnCall = (ko, to, subject, body) => { };

        service.SendEmail("user@example.com", "Subject", "Body");

        Assert.Equal("user@example.com", stub.SendEmail.LastCallArgs?.to);
    }
    #endregion
}

// Minimal Assert for compilation
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
}
