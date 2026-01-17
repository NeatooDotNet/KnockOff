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
        Assert.NotNull(knockOff.Name);
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
    public void ReplaceMockObject_ImplicitConversion()
    {
        var knockOff = new MigUserServiceKnockOff();

        IMigUserService service = knockOff;

        Assert.NotNull(service);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-setup-returns
    // ========================================================================

    [Fact]
    public void ConvertSetupReturns_OnCall()
    {
        var knockOff = new MigUserServiceKnockOff();

        // OnCall is a method for standalone stubs
        knockOff.GetUser.OnCall((ko, id) =>
            new MigUser { Id = id, Name = "Test" });

        IMigUserService service = knockOff;
        var user = service.GetUser(42);

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

        // OnCall is a method for standalone stubs
        knockOff.GetUserAsync.OnCall((ko, id) =>
            Task.FromResult<MigUser?>(new MigUser { Id = id }));

        IMigUserService service = knockOff;
        var user = await service.GetUserAsync(42);

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

        // Set up callbacks to enable tracking
        var saveTracking = knockOff.Save.OnCall((ko, user) => { });
        var deleteTracking = knockOff.Delete.OnCall((ko, id) => { });
        var getAllTracking = knockOff.GetAll.OnCall((ko) => Enumerable.Empty<MigUser>());
        var updateTracking = knockOff.Update.OnCall((ko, user) => { });

        service.Save(new MigUser());
        _ = service.GetAll();
        service.Update(new MigUser());
        service.Update(new MigUser());
        service.Update(new MigUser());

        Assert.Equal(1, saveTracking.CallCount);
        Assert.Equal(0, deleteTracking.CallCount);
        Assert.True(getAllTracking.WasCalled);
        Assert.Equal(3, updateTracking.CallCount);
    }

    // ========================================================================
    // docs:migration-from-moq:convert-callback
    // ========================================================================

    [Fact]
    public void ConvertCallback_AutomaticCapture()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        // Set up callback to enable tracking
        var tracking = knockOff.Save.OnCall((ko, user) => { });

        service.Save(new MigUser { Id = 42 });

        var captured = tracking.LastArg;
        Assert.Equal(42, captured?.Id);
    }

    [Fact]
    public void ConvertCallback_CustomLogic()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;
        var customList = new List<MigUser>();

        // OnCall is a method for standalone stubs
        knockOff.Save.OnCall((ko, user) =>
        {
            customList.Add(user);
        });

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

        knockOff.Name.OnGet = (ko) => "Test";

        Assert.Equal("Test", service.Name);
    }

    [Fact]
    public void ConvertPropertySetup_SetterTracking()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        service.Name = "Value";

        Assert.Equal("Value", knockOff.Name.LastSetValue);
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
    public void StaticReturns_Tracking()
    {
        var knockOff = new MigConfigServiceKnockOff();
        IMigConfigService service = knockOff;

        // User method interceptors provide tracking, not callbacks
        var config = service.GetConfig();

        // Verify the call was tracked
        Assert.True(knockOff.GetConfig2.WasCalled);
        Assert.Equal(1, knockOff.GetConfig2.CallCount);

        // User method returns default behavior
        Assert.Equal(30, config.Timeout);
    }

    // ========================================================================
    // docs:migration-from-moq:conditional-returns
    // ========================================================================

    [Fact]
    public void ConditionalReturns_SwitchExpression()
    {
        var knockOff = new MigUserServiceKnockOff();
        IMigUserService service = knockOff;

        // OnCall is a method for standalone stubs
        knockOff.GetUser.OnCall((ko, id) => id switch
        {
            1 => new MigUser { Name = "Admin" },
            2 => new MigUser { Name = "Guest" },
            _ => new MigUser { Name = "Unknown" }
        });

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

        // OnCall is a method for standalone stubs
        knockOff.Connect.OnCall((ko) =>
            throw new TimeoutException());

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
        // OnCall is a method for standalone stubs
        knockOff.GetNext.OnCall((ko) => values.Dequeue());

        Assert.Equal(1, sequence.GetNext());
        Assert.Equal(2, sequence.GetNext());
        Assert.Equal(3, sequence.GetNext());
    }

    // ========================================================================
    // docs:migration-from-moq:multiple-interfaces - Multi-interface tests removed (KO0010)
    // ========================================================================

    // ========================================================================
    // docs:migration-from-moq:argument-matching
    // ========================================================================

    [Fact]
    public void ArgumentMatching_InCallback()
    {
        var knockOff = new MigLoggerKnockOff();
        IMigLogger logger = knockOff;
        var errors = new List<string>();

        // OnCall is a method for standalone stubs
        knockOff.Log.OnCall((ko, message) =>
        {
            if (message.Contains("error"))
                errors.Add(message);
        });

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

        // OnCall is a method for standalone stubs
        knockOff.Save.OnCall((ko, entity) =>
        {
            entity.Id = nextId++;
            savedEntities.Add(entity);
        });

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
    public void AutomaticTracking_WithCallback()
    {
        var knockOff = new MigProcessorKnockOff();
        IMigProcessor processor = knockOff;

        // Set up callback to enable tracking
        var tracking = knockOff.Process.OnCall((ko, data) => { });

        processor.Process("data");

        // Args are captured by the tracking object
        Assert.Equal("data", tracking.LastArg);
    }
}
