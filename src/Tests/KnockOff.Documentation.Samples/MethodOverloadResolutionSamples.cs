using KnockOff;
using NSubstitute;

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
// NSubstitute Comparison Samples (focused snippets)
// =============================================================================

public class NSubstituteOverloadResolutionTests
{
    [Fact]
    public void NSubstitute_AnyValueMatching()
    {
        var formatter = Substitute.For<IFormatter>();

        #region readme-nsubstitute-any-value
        // Arg.Any<T>() required - compiler needs the types to resolve overload
        formatter.Format(Arg.Any<string>(), Arg.Any<bool>()).Returns("bool overload");
        formatter.Format(Arg.Any<string>(), Arg.Any<int>()).Returns("int overload");
        #endregion

        Assert.Equal("bool overload", formatter.Format("test", true));
        Assert.Equal("int overload", formatter.Format("test", 10));
    }

    [Fact]
    public void NSubstitute_SpecificValueMatching()
    {
        var formatter = Substitute.For<IFormatter>();

        #region readme-nsubstitute-specific-value
        // Specific value matching - literals work when all args are specific
        formatter.Format("test", true).Returns("UPPERCASE");
        formatter.Format("test", 10).Returns("truncated");
        #endregion

        Assert.Equal("UPPERCASE", formatter.Format("test", true));
        Assert.Equal("truncated", formatter.Format("test", 10));
    }

    [Fact]
    public void NSubstitute_ArgumentAccess()
    {
        var formatter = Substitute.For<IFormatter>();

        #region readme-nsubstitute-argument-access
        // To use argument values, extract from CallInfo:
        formatter.Format(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(x => x.ArgAt<bool>(1) ? x.ArgAt<string>(0).ToUpper() : x.ArgAt<string>(0));
        #endregion

        Assert.Equal("HELLO", formatter.Format("hello", true));
        Assert.Equal("hello", formatter.Format("hello", false));
    }
}

// =============================================================================
// KnockOff Samples (focused snippets)
// =============================================================================

public class KnockOffOverloadResolutionTests
{
    [Fact]
    public void KnockOff_AnyValueMatching()
    {
        var stub = new FormatterStub();

        #region readme-knockoff-any-value
        // Explicit parameter types resolve the overload - standard C# syntax
        stub.Format.Return((string input, bool uppercase) => "bool overload");
        stub.Format.Return((string input, int maxLength) => "int overload");
        #endregion

        IFormatter formatter = stub;

        Assert.Equal("bool overload", formatter.Format("test", true));
        Assert.Equal("int overload", formatter.Format("test", 10));
    }

    [Fact]
    public void KnockOff_SpecificValueMatching()
    {
        var stub = new FormatterStub();

        #region readme-knockoff-specific-value
        // Specific value matching - parameter types resolve the overload
        stub.Format.When("test", true).Return("UPPERCASE");
        stub.Format.When("test", 10).Return("truncated");
        #endregion

        IFormatter formatter = stub;

        Assert.Equal("UPPERCASE", formatter.Format("test", true));
        Assert.Equal("truncated", formatter.Format("test", 10));
    }

    [Fact]
    public void KnockOff_ArgumentAccess()
    {
        var stub = new FormatterStub();

        #region readme-knockoff-argument-access
        // Arguments are directly available with names and types:
        stub.Format.Return((string input, bool uppercase) => uppercase ? input.ToUpper() : input);
        #endregion

        IFormatter formatter = stub;

        Assert.Equal("HELLO", formatter.Format("hello", true));
        Assert.Equal("hello", formatter.Format("hello", false));
    }
}

// =============================================================================
// Additional KnockOff Tests
// =============================================================================

public class MethodOverloadResolutionTests
{
    [Fact]
    public void CompleteExample_AllOverloadPatterns()
    {
        var stub = new FormatterStub();

        // Configure bool overload - use argument values directly
        stub.Format.Return((string input, bool uppercase) =>
            uppercase ? input.ToUpper() : input.ToLower());

        // Configure int overload - truncate to maxLength
        stub.Format.Return((string input, int maxLength) =>
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
        stub.Format.Return((string input, bool uppercase) => input).Verifiable();
        stub.Format.Return((string input, int maxLength) => input).Verifiable();

        IFormatter formatter = stub;

        formatter.Format("test", true);
        formatter.Format("test", 10);

        // Verify all configured overloads were called
        stub.Verify();
    }

    [Fact]
    public void MixedConfiguration_WhenAndReturn()
    {
        var stub = new FormatterStub();

        // When for specific values on bool overload
        stub.Format.When("special", true).Return("SPECIAL CASE");

        // Returns as fallback for bool overload
        stub.Format.Return((string input, bool uppercase) =>
            uppercase ? input.ToUpper() : input);

        // Returns for int overload
        stub.Format.Return((string input, int maxLength) =>
            input[..Math.Min(input.Length, maxLength)]);

        IFormatter formatter = stub;

        // When match takes precedence
        Assert.Equal("SPECIAL CASE", formatter.Format("special", true));

        // Return fallback for non-matching bool calls
        Assert.Equal("OTHER", formatter.Format("other", true));

        // Int overload uses its Return
        Assert.Equal("hello", formatter.Format("hello world", 5));
    }
}
