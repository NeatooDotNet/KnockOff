namespace KnockOff.Documentation.Samples.Tests.ReadMe;

using KnockOff.Documentation.Samples.ReadMe;

/// <summary>
/// Tests to verify README.md samples compile and work correctly.
/// </summary>
public class ReadMeSamplesTests
{
	[Fact]
	public void StandaloneStub_CompilesAndWorks()
	{
		var stub = new UserServiceStub();
		stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });

		IUserService service = stub;
		var user = service.GetUser(42);

		Assert.NotNull(user);
		Assert.Equal(42, user.Id);
		Assert.Equal("Test User", user.Name);
		Assert.Equal(42, stub.GetUser.LastArg);
		Assert.True(stub.GetUser.WasCalled);
	}

	[Fact]
	public void InlineStub_CompilesAndWorks()
	{
		var testInstance = new NotificationTests();
		var emailStub = new NotificationTests.Stubs.IEmailService();

		emailStub.SendEmail.OnCall((ko, to, subject, body) => { });

		IEmailService emailService = emailStub;
		emailService.SendEmail("user@example.com", "Welcome!", "Body");

		Assert.True(emailStub.SendEmail.WasCalled);
		Assert.Equal("user@example.com", emailStub.SendEmail.LastCallArgs?.to);
		Assert.Equal("Welcome!", emailStub.SendEmail.LastCallArgs?.subject);
	}

	[Fact]
	public void Verification_Works()
	{
		var stub = new UserServiceStub();
		IUserService service = stub;

		service.SaveUser(new User { Id = 1 });
		service.SaveUser(new User { Id = 2 });
		service.SaveUser(new User { Id = 3 });

		Assert.True(stub.SaveUser.WasCalled);
		Assert.Equal(3, stub.SaveUser.CallCount);
	}
}
