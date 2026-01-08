/// <summary>
/// Code samples for docs/getting-started.md
///
/// Snippets in this file:
/// - docs:getting-started:interface-definition
/// - docs:getting-started:stub-class
/// - docs:getting-started:step3-test
/// - docs:getting-started:method-verification
/// - docs:getting-started:property-verification
/// - docs:getting-started:user-method
/// - docs:getting-started:callbacks
/// - docs:getting-started:reset
/// - docs:getting-started:via-callback
/// - docs:getting-started:returning-values
/// - docs:getting-started:simulating-failures
/// - docs:getting-started:single-method-suffix
/// - docs:getting-started:capturing-arguments
/// - docs:getting-started:single-interface
/// - docs:getting-started:inline-stubs-example
/// - docs:getting-started:nested-classes
/// - docs:getting-started:nested-partial-error
/// - docs:getting-started:nested-partial-correct
/// - docs:getting-started:method-overloads
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

public interface IRepository
{
    void Save(object entity);
}

public interface IUnitOfWork
{
    void Commit();
}

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
// These stubs are used by InlineStubsExampleTests.
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

[KnockOff]
public partial class UnitOfWorkKnockOff : IUnitOfWork { }

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

// ============================================================================
// Step 3: Use in Tests (Comment-based sample)
// ============================================================================

#region docs:getting-started:step3-test
// [Fact]
// public void NotificationService_SendsEmail_WhenUserRegisters()
// {
//     // Arrange
//     var emailKnockOff = new EmailServiceKnockOff();
//     IEmailService emailService = emailKnockOff;
//
//     var notificationService = new NotificationService(emailService);
//
//     // Act
//     notificationService.NotifyRegistration("user@example.com");
//
//     // Assert
//     Assert.True(emailKnockOff.SendEmail.WasCalled);
//     Assert.Equal("user@example.com", emailKnockOff.SendEmail.LastCallArgs?.to);
// }
#endregion

// ============================================================================
// Method Verification Patterns (Comment-based sample)
// ============================================================================

#region docs:getting-started:method-verification
// Check if called
// Assert.True(knockOff.SendEmail.WasCalled);
//
// Check call count
// Assert.Equal(3, knockOff.SendEmail.CallCount);
//
// Check last argument (single parameter)
// Assert.Equal(42, knockOff.GetById.LastCallArg);
//
// Check last arguments (multiple parameters - named tuple)
// var args = knockOff.SendEmail.LastCallArgs;
// Assert.Equal("user@example.com", args?.to);
// Assert.Equal("Welcome", args?.subject);
#endregion

// ============================================================================
// Property Verification Patterns (Comment-based sample)
// ============================================================================

#region docs:getting-started:property-verification
// Check getter calls
// Assert.Equal(2, knockOff.IsConnected.GetCount);
//
// Check setter calls
// Assert.Equal(1, knockOff.Name.SetCount);
// Assert.Equal("NewValue", knockOff.Name.LastSetValue);
#endregion

// ============================================================================
// Callbacks Example (Comment-based sample)
// ============================================================================

#region docs:getting-started:callbacks
// [Fact]
// public void RejectsEmail_WhenNotConnected()
// {
//     var knockOff = new EmailServiceKnockOff();
//
//     // Configure property to return false
//     knockOff.IsConnected.OnGet = (ko) => false;
//
//     // Configure method to throw
//     knockOff.SendEmail.OnCall = (ko, to, subject, body) =>
//     {
//         throw new InvalidOperationException("Not connected");
//     };
//
//     // ... test code
// }
#endregion

// ============================================================================
// Reset Example (Comment-based sample)
// ============================================================================

#region docs:getting-started:reset
// Reset specific handler
// knockOff.SendEmail.Reset();
//
// After reset:
// Assert.Equal(0, knockOff.SendEmail.CallCount);
// Callbacks are also cleared
#endregion

// ============================================================================
// Simulating Failures Example (Comment-based sample)
// ============================================================================

#region docs:getting-started:via-callback
// knockOff.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };
#endregion

