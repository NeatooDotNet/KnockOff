/// <summary>
/// Code samples for ~/.claude/skills/knockoff/moq-migration.md
///
/// Snippets in this file:
/// - skill:moq-migration:step1-create
/// - skill:moq-migration:step2-object
/// - skill:moq-migration:step3-setup
/// - skill:moq-migration:step4-async
/// - skill:moq-migration:step5-verify
/// - skill:moq-migration:step6-callback
/// - skill:moq-migration:static-returns
/// - skill:moq-migration:conditional-returns
/// - skill:moq-migration:throwing-exceptions
/// - skill:moq-migration:sequential-returns
/// - skill:moq-migration:property-setup
/// - skill:moq-migration:multiple-interfaces
/// - skill:moq-migration:argument-matching
/// - skill:moq-migration:method-overloads
/// - skill:moq-migration:out-params
/// - skill:moq-migration:ref-params
///
/// Corresponding tests: MoqMmrationSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Mmration Samples
// ============================================================================

public class MmUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MmConfig
{
    public int Timeout { get; set; }
}

// ============================================================================
// Step 1: Create KnockOff Class
// ============================================================================

public interface IMmUserService
{
    MmUser? GetUser(int id);
    Task<MmUser?> GetUserAsync(int id);
    void Save(MmUser user);
    void Delete(int id);
    IEnumerable<MmUser> GetAll();
    void Update(MmUser user);
}

#region skill:moq-migration:step1-create
[KnockOff]
public partial class MmUserServiceKnockOff : IMmUserService { }
#endregion

// skill:moq-migration:step2-object - kept inline in docs (before/after comparison)
// skill:moq-migration:step3-setup - kept inline in docs (before/after comparison)
// skill:moq-migration:step4-async - kept inline in docs (before/after comparison)
// skill:moq-migration:step5-verify - kept inline in docs (before/after comparison)
// skill:moq-migration:step6-callback - kept inline in docs (before/after comparison)

// ============================================================================
// Static Returns
// ============================================================================

public interface IMmConfigService
{
    MmConfig GetConfig();
}

#region skill:moq-migration:static-returns
[KnockOff]
public partial class MmConfigServiceKnockOff : IMmConfigService
{
    protected MmConfig GetConfig() => new MmConfig { Timeout = 30 };
}
#endregion

// skill:moq-migration:conditional-returns - kept inline in docs (before/after comparison)

// ============================================================================
// Throwing Exceptions
// ============================================================================

public interface IMmConnectionService
{
    void Connect();
}

#region skill:moq-migration:throwing-exceptions
[KnockOff]
public partial class MmConnectionKnockOff : IMmConnectionService { }
#endregion

// ============================================================================
// Sequential Returns
// ============================================================================

public interface IMmSequenceService
{
    int GetNext();
}

#region skill:moq-migration:sequential-returns
[KnockOff]
public partial class MmSequenceKnockOff : IMmSequenceService { }
#endregion

// ============================================================================
// Property Setup
// ============================================================================

public interface IMmPropService
{
    string Name { get; set; }
}

#region skill:moq-migration:property-setup
[KnockOff]
public partial class MmPropServiceKnockOff : IMmPropService { }
#endregion

// ============================================================================
// Multiple Interfaces
// ============================================================================

public interface IMmRepository
{
    void Save(object entity);
}

public interface IMmUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
#region skill:moq-migration:multiple-interfaces
[KnockOff]
public partial class MmRepositoryKnockOff : IMmRepository { }

[KnockOff]
public partial class MmUnitOfWorkKnockOff : IMmUnitOfWork { }
#endregion

// ============================================================================
// Argument Matching
// ============================================================================

public interface IMmLogger
{
    void Log(string message);
}

#region skill:moq-migration:argument-matching
[KnockOff]
public partial class MmLoggerKnockOff : IMmLogger { }
#endregion

// ============================================================================
// Method Overloads
// ============================================================================

public interface IMmProcessorService
{
    void Process(string data);
    void Process(string data, int priority);
    int Calculate(int value);
    int Calculate(int a, int b);
}

#region skill:moq-migration:method-overloads
[KnockOff]
public partial class MmProcessorKnockOff : IMmProcessorService { }
#endregion

// ============================================================================
// Out Parameters
// ============================================================================

public interface IMmParser
{
    bool TryParse(string input, out int result);
}

