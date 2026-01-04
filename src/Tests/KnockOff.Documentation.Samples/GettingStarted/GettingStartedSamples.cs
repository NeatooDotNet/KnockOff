/// <summary>
/// Code samples for docs/getting-started.md
///
/// Snippets in this file:
/// - docs:getting-started:interface-definition
/// - docs:getting-started:stub-class
/// - docs:getting-started:method-verification
/// - docs:getting-started:property-verification
/// - docs:getting-started:user-method
/// - docs:getting-started:callbacks
/// - docs:getting-started:reset
/// - docs:getting-started:returning-values
/// - docs:getting-started:simulating-failures
/// - docs:getting-started:capturing-arguments
/// - docs:getting-started:multiple-interfaces
///
/// Corresponding tests: GettingStartedSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.GettingStarted;

// ============================================================================
// Interface Definition
// ============================================================================

#region docs:getting-started:interface-definition
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
}
#endregion

// ============================================================================
// Basic Stub Class
// ============================================================================

#region docs:getting-started:stub-class
[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // That's it! The generator creates the implementation.
}
#endregion

// ============================================================================
// Stub with User-Defined Method
// ============================================================================

#region docs:getting-started:user-method
[KnockOff]
public partial class EmailServiceWithValidation : IEmailServiceWithValidation
{
    // This method is called when IEmailService.IsValidAddress is invoked
    protected bool IsValidAddress(string email) =>
        email.Contains("@") && email.Contains(".");
}
#endregion

// Supporting interface for user-method sample
public interface IEmailServiceWithValidation
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
    bool IsValidAddress(string email);
}

// ============================================================================
// Stub for Return Values Example
// ============================================================================

#region docs:getting-started:returning-values
[KnockOff]
public partial class UserServiceKnockOff : IUserServiceSimple
{
    // Via user method (in stub class)
    protected User GetUser(int id) => new User { Id = id, Name = "Default" };
}
#endregion

public interface IUserServiceSimple
{
    User GetUser(int id);
    string Name { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Multiple Interfaces Example
// ============================================================================

#region docs:getting-started:multiple-interfaces
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork
{
}
#endregion

public interface IRepository
{
    void Save(object entity);
}

public interface IUnitOfWork
{
    void Commit();
}

// ============================================================================
// Stub for Async Failure Example
// ============================================================================

[KnockOff]
public partial class AsyncServiceKnockOff : IAsyncSaveService
{
}

public interface IAsyncSaveService
{
    Task<int> SaveAsync(object entity);
}