#region docs:getting-started:simulating-failures
// knockOff.SaveAsync.OnCall = (ko, entity) =>
//     Task.FromException<int>(new DbException("Connection lost"));
#endregion

#region docs:getting-started:single-method-suffix
// knockOff.SendEmail.CallCount;  // Single method - no suffix
#endregion

// ============================================================================
// Capturing Arguments Example (Comment-based sample)
// ============================================================================

#region docs:getting-started:capturing-arguments
// List<string> sentEmails = new();
//
// knockOff.SendEmail.OnCall = (ko, to, subject, body) =>
// {
//     sentEmails.Add(to);
// };
//
// // ... run test ...
//
// Assert.Equal(3, sentEmails.Count);
// Assert.Contains("admin@example.com", sentEmails);
#endregion

// ============================================================================
// Method Overloads Example
// ============================================================================

#region docs:getting-started:method-overloads
public interface IProcessService
{
    void Process(string data);                           // Overload 1
    void Process(string data, int priority);             // Overload 2
    void Process(string data, int priority, bool async); // Overload 3
}

[KnockOff]
public partial class ProcessServiceKnockOff : IProcessService { }

// Each overload has its own interceptor (1-based numbering)
// knockOff.Process1.CallCount;  // Calls to Process(string)
// knockOff.Process2.CallCount;  // Calls to Process(string, int)
// knockOff.Process3.CallCount;  // Calls to Process(string, int, bool)
//
// Identify exactly which overload was called
// Assert.True(knockOff.Process2.WasCalled);
// Assert.False(knockOff.Process1.WasCalled);
//
// Simple callbacks - no delegate casting needed
// knockOff.Process1.OnCall = (ko, data) => { };
// knockOff.Process2.OnCall = (ko, data, priority) => { };
// knockOff.Process3.OnCall = (ko, data, priority, async) => { };
//
// Proper types - no nullable wrappers
// var args = knockOff.Process3.LastCallArgs;
// Assert.Equal("test", args.Value.data);
// Assert.Equal(5, args.Value.priority);  // int, not int?
// Assert.True(args.Value.async);
#endregion

// ============================================================================
// Single Interface Constraint Example
// ============================================================================

public interface IEntity
{
    int Id { get; }
}

#region docs:getting-started:single-interface
// Single interface - this is the standard pattern
[KnockOff]
public partial class SingleRepositoryKnockOff : IRepository { }

[KnockOff]
public partial class SingleUnitOfWorkKnockOff : IUnitOfWork { }

// Interface inheritance is fine - IEntity is a single interface
[KnockOff]
public partial class EntityKnockOff : IEntity { }

// Multiple unrelated interfaces - not supported
// This emits diagnostic KO0010
// [KnockOff]
// public partial class DataContextKnockOff : IRepository, IUnitOfWork { }
#endregion

// ============================================================================
// Inline Stubs Example (Cross-reference to inline-stubs.md)
// ============================================================================

#region docs:getting-started:inline-stubs-example
[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class InlineStubsExampleTests
{
    // [Fact]
    // public void Test()
    // {
    //     var repo = new Stubs.IRepository();
    //     var uow = new Stubs.IUnitOfWork();
    //     // ...
    // }
}
#endregion

// ============================================================================
// Nested Classes Example
// ============================================================================

public interface IUserRepository
{
    User? GetUser(int id);
    void SaveUser(User user);
}

#region docs:getting-started:nested-classes
public partial class UserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class UserRepositoryKnockOff : IUserRepository
    {
    }

    // [Fact]
    // public void GetUser_ReturnsUser()
    // {
    //     var knockOff = new UserRepositoryKnockOff();
    //     // ... test code
    // }
}
#endregion

// ============================================================================
// Nested Classes - Partial Requirement (Error example)
// ============================================================================

#region docs:getting-started:nested-partial-error
// Won't compile - containing class not partial
// public class MyBadTests
// {
//     [KnockOff]
//     public partial class ServiceKnockOff : IService { }
// }
#endregion

#region docs:getting-started:nested-partial-correct
// Correct - containing class is partial
// public partial class MyGoodTests
// {
//     [KnockOff]
//     public partial class ServiceKnockOff : IService { }
// }
#endregion
