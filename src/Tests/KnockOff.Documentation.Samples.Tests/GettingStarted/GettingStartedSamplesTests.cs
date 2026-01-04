using KnockOff.Documentation.Samples.GettingStarted;

namespace KnockOff.Documentation.Samples.Tests.GettingStarted;

/// <summary>
/// Tests for docs/getting-started.md samples.
/// Verifies all code snippets compile and work as documented.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "GettingStarted")]
public class GettingStartedSamplesTests : SamplesTestBase
{
    // ========================================================================
    // docs:getting-started:stub-class
    // ========================================================================

    [Fact]
    public void EmailServiceKnockOff_CanBeCreated()
    {
        var knockOff = new EmailServiceKnockOff();
        Assert.NotNull(knockOff);
        Assert.NotNull(knockOff.IEmailService);
    }

    // ========================================================================
    // docs:getting-started:method-verification
    // ========================================================================

    [Fact]
    public void MethodVerification_WasCalled_TracksInvocation()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        emailService.SendEmail("user@example.com", "Subject", "Body");

        // Check if called
        Assert.True(knockOff.IEmailService.SendEmail.WasCalled);
    }

    [Fact]
    public void MethodVerification_CallCount_TracksMultipleCalls()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        emailService.SendEmail("a@example.com", "S1", "B1");
        emailService.SendEmail("b@example.com", "S2", "B2");
        emailService.SendEmail("c@example.com", "S3", "B3");

        // Check call count
        Assert.Equal(3, knockOff.IEmailService.SendEmail.CallCount);
    }

    [Fact]
    public void MethodVerification_LastCallArgs_TracksNamedTuple()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        emailService.SendEmail("user@example.com", "Welcome", "Hello!");

        // Check last arguments (multiple parameters - named tuple)
        var args = knockOff.IEmailService.SendEmail.LastCallArgs;
        Assert.Equal("user@example.com", args?.to);
        Assert.Equal("Welcome", args?.subject);
    }

    [Fact]
    public void MethodVerification_AllCalls_TracksHistory()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        emailService.SendEmail("first@example.com", "S1", "B1");
        emailService.SendEmail("second@example.com", "S2", "B2");
        emailService.SendEmail("third@example.com", "S3", "B3");

        // Check all calls
        Assert.Equal(3, knockOff.IEmailService.SendEmail.AllCalls.Count);
        Assert.Equal("first@example.com", knockOff.IEmailService.SendEmail.AllCalls[0].to);
    }

    // ========================================================================
    // docs:getting-started:property-verification
    // ========================================================================

    [Fact]
    public void PropertyVerification_GetCount_TracksGetterCalls()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        _ = emailService.IsConnected;
        _ = emailService.IsConnected;

        // Check getter calls
        Assert.Equal(2, knockOff.IEmailService.IsConnected.GetCount);
    }

    [Fact]
    public void PropertyVerification_SetCount_TracksSetterCalls()
    {
        var knockOff = new UserServiceKnockOff();
        IUserServiceSimple userService = knockOff;

        userService.Name = "NewValue";

        // Check setter calls
        Assert.Equal(1, knockOff.IUserServiceSimple.Name.SetCount);
        Assert.Equal("NewValue", knockOff.IUserServiceSimple.Name.LastSetValue);
    }

    // ========================================================================
    // docs:getting-started:user-method
    // ========================================================================

    [Fact]
    public void UserMethod_IsValidAddress_ProvidesDefaultBehavior()
    {
        var knockOff = new EmailServiceWithValidation();
        IEmailServiceWithValidation emailService = knockOff;

        Assert.True(emailService.IsValidAddress("test@example.com"));
        Assert.False(emailService.IsValidAddress("invalid"));
    }

    // ========================================================================
    // docs:getting-started:callbacks
    // ========================================================================

    [Fact]
    public void Callbacks_OnGet_ConfiguresPropertyBehavior()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        // Configure property to return false
        knockOff.IEmailService.IsConnected.OnGet = (ko) => false;

        Assert.False(emailService.IsConnected);
    }

    [Fact]
    public void Callbacks_OnCall_ConfiguresMethodBehavior()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        // Configure method to throw
        knockOff.IEmailService.SendEmail.OnCall = (ko, to, subject, body) =>
        {
            throw new InvalidOperationException("Not connected");
        };

        Assert.Throws<InvalidOperationException>(() =>
            emailService.SendEmail("user@example.com", "Subject", "Body"));
    }

    // ========================================================================
    // docs:getting-started:reset
    // ========================================================================

    [Fact]
    public void Reset_ClearsTrackingState()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        emailService.SendEmail("user@example.com", "Subject", "Body");
        Assert.Equal(1, knockOff.IEmailService.SendEmail.CallCount);

        // Reset specific handler
        knockOff.IEmailService.SendEmail.Reset();

        // After reset:
        Assert.Equal(0, knockOff.IEmailService.SendEmail.CallCount);
    }

    // ========================================================================
    // docs:getting-started:returning-values
    // ========================================================================

    [Fact]
    public void ReturningValues_UserMethod_ProvidesDefaultBehavior()
    {
        var knockOff = new UserServiceKnockOff();
        IUserServiceSimple userService = knockOff;

        var user = userService.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Default", user.Name);
    }

    [Fact]
    public void ReturningValues_Callback_OverridesUserMethod()
    {
        var knockOff = new UserServiceKnockOff();
        IUserServiceSimple userService = knockOff;

        // Via callback
        knockOff.IUserServiceSimple.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };

        var user = userService.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Test", user.Name);
    }

    // ========================================================================
    // docs:getting-started:simulating-failures
    // ========================================================================

    [Fact]
    public async Task SimulatingFailures_AsyncException()
    {
        var knockOff = new AsyncServiceKnockOff();
        IAsyncSaveService service = knockOff;

        knockOff.IAsyncSaveService.SaveAsync.OnCall = (ko, entity) =>
            Task.FromException<int>(new InvalidOperationException("Connection lost"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(new object()));
    }

    // ========================================================================
    // docs:getting-started:capturing-arguments
    // ========================================================================

    [Fact]
    public void CapturingArguments_ForLaterAssertions()
    {
        var knockOff = new EmailServiceKnockOff();
        IEmailService emailService = knockOff;

        List<string> sentEmails = new();

        knockOff.IEmailService.SendEmail.OnCall = (ko, to, subject, body) =>
        {
            sentEmails.Add(to);
        };

        emailService.SendEmail("admin@example.com", "S1", "B1");
        emailService.SendEmail("user@example.com", "S2", "B2");
        emailService.SendEmail("admin@example.com", "S3", "B3");

        Assert.Equal(3, sentEmails.Count);
        Assert.Contains("admin@example.com", sentEmails);
    }

    // ========================================================================
    // docs:getting-started:multiple-interfaces
    // ========================================================================

    [Fact]
    public void MultipleInterfaces_CanUseBothInterfaces()
    {
        var knockOff = new DataContextKnockOff();

        // Use either interface
        IRepository repo = knockOff.AsRepository();
        IUnitOfWork uow = knockOff.AsUnitOfWork();

        repo.Save(new object());
        uow.Commit();

        Assert.True(knockOff.IRepository.Save.WasCalled);
        Assert.True(knockOff.IUnitOfWork.Commit.WasCalled);
    }

    [Fact]
    public void MultipleInterfaces_CanCastDirectly()
    {
        var knockOff = new DataContextKnockOff();

        // Or cast directly
        IRepository repo = knockOff;

        repo.Save(new object());
        Assert.True(knockOff.IRepository.Save.WasCalled);
    }
}
