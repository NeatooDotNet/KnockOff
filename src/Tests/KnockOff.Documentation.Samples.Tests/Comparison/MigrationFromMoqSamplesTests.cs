using KnockOff.Documentation.Samples.Comparison;

namespace KnockOff.Documentation.Samples.Tests.Comparison;

/// <summary>
/// Tests for docs/migration-from-moq.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Comparison")]
public class MigrationFromMoqSamplesTests : SamplesTestBase
{
    // ========================================================================
    // docs:migration-from-moq:create-knockoff-class
    // ========================================================================

    [Fact]
    public void CreateKnockOffClass_CanInstantiate()
    {
        var knockOff = new MigUserServiceKnockOff();

        Assert.NotNull(knockOff);
        Assert.NotNull(knockOff.IMigUserService);
    }

    // ========================================================================
    // docs:migration-from-moq:replace-mock-object
    // ========================================================================

    [Fact]
    public void ReplaceMockObject_CastToInterface()
    {
        var knockOff = new MigUserServiceKnockOff();

        IMigUserService service = knockOff;

        Assert.NotNull(service);
    }

    [Fact]
    public void ReplaceMockObject_AsMethod()
    {
        var knockOff = new MigUserServiceKnockOff();

        IMigUserService service = knockOff.AsMigUserService();

        Assert.NotNull(service);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-setup-returns
    // ========================================================================

    [Fact]
    public void ConvertSetupReturns_OnCall()
    {
        var knockOff = new MigUserServiceKnockOff();

        knockOff.IMigUserService.GetUser.OnCall = (ko, id) =>
            new MigUser { Id = id, Name = "Test" };

        var user = knockOff.AsMigUserService().GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Test", user.Name);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-async-returns
    // ========================================================================

    [Fact]
    public async Task ConvertAsyncReturns_TaskFromResult()
    {
        var knockOff = new MigUserServiceKnockOff();

        knockOff.IMigUserService.GetUserAsync.OnCall = (ko, id) =>
            Task.FromResult<MigUser?>(new MigUser { Id = id });

        var user = await knockOff.AsMigUserService().GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-verification
    // ========================================================================

    [Fact]
    public void ConvertVerification_CallCount()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        // Set up GetAll to return empty collection (required for non-nullable return type)
        knockOff.IMigUserService.GetAll.OnCall = (ko) => Enumerable.Empty<MigUser>();

        service.Save(new MigUser());
        _ = service.GetAll();
        service.Update(new MigUser());
        service.Update(new MigUser());
        service.Update(new MigUser());

        Assert.Equal(1, knockOff.IMigUserService.Save.CallCount);
        Assert.Equal(0, knockOff.IMigUserService.Delete.CallCount);
        Assert.True(knockOff.IMigUserService.GetAll.WasCalled);
        Assert.Equal(3, knockOff.IMigUserService.Update.CallCount);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-callback
    // ========================================================================

    [Fact]
    public void ConvertCallback_AutomaticCapture()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        service.Save(new MigUser { Id = 42 });

        var captured = knockOff.IMigUserService.Save.LastCallArg;
        Assert.Equal(42, captured?.Id);
    }

    [Fact]
    public void ConvertCallback_CustomLogic()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;
        var customList = new List<MigUser>();

        knockOff.IMigUserService.Save.OnCall = (ko, user) =>
        {
            customList.Add(user);
        };

        service.Save(new MigUser { Id = 1 });
        service.Save(new MigUser { Id = 2 });

        Assert.Equal(2, customList.Count);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-property-setup
    // ========================================================================

    [Fact]
    public void ConvertPropertySetup_OnGet()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        knockOff.IMigUserService.Name.OnGet = (ko) => "Test";

        Assert.Equal("Test", service.Name);
    }

    [Fact]
    public void ConvertPropertySetup_SetterTracking()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        service.Name = "Value";

        Assert.Equal("Value", knockOff.IMigUserService.Name.LastSetValue);
    }

    // ========================================================================
    // docs:migration-from-moq:static-returns-user-method
    // ========================================================================

    [Fact]
    public void StaticReturns_UserMethod()
    {
        var knockOff = new MigConfigServiceKnockOff();
        IMigConfigService service = knockOff;

        var config = service.GetConfig();

        Assert.Equal(30, config.Timeout);
    }

    // ========================================================================
    // docs:migration-from-moq:static-returns-callback
    // ========================================================================

    [Fact]
    public void StaticReturns_Callback()
    {
        var knockOff = new MigConfigServiceKnockOff();
        IMigConfigService service = knockOff;

        // Override user method with callback
        knockOff.IMigConfigService.GetConfig.OnCall = (ko) => new MigConfig { Timeout = 60 };

        var config = service.GetConfig();

        Assert.Equal(60, config.Timeout);
    }

