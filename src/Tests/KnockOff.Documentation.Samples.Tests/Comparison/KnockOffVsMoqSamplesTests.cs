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

        Assert.Equal(1, knockOff.GetUser2.CallCount);
        Assert.Equal(42, knockOff.GetUser2.LastCallArg);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:property-stub
    // ========================================================================

    [Fact]
    public void PropertyStub_OnGet_ReturnsDynamicValue()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        knockOff.CurrentUser.OnGet = (ko) => new VsUser { Name = "Test" };

        var user = service.CurrentUser;

        Assert.Equal("Test", user?.Name);
        Assert.Equal(1, knockOff.CurrentUser.GetCount);
    }

    [Fact]
    public void PropertyStub_OnSet_TracksValue()
    {
        var knockOff = new VsUserServiceKnockOff();
        IVsUserService service = knockOff;

        service.CurrentUser = new VsUser { Name = "New" };

        Assert.Equal(1, knockOff.CurrentUser.SetCount);
        Assert.Equal("New", knockOff.CurrentUser.LastSetValue?.Name);
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

        var captured = knockOff.Save.LastCallArg;
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

        Assert.Equal(3, knockOff.Save.CallCount);
        Assert.Equal(3, knockOff.Save.LastCallArg?.Id);
    }

    // ========================================================================
    // docs:knockoff-vs-moq:multiple-interfaces - Multi-interface tests removed (KO0010)
    // ========================================================================

    // ========================================================================
    // docs:knockoff-vs-moq:indexer-stub
    // ========================================================================

    [Fact]
    public void IndexerStub_BackingDictionary_Works()
    {
        var knockOff = new VsPropertyStoreKnockOff();
        knockOff.StringIndexerBacking["Name"] = new VsPropertyInfo { Value = "Test" };
        knockOff.StringIndexerBacking["Age"] = new VsPropertyInfo { Value = "25" };

        IVsPropertyStore store = knockOff;
        var name = store["Name"];

        Assert.Equal("Test", name?.Value);
        Assert.Equal("Name", knockOff.StringIndexer.LastGetKey);
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

        Assert.True(knockOff.DataReceived.HasSubscribers);
        Assert.Equal(1, knockOff.DataReceived.AddCount);
    }

    [Fact]
    public void EventStub_Raise_Works()
    {
        var knockOff = new VsEventSourceKnockOff();
        IVsEventSource source = knockOff;
        string? receivedData = null;

        source.DataReceived += (sender, data) => receivedData = data;

        knockOff.DataReceived.Raise(null, "test data");

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
        knockOff.GetAll.OnCall = (ko) => Enumerable.Empty<VsEntity>();

        repo.Save(new VsEntity());
        repo.Update(new VsEntity());
        repo.Update(new VsEntity());
        repo.Update(new VsEntity());
        _ = repo.GetAll();

        Assert.Equal(1, knockOff.Save.CallCount);      // Times.Once
        Assert.Equal(0, knockOff.Delete.CallCount);    // Times.Never
        Assert.True(knockOff.GetAll.WasCalled);        // Times.AtLeastOnce
        Assert.Equal(3, knockOff.Update.CallCount);    // Times.Exactly(3)
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
        knockOff.GetNext.OnCall = (ko) => returnValues.Dequeue();

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

        knockOff.GetUser2.OnCall = (ko, id) => new VsUser { Id = id, Name = "Special" };

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

        knockOff.GetUser2.OnCall = (ko, id) => new VsUser { Name = "First" };
        var user1 = service.GetUser(1);
        Assert.Equal("First", user1.Name);

        knockOff.GetUser2.Reset();

        // Now falls back to user method
        var user2 = service.GetUser(2);
        Assert.Equal("Default", user2.Name);
        Assert.Equal(1, knockOff.GetUser2.CallCount); // Reset cleared previous count
    }
}
