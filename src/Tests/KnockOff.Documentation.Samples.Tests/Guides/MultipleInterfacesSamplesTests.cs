using KnockOff.Documentation.Samples.Guides;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/multiple-interfaces.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Guides")]
public class MultipleInterfacesSamplesTests
{
    [Fact]
    public void SeparateStubs_ConfigureIndependently()
    {
        var repo = new MultiRepositoryKnockOff();
        var uow = new MultiUnitOfWorkKnockOff();

        repo.GetById.OnCall((ko, id) => new MultiUser { Id = id });
        uow.SaveChangesAsync.OnCall((ko, ct) => Task.FromResult(1));

        IMultiRepository repoService = repo;
        IMultiUnitOfWork uowService = uow;

        var user = repoService.GetById(42);
        Assert.Equal(42, user?.Id);
    }

    [Fact]
    public async Task InlineStubs_SeparateInterceptors()
    {
        var repoStub = new MultiDataContextTests.Stubs.IMultiRepository();
        var uowStub = new MultiDataContextTests.Stubs.IMultiUnitOfWork();

        // Track calls on repo
        var addCount = 0;
        repoStub.Add.OnCall = (ko, user) => { addCount++; };

        // Return add count from SaveChanges
        uowStub.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(addCount);

        IMultiRepository repo = repoStub;
        IMultiUnitOfWork uow = uowStub;

        repo.Add(new MultiUser { Name = "First" });
        repo.Add(new MultiUser { Name = "Second" });

        var saved = await uow.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, saved);
        Assert.Equal(2, repoStub.Add.CallCount);
    }
}
