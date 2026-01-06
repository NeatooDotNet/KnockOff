using KnockOff.Documentation.Samples.Comparison;

namespace KnockOff.Documentation.Samples.Tests.Comparison;

/// <summary>
/// Tests for docs/knockoff-vs-moq.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Comparison")]
public class KnockOffVsMoqSamplesTests : SamplesTestBase
{
    // ========================================================================
    // docs:knockoff-vs-moq:basic-stub
    // ========================================================================

    [Fact]
    public void BasicStub_GetUser_ReturnsUser()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        var user = service.GetUser(42);

        Assert.Equal(42, user.Id);
        Assert.Equal("Test User", user.Name);
    }

    [Fact]
    public void BasicStub_Verification_TracksCall()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        service.GetUser(42);

        Assert.Equal(1, knockOff.IVsUserService.GetUser.CallCount);
        Assert.Equal(42, knockOff.IVsUserService.GetUser.LastCallArg);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:property-stub
    // ========================================================================

    [Fact]
    public void PropertyStub_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        knockOff.IVsUserService.CurrentUser.OnGet = (ko) => new VsUser { Name = "Test" };

        var user = service.CurrentUser;

        Assert.Equal("Test", user?.Name);
        Assert.Equal(1, knockOff.IVsUserService.CurrentUser.GetCount);
    }

    [Fact]
    public void PropertyStub_OnSet_TracksValue()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        service.CurrentUser = new VsUser { Name = "New" };

        Assert.Equal(1, knockOff.IVsUserService.CurrentUser.SetCount);
        Assert.Equal("New", knockOff.IVsUserService.CurrentUser.LastSetValue?.Name);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:async-stub
    // ========================================================================

    [Fact]
    public async Task AsyncStub_GetByIdAsync_ReturnsEntity()
    {
        var knockOff = new VsRepositoryKnockOff();
        IVsRepository repo = knockOff;

        var entity = await repo.GetByIdAsync(42);

        Assert.NotNull(entity);
        Assert.Equal(42, entity.Id);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:argument-capture
    // ========================================================================

    [Fact]
    public void ArgumentCapture_AutomaticTracking()
    {
        var knockOff = new VsRepositoryKnockOff();
        IVsRepository repo = knockOff;

        repo.Save(new VsEntity { Id = 1 });

        var captured = knockOff.IVsRepository.Save.LastCallArg;
        Assert.Equal(1, captured?.Id);
    }

    [Fact]
    public void ArgumentCapture_AllCalls_TracksHistory()
    {
        var knockOff = new VsRepositoryKnockOff();
        IVsRepository repo = knockOff;

        repo.Save(new VsEntity { Id = 1 });
        repo.Save(new VsEntity { Id = 2 });
        repo.Save(new VsEntity { Id = 3 });

        Assert.Equal(3, knockOff.IVsRepository.Save.CallCount);
        Assert.Equal(3, knockOff.IVsRepository.Save.LastCallArg?.Id);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:multiple-interfaces
    // ========================================================================

    [Fact]
    public async Task MultipleInterfaces_BothInterfacesWork()
    {
        var knockOff = new VsEmployeeRepoKnockOff();
        IVsEmployeeRepository repo = knockOff.AsVsEmployeeRepository();
        IVsUnitOfWork unitOfWork = knockOff.AsVsUnitOfWork();

        var result = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, result);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:indexer-stub
    // ========================================================================

    [Fact]
    public void IndexerStub_BackingDictionary_Works()
    {
        var knockOff = new VsPropertyStoreKnockOff();
        knockOff.IVsPropertyStore_StringIndexerBacking["Name"] = new VsPropertyInfo { Value = "Test" };
        knockOff.IVsPropertyStore_StringIndexerBacking["Age"] = new VsPropertyInfo { Value = "25" };

        IVsPropertyStore store = knockOff;
        var name = store["Name"];

        Assert.Equal("Test", name?.Value);
        Assert.Equal("Name", knockOff.IVsPropertyStore.StringIndexer.LastGetKey);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:event-stub
    // ========================================================================

    [Fact]
    public void EventStub_Subscription_Tracked()
    {
        var knockOff = new VsEventSourceKnockOff();
        IVsEventSource source = knockOff;

        source.DataReceived += (sender, data) => { };

        Assert.True(knockOff.IVsEventSource.DataReceived.HasSubscribers);
        Assert.Equal(1, knockOff.IVsEventSource.DataReceived.SubscribeCount);
    }

    [Fact]
    public void EventStub_Raise_Works()
    {
        var knockOff = new VsEventSourceKnockOff();
        IVsEventSource source = knockOff;
        string? receivedData = null;

        source.DataReceived += (sender, data) => receivedData = data;

        knockOff.IVsEventSource.DataReceived.Raise("test data");

        Assert.True(knockOff.IVsEventSource.DataReceived.WasRaised);
        Assert.Equal("test data", receivedData);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:verification-patterns
    // ========================================================================

    [Fact]
    public void VerificationPatterns_CallCount()
    {
        var knockOff = new VsVerificationRepositoryKnockOff();
        IVsVerificationRepository repo = knockOff;

        // Set up GetAll to return empty collection (required for non-nullable return type)
        knockOff.IVsVerificationRepository.GetAll.OnCall = (ko) => Enumerable.Empty<VsEntity>();

        repo.Save(new VsEntity());
        repo.Update(new VsEntity());
        repo.Update(new VsEntity());
        repo.Update(new VsEntity());
        _ = repo.GetAll();

        Assert.Equal(1, knockOff.IVsVerificationRepository.Save.CallCount);      // Times.Once
        Assert.Equal(0, knockOff.IVsVerificationRepository.Delete.CallCount);    // Times.Never
        Assert.True(knockOff.IVsVerificationRepository.GetAll.WasCalled);        // Times.AtLeastOnce
        Assert.Equal(3, knockOff.IVsVerificationRepository.Update.CallCount);    // Times.Exactly(3)
    }

    // ========================================================================
    // docs:knockoff-vs-moq:sequential-returns
    // ========================================================================

    [Fact]
    public void SequentialReturns_Queue_Works()
    {
        var knockOff = new VsSequenceKnockOff();
        IVsSequence sequence = knockOff;

        var returnValues = new Queue<int>([1, 2, 3]);
        knockOff.IVsSequence.GetNext.OnCall = (ko) => returnValues.Dequeue();

        Assert.Equal(1, sequence.GetNext());
        Assert.Equal(2, sequence.GetNext());
        Assert.Equal(3, sequence.GetNext());
    }

    // ========================================================================
    // docs:knockoff-vs-moq:per-test-override
    // ========================================================================

    [Fact]
    public void PerTestOverride_DefaultBehavior()
    {
        var knockOff = new VsOverrideServiceKnockOff();
        IVsOverrideService service = knockOff;

        var user = service.GetUser(42);

        Assert.Equal("Default", user.Name);
    }

    [Fact]
    public void PerTestOverride_CallbackOverrides()
    {
        var knockOff = new VsOverrideServiceKnockOff();
        IVsOverrideService service = knockOff;

        knockOff.IVsOverrideService.GetUser.OnCall = (ko, id) => new VsUser { Id = id, Name = "Special" };

        var user = service.GetUser(42);

        Assert.Equal("Special", user.Name);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:reset-and-reuse
    // ========================================================================

    [Fact]
    public void ResetAndReuse_ClearsCallbackAndTracking()
    {
        var knockOff = new VsOverrideServiceKnockOff();
        IVsOverrideService service = knockOff;

        knockOff.IVsOverrideService.GetUser.OnCall = (ko, id) => new VsUser { Name = "First" };
        var user1 = service.GetUser(1);
        Assert.Equal("First", user1.Name);

        knockOff.IVsOverrideService.GetUser.Reset();

        // Now falls back to user method
        var user2 = service.GetUser(2);
        Assert.Equal("Default", user2.Name);
        Assert.Equal(1, knockOff.IVsOverrideService.GetUser.CallCount); // Reset cleared previous count
    }
}
