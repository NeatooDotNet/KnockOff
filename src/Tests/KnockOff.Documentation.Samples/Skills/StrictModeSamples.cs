/// <summary>
/// Code samples for ~/.claude/skills/knockoff/strict-mode.md
///
/// Snippets in this file:
/// - skill:strict-mode:extension
/// - skill:strict-mode:standalone
/// - skill:strict-mode:inline
/// - skill:strict-mode:attribute-standalone
/// - skill:strict-mode:attribute-inline
/// - skill:strict-mode:moq-migration
/// - skill:SKILL:strict-mode-quick
/// - skill:moq-migration:strict-knockoff
/// - skill:moq-migration:strict-throws
/// </summary>

using KnockOff.Documentation.Samples.SampleDomain;

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Strict Mode Samples
// ============================================================================

public interface ISmUserService
{
    User GetUser(int id);
    void DeleteUser(int id);
}

public interface ISmStrictByDefault
{
    int GetData();
}

// ============================================================================
// Standalone Stubs
// ============================================================================

[KnockOff]
public partial class SmUserServiceKnockOff : ISmUserService
{
}

[KnockOff(Strict = true)]
public partial class SmStrictUserServiceKnockOff : ISmUserService
{
}

// ============================================================================
// Inline Stubs
// ============================================================================

[KnockOff<ISmUserService>]
[KnockOff<ISmStrictByDefault>(Strict = true)]
public partial class SmInlineTests
{
}

// ============================================================================
// Sample Code Regions
// ============================================================================

public static class StrictModeSamples
{
    #region skill-strict-mode-extension
    public static void ExtensionMethodExample()
    {
        // Standalone stub
        var standalone = new SmUserServiceKnockOff().Strict();

        // Inline stub
        var inline = new SmInlineTests.Stubs.ISmUserService().Strict();

        // Works with any stub type - returns same instance for chaining
    }
    #endregion

    #region skill-strict-mode-standalone
    public static void StandaloneExample()
    {
        var stub = new SmUserServiceKnockOff().Strict();

        ISmUserService service = stub;
        // service.GetUser(1);  // Would throw StubException - no OnCall configured!
    }
    #endregion

    #region skill-strict-mode-inline
    public static void InlineExample()
    {
        // Constructor parameter
        var stub1 = new SmInlineTests.Stubs.ISmUserService(strict: true);

        // Or extension method
        var stub2 = new SmInlineTests.Stubs.ISmUserService().Strict();

        stub2.GetUser.OnCall = (ko, id) => new User { Id = id };  // Configure what you expect

        ISmUserService service = stub2;
        _ = service.GetUser(1);     // OK - OnCall configured
        // service.DeleteUser(1);  // Would throw StubException - no OnCall configured!
    }
    #endregion

    #region skill-strict-mode-attribute-standalone
    public static void AttributeStandaloneExample()
    {
        // SmStrictUserServiceKnockOff has [KnockOff(Strict = true)]
        // All instances default to strict
        var stub = new SmStrictUserServiceKnockOff();

        // Override per-instance if needed
        stub.Strict = false;  // This instance is lenient
    }
    #endregion

    #region skill-strict-mode-attribute-inline
    public static void AttributeInlineExample()
    {
        // ISmStrictByDefault has [KnockOff<T>(Strict = true)]
        // Default is strict, but can override:
        var lenient = new SmInlineTests.Stubs.ISmStrictByDefault(strict: false);
    }
    #endregion

    #region skill-strict-mode-moq-migration
    public static void MoqMigrationExample()
    {
        // Moq equivalent:
        // var mock = new Mock<ISmUserService>(MockBehavior.Strict);
        // mock.Setup(x => x.GetUser(1)).Returns(new User());

        // KnockOff - standalone
        var stub1 = new SmUserServiceKnockOff().Strict();
        stub1.GetUser.OnCall((ko, id) => new User());

        // KnockOff - inline
        var stub2 = new SmInlineTests.Stubs.ISmUserService().Strict();
        stub2.GetUser.OnCall = (ko, id) => new User();
    }
    #endregion

    #region skill-SKILL-strict-mode-quick
    public static void QuickExample()
    {
        // Fluent API (recommended)
        var stub1 = new SmUserServiceKnockOff().Strict();
        var stub2 = new SmInlineTests.Stubs.ISmUserService().Strict();

        // Constructor parameter (inline only)
        var stub3 = new SmInlineTests.Stubs.ISmUserService(strict: true);

        // Attribute default - see SmStrictUserServiceKnockOff above
    }
    #endregion

    #region skill-moq-migration-strict-knockoff
    public static void MoqMigrationStrictKnockOff()
    {
        // Extension method (recommended) - works with any stub type
        var stub1 = new SmUserServiceKnockOff().Strict();
        var stub2 = new SmInlineTests.Stubs.ISmUserService().Strict();

        // Property setter
        var stub3 = new SmUserServiceKnockOff();
        stub3.Strict = true;

        // Constructor parameter (inline stubs only)
        var stub4 = new SmInlineTests.Stubs.ISmUserService(strict: true);

        // Attribute default - see SmStrictUserServiceKnockOff
    }
    #endregion

    #region skill-moq-migration-strict-throws
    public static void MoqMigrationStrictThrows()
    {
        var stub = new SmUserServiceKnockOff().Strict();
        // stub.GetUser.OnCall not set
        // stub.Object.GetUser(1);  // Would throw StubException!
    }
    #endregion
}
