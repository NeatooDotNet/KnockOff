/// <summary>
/// Code samples for docs/guides/multiple-interfaces.md
///
/// Snippets in this file:
/// - docs:multiple-interfaces:basic-usage
/// - docs:multiple-interfaces:shared-method
/// - docs:multiple-interfaces:repo-uow
/// - docs:multiple-interfaces:multiple-repos
///
/// Corresponding tests: MultipleInterfacesSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class MiUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MiOrder
{
    public int Id { get; set; }
}

// ============================================================================
// Basic Usage
// ============================================================================

#region docs:multiple-interfaces:basic-usage
public interface IMiLogger
{
    void Log(string message);
    string Name { get; set; }
}

public interface IMiNotifier
{
    void Notify(string recipient);
    string Name { get; }  // Same name, different accessor
}

[KnockOff]
public partial class MiLoggerNotifierKnockOff : IMiLogger, IMiNotifier { }
#endregion

// ============================================================================
// Same Method Signature
// ============================================================================

#region docs:multiple-interfaces:shared-method
public interface IMiLoggerSame
{
    void Log(string message);
}

public interface IMiAuditor
{
    void Log(string message);  // Same signature
    void Audit(string action, int userId);
}

[KnockOff]
public partial class MiLoggerAuditorKnockOff : IMiLoggerSame, IMiAuditor { }
#endregion

// ============================================================================
// Repository + Unit of Work
// ============================================================================

#region docs:multiple-interfaces:repo-uow
public interface IMiRepository
{
    MiUser? GetById(int id);
    void Add(MiUser user);
}

public interface IMiUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

[KnockOff]
public partial class MiDataContextKnockOff : IMiRepository, IMiUnitOfWork { }
#endregion

// ============================================================================
// Multiple Repositories
// ============================================================================

public interface IMiUserRepository
{
    MiUser? GetUser(int id);
}

public interface IMiOrderRepository
{
    MiOrder? GetOrder(int id);
}

#region docs:multiple-interfaces:multiple-repos
[KnockOff]
public partial class MiCompositeRepositoryKnockOff : IMiUserRepository, IMiOrderRepository { }
#endregion

