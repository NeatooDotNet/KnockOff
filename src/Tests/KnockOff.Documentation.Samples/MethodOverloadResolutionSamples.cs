using KnockOff;

namespace KnockOff.Documentation.Samples.MethodOverloadResolution;

// =============================================================================
// Interfaces for Method Overload Resolution Samples
// =============================================================================

#region readme-method-overload-interface
public interface IFormatter
{
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}
#endregion

// =============================================================================
// Stubs for Method Overload Resolution Samples
// =============================================================================

[KnockOff]
public partial class FormatterStub : IFormatter { }

// =============================================================================
// Method Overload Resolution Samples
// =============================================================================

public class MethodOverloadResolutionTests
{
    #region readme-method-overload-knockoff
    [Fact]
    public void OnCall_WithExplicitParameterTypes_ResolvesOverload()
    {
        var stub = new FormatterStub();

        // Explicit parameter types resolve the overload - standard C# syntax
        stub.Format.OnCall((string input, bool uppercase) => "bool overload");
        stub.Format.OnCall((string input, int maxLength) => "int overload");

        IFormatter formatter = stub;

        // Each overload is correctly configured
        Assert.Equal("bool overload", formatter.Format("test", true));
        Assert.Equal("int overload", formatter.Format("test", 10));
    }
    #endregion

    #region readme-method-overload-when
    [Fact]
    public void When_WithLiteralValues_ResolvesOverload()
    {
        var stub = new FormatterStub();

        // Specific value matching - parameter types resolve the overload
        stub.Format.When("test", true).Returns("UPPERCASE");
        stub.Format.When("test", 10).Returns("truncated");

        IFormatter formatter = stub;

        Assert.Equal("UPPERCASE", formatter.Format("test", true));
        Assert.Equal("truncated", formatter.Format("test", 10));
    }
    #endregion

    #region readme-method-overload-argument-access
    [Fact]
    public void OnCall_ArgumentsDirectlyAvailable_WithNamesAndTypes()
    {
        var stub = new FormatterStub();

        // Arguments are directly available with names and types:
        stub.Format.OnCall((string input, bool uppercase) => uppercase ? input.ToUpper() : input);

        IFormatter formatter = stub;

        Assert.Equal("HELLO", formatter.Format("hello", true));
        Assert.Equal("hello", formatter.Format("hello", false));
    }
    #endregion

    [Fact]
    public void CompleteExample_AllOverloadPatterns()
    {
        var stub = new FormatterStub();

        // Configure bool overload - use argument values directly
        stub.Format.OnCall((string input, bool uppercase) =>
            uppercase ? input.ToUpper() : input.ToLower());

        // Configure int overload - truncate to maxLength
        stub.Format.OnCall((string input, int maxLength) =>
            input.Length <= maxLength ? input : input[..maxLength] + "...");

        IFormatter formatter = stub;

        // Bool overload works correctly
        Assert.Equal("HELLO WORLD", formatter.Format("Hello World", true));
        Assert.Equal("hello world", formatter.Format("Hello World", false));

        // Int overload works correctly
        Assert.Equal("Hello", formatter.Format("Hello", 10));
        Assert.Equal("Hello W...", formatter.Format("Hello World", 7));
    }

    [Fact]
    public void Verification_WorksWithOverloads()
    {
        var stub = new FormatterStub();

        // Configure both overloads with verification
        stub.Format.OnCall((string input, bool uppercase) => input).Verifiable();
        stub.Format.OnCall((string input, int maxLength) => input).Verifiable();

        IFormatter formatter = stub;

        formatter.Format("test", true);
        formatter.Format("test", 10);

        // Verify all configured overloads were called
        stub.Verify();
    }

    [Fact]
    public void MixedConfiguration_WhenAndOnCall()
    {
        var stub = new FormatterStub();

        // When for specific values on bool overload
        stub.Format.When("special", true).Returns("SPECIAL CASE");

        // OnCall as fallback for bool overload
        stub.Format.OnCall((string input, bool uppercase) =>
            uppercase ? input.ToUpper() : input);

        // OnCall for int overload
        stub.Format.OnCall((string input, int maxLength) =>
            input[..Math.Min(input.Length, maxLength)]);

        IFormatter formatter = stub;

        // When match takes precedence
        Assert.Equal("SPECIAL CASE", formatter.Format("special", true));

        // OnCall fallback for non-matching bool calls
        Assert.Equal("OTHER", formatter.Format("other", true));

        // Int overload uses its OnCall
        Assert.Equal("hello", formatter.Format("hello world", 5));
    }
}