    // ========================================================================
    // docs:migration-from-moq:conditional-returns
    // ========================================================================

    [Fact]
    public void ConditionalReturns_SwitchExpression()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        knockOff.IMigUserService.GetUser.OnCall = (ko, id) => id switch
        {
            1 => new MigUser { Name = "Admin" },
            2 => new MigUser { Name = "Guest" },
            _ => new MigUser { Name = "Unknown" }
        };

        Assert.Equal("Admin", service.GetUser(1).Name);
        Assert.Equal("Guest", service.GetUser(2).Name);
        Assert.Equal("Unknown", service.GetUser(999).Name);
    }

    // ========================================================================
    // docs:migration-from-moq:throwing-exceptions
    // ========================================================================

    [Fact]
    public void ThrowingExceptions_OnCall()
    {
        var knockOff = new MigConnectionKnockOff();
        IMigConnection connection = knockOff;

        knockOff.IMigConnection.Connect.OnCall = (ko) =>
            throw new TimeoutException();

        Assert.Throws<TimeoutException>(() => connection.Connect());
    }

    // ========================================================================
    // docs:migration-from-moq:sequential-returns
    // ========================================================================

    [Fact]
    public void SequentialReturns_Queue()
    {
        var knockOff = new MigSequenceKnockOff();
        IMigSequence sequence = knockOff;

        var values = new Queue<int>([1, 2, 3]);
        knockOff.IMigSequence.GetNext.OnCall = (ko) => values.Dequeue();

        Assert.Equal(1, sequence.GetNext());
        Assert.Equal(2, sequence.GetNext());
        Assert.Equal(3, sequence.GetNext());
    }

    // ========================================================================
    // docs:migration-from-moq:multiple-interfaces
    // ========================================================================

    [Fact]
    public async Task MultipleInterfaces_BothWork()
    {
        var knockOff = new MigDataContextKnockOff();

        knockOff.IMigUnitOfWork.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);

        IMigRepository repo = knockOff.AsMigRepository();
        IMigUnitOfWork uow = knockOff.AsMigUnitOfWork();

        var result = await uow.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, result);
    }

    // ========================================================================
    // docs:migration-from-moq:argument-matching
    // ========================================================================

    [Fact]
    public void ArgumentMatching_InCallback()
    {
        var knockOff = new MigLoggerKnockOff();
        IMigLogger logger = knockOff;
        var errors = new List<string>();

        knockOff.IMigLogger.Log.OnCall = (ko, message) =>
        {
            if (message.Contains("error"))
                errors.Add(message);
        };

        logger.Log("info: starting");
        logger.Log("error: something failed");
        logger.Log("info: continuing");
        logger.Log("error: another failure");

        Assert.Equal(2, errors.Count);
    }

    // ========================================================================
    // docs:migration-from-moq:shared-stubs
    // ========================================================================

    [Fact]
    public void SharedStubs_UserMethod()
    {
        var knockOff = new MigSharedRepositoryKnockOff();
        IMigSharedRepository repo = knockOff;

        Assert.Equal("Admin", repo.GetById(1)?.Name);
        Assert.Equal("Guest", repo.GetById(2)?.Name);
        Assert.Null(repo.GetById(999));
    }

    // ========================================================================
    // docs:migration-from-moq:complex-callbacks
    // ========================================================================

    [Fact]
    public void ComplexCallbacks_TrackingAndModification()
    {
        var knockOff = new MigSaveServiceKnockOff();
        IMigSaveService service = knockOff;

        var nextId = 100;
        var savedEntities = new List<MigEntity>();

        knockOff.IMigSaveService.Save.OnCall = (ko, entity) =>
        {
            entity.Id = nextId++;
            savedEntities.Add(entity);
        };

        service.Save(new MigEntity());
        service.Save(new MigEntity());

        Assert.Equal(2, savedEntities.Count);
        Assert.Equal(100, savedEntities[0].Id);
        Assert.Equal(101, savedEntities[1].Id);
    }

    // ========================================================================
    // docs:migration-from-moq:automatic-tracking
    // ========================================================================

    [Fact]
    public void AutomaticTracking_NoSetupNeeded()
    {
        var knockOff = new MigProcessorKnockOff();
        IMigProcessor processor = knockOff;

        // No setup needed - just call the method
        processor.Process("data");

        // Args are captured automatically
        Assert.Equal("data", knockOff.IMigProcessor.Process.LastCallArg);
    }
}