#region skill:moq-migration:out-params
[KnockOff]
public partial class MmParserKnockOff : IMmParser { }
#endregion

// ============================================================================
// Ref Parameters
// ============================================================================

public interface IMmRefProcessor
{
    void Increment(ref int value);
}

#region skill:moq-migration:ref-params
[KnockOff]
public partial class MmRefProcessorKnockOff : IMmRefProcessor { }
#endregion

// ============================================================================
// Gradual Mmration
// ============================================================================

public interface IMmOrderService
{
    void PlaceOrder(int orderId);
}

[KnockOff]
public partial class MmSharedRepositoryKnockOff : IMmRepository { }

// Use both in same project (gradual migration example)
// var userKnockOff = new MmUserServiceKnockOff();          // New tests: KnockOff
// var orderMock = new Mock<IMmOrderService>();              // Legacy tests: Keep Moq

// ============================================================================
// Class Stubs with .Object
// ============================================================================

public class MmEmailService
{
    public virtual void Send(string to, string body) { }
}

#region skill:moq-migration:class-stub-object
[KnockOff<MmEmailService>]
public partial class MmEmailServiceTests { }
#endregion

// Usage: skill:moq-migration:class-stub-object-usage - sourced from MoqMigrationSamplesTests.cs

// ============================================================================
// As{Interface}() Helper Methods
// ============================================================================

public interface IMmEntityBase
{
    int Id { get; }
}

public interface IMmEmployee : IMmEntityBase
{
    string Name { get; set; }
}

#region skill:moq-migration:as-interface-helpers
[KnockOff]
public partial class MmEmployeeKnockOff : IMmEmployee { }
#endregion

// Usage: skill:moq-migration:as-interface-helpers-usage - sourced from MoqMigrationSamplesTests.cs

// ============================================================================
// SetupProperty / Tracked Properties (Backing Fields)
// ============================================================================

public interface IMmTrackedPropService
{
    bool Active { get; set; }
    DateTime NewDate { get; set; }
    long VisitId { get; set; }
    string VisitLabel { get; set; }
    DateTime? PreviousVisitDate { get; set; }
}

#region skill:moq-migration:setup-property
[KnockOff]
public partial class MmTrackedPropServiceKnockOff : IMmTrackedPropService { }
#endregion

// Usage: skill:moq-migration:setup-property-usage - sourced from MoqMigrationSamplesTests.cs

// ============================================================================
// Interface Inheritance
// ============================================================================

public interface IMmInheritedEntityBase
{
    int Id { get; }
}

public interface IMmInheritedEmployee : IMmInheritedEntityBase
{
    string Name { get; set; }
    string Department { get; set; }
}

#region skill:moq-migration:interface-inheritance
[KnockOff]
public partial class MmInheritedEmployeeKnockOff : IMmInheritedEmployee { }
#endregion

// ============================================================================
// Usage Examples - containing snippet regions
// ============================================================================

/// <summary>
/// Static usage examples for moq-migration skill snippets.
/// Note: Backing field access (e.g., ActiveBacking) requires protected access,
/// so those patterns remain as pseudocode in the skill documentation.
/// </summary>
public static class MoqMigrationUsageExamples
{
    public static void ClassStubObjectUsage()
    {
        #region skill:moq-migration:class-stub-object-usage
        var stub = new MmEmailServiceTests.Stubs.MmEmailService();
        MmEmailService service = stub.Object;
        #endregion

        _ = service;
    }

    public static void InterfaceAccessUsage()
    {
        var knockOff = new MmEmployeeKnockOff();

        #region skill:moq-migration:interface-access-usage
        // Standalone stubs implement interfaces via implicit conversion
        IMmEmployee employee = knockOff;

        // For inherited interfaces, cast to the base type
        IMmEntityBase baseEntity = knockOff;
        #endregion

        _ = (employee, baseEntity);
    }

    public static void InterfaceInheritanceCallbacksUsage()
    {
        #region skill:moq-migration:interface-inheritance-callbacks
        var knockOff = new MmInheritedEmployeeKnockOff();

        // All members tracked on stub (flat API)
        knockOff.Id.OnGet = (ko) => 42;
        knockOff.Name.OnGet = (ko) => "John";
        knockOff.Department.OnGet = (ko) => "Engineering";
        #endregion
    }
}
