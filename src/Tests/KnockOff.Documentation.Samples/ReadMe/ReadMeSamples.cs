/// <summary>
/// Code samples for README.md
///
/// Snippets in this file:
/// - readme:installation
/// - readme:quick-example-standalone
/// - readme:quick-example-inline
/// - readme:quick-example-verification
/// - readme:moq-comparison-table
///
/// Corresponding tests: ReadMeSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.ReadMe;

// ============================================================================
// Domain Types
// ============================================================================

public class User
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
}

// ============================================================================
// Installation
// ============================================================================

#region readme-installation
// dotnet add package KnockOff
#endregion

// ============================================================================
// Quick Example - Standalone Pattern
// ============================================================================

public interface IUserService
{
	User? GetUser(int id);
	void SaveUser(User user);
	bool IsConnected { get; }
}

#region readme-quick-example-standalone
[KnockOff]
public partial class UserServiceStub : IUserService { }

// Usage in test:
// var stub = new UserServiceStub();
// stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });
//
// IUserService service = stub;  // Implicit conversion
// var user = service.GetUser(42);
//
// Assert.Equal(42, stub.GetUser.LastArg);
// Assert.True(stub.GetUser.WasCalled);
#endregion

// ============================================================================
// Quick Example - Inline Pattern
// ============================================================================

public interface IEmailService
{
	void SendEmail(string to, string subject, string body);
	int EmailsSent { get; }
}

#region readme-quick-example-inline
[KnockOff<IEmailService>]
public partial class NotificationTests
{
	// [Fact]
	// public void SendsWelcomeEmail_WhenUserRegisters()
	// {
	//     var emailStub = new Stubs.IEmailService();
	//     emailStub.SendEmail.OnCall((ko, to, subject, body) => { });
	//
	//     IEmailService emailService = emailStub;
	//     var notificationService = new NotificationService(emailService);
	//
	//     notificationService.NotifyRegistration("user@example.com");
	//
	//     Assert.True(emailStub.SendEmail.WasCalled);
	//     Assert.Equal("user@example.com", emailStub.SendEmail.LastCallArgs?.to);
	// }
}
#endregion

// ============================================================================
// Quick Example - Verification
// ============================================================================

#region readme-quick-example-verification
// Check if method was called
// Assert.True(stub.SaveUser.WasCalled);
//
// Check call count
// Assert.Equal(3, stub.SaveUser.CallCount);
//
// Capture arguments (single parameter)
// Assert.Equal(42, stub.GetUser.LastArg);
//
// Capture arguments (multiple parameters - named tuple)
// var args = stub.SendEmail.LastCallArgs;
// Assert.Equal("user@example.com", args?.to);
// Assert.Equal("Welcome!", args?.subject);
#endregion

// ============================================================================
// Moq Comparison (Comment-based for table)
// ============================================================================

#region readme-moq-comparison-table
// | Feature | Moq | KnockOff |
// |---------|-----|----------|
// | **When errors occur** | Runtime | Compile-time |
// | **How it works** | Reflection | Source generation |
// | **Configuration** | Fluent API | Callbacks + properties |
// | **Type safety** | Magic strings | Fully typed |
// | **IntelliSense** | Limited | Full support |
#endregion
