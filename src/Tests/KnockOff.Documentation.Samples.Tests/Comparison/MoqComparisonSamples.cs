/// <summary>
/// Moq code samples for docs/knockoff-vs-moq.md
///
/// These snippets show Moq patterns as the "Before" examples in the comparison docs.
///
/// Snippets in this file:
/// - moq-basic-setup
/// - moq-property-mocking
/// - moq-async-methods
/// - moq-argument-capture
/// - moq-multiple-interfaces
/// - moq-indexer-mocking
/// - moq-event-mocking
/// - moq-verification-patterns
/// - moq-sequential-returns
/// </summary>

using Moq;

namespace KnockOff.Documentation.Samples.Tests.Comparison;

// ============================================================================
// Domain Types for Moq Comparison Samples
// ============================================================================

public class MoqUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MoqEntity
{
    public int Id { get; set; }
}

public class MoqPropertyInfo
{
    public string Value { get; set; } = string.Empty;
}

public interface IMoqUserService
{
    MoqUser? GetUser(int id);
    MoqUser? CurrentUser { get; set; }
}

public interface IMoqRepository
{
    Task<MoqEntity?> GetByIdAsync(int id);
    void Save(MoqEntity entity);
    void Delete(int id);
    IEnumerable<MoqEntity> GetAll();
    void Update(MoqEntity entity);
}

public interface IMoqEmployeeRepository { }

public interface IMoqUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface IMoqPropertyStore
{
    MoqPropertyInfo? this[string key] { get; set; }
}

public interface IMoqEventSource
{
    event EventHandler<string>? DataReceived;
}

public interface IMoqSequence
{
    int GetNext();
}

// ============================================================================
// Moq Samples for knockoff-vs-moq.md
// ============================================================================

[Trait("Category", "Documentation")]
[Trait("Category", "MoqComparison")]
public class MoqComparisonSamples
{
    #region moq-basic-setup
    [Fact]
    public void BasicSetupAndVerification()
    {
        var mock = new Mock<IMoqUserService>();
        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(new MoqUser { Id = 1 });

        var service = mock.Object;
        var user = service.GetUser(42);

        mock.Verify(x => x.GetUser(42), Times.Once);
    }
    #endregion

    #region moq-property-mocking
    [Fact]
    public void PropertyMocking()
    {
        var mock = new Mock<IMoqUserService>();
        mock.Setup(x => x.CurrentUser).Returns(new MoqUser { Name = "Test" });
        mock.SetupSet(x => x.CurrentUser = It.IsAny<MoqUser>()).Verifiable();

        var user = mock.Object.CurrentUser;
        mock.Object.CurrentUser = new MoqUser { Name = "New" };

        mock.VerifySet(x => x.CurrentUser = It.IsAny<MoqUser>(), Times.Once);
    }
    #endregion

    #region moq-async-methods
    [Fact]
    public async Task AsyncMethods()
    {
        var mock = new Mock<IMoqRepository>();
        mock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new MoqEntity { Id = 1 });

        var entity = await mock.Object.GetByIdAsync(42);

        Assert.Equal(1, entity?.Id);
    }
    #endregion

    #region moq-argument-capture
    [Fact]
    public void ArgumentCapture()
    {
        MoqEntity? captured = null;
        var mock = new Mock<IMoqRepository>();
        mock.Setup(x => x.Save(It.IsAny<MoqEntity>()))
            .Callback<MoqEntity>(e => captured = e);

        mock.Object.Save(new MoqEntity { Id = 1 });

        Assert.Equal(1, captured?.Id);
    }
    #endregion

    #region moq-multiple-interfaces
    [Fact]
    public async Task MultipleInterfaces()
    {
        var mock = new Mock<IMoqEmployeeRepository>();
        mock.As<IMoqUnitOfWork>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var repo = mock.Object;
        var unitOfWork = mock.As<IMoqUnitOfWork>().Object;

        var result = await unitOfWork.SaveChangesAsync(CancellationToken.None);
        Assert.Equal(1, result);
    }
    #endregion

    #region moq-indexer-mocking
    [Fact]
    public void IndexerMocking()
    {
        var mock = new Mock<IMoqPropertyStore>();
        mock.Setup(x => x["Name"]).Returns(new MoqPropertyInfo { Value = "Test" });
        mock.Setup(x => x["Age"]).Returns(new MoqPropertyInfo { Value = "25" });

        var name = mock.Object["Name"];

        Assert.Equal("Test", name?.Value);
    }
    #endregion

    #region moq-event-mocking
    [Fact]
    public void EventMocking()
    {
        var mock = new Mock<IMoqEventSource>();

        string? receivedData = null;
        mock.Object.DataReceived += (sender, data) => receivedData = data;

        // Raise event
        mock.Raise(x => x.DataReceived += null, mock.Object, "test data");

        Assert.Equal("test data", receivedData);
    }
    #endregion

    #region moq-verification-patterns
    [Fact]
    public void VerificationPatterns()
    {
        var mock = new Mock<IMoqRepository>();

        mock.Object.Save(new MoqEntity());
        mock.Object.GetAll();
        mock.Object.Update(new MoqEntity());
        mock.Object.Update(new MoqEntity());
        mock.Object.Update(new MoqEntity());

        mock.Verify(x => x.Save(It.IsAny<MoqEntity>()), Times.Once);
        mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
        mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
        mock.Verify(x => x.Update(It.IsAny<MoqEntity>()), Times.Exactly(3));
    }
    #endregion

    #region moq-sequential-returns
    [Fact]
    public void SequentialReturns()
    {
        var mock = new Mock<IMoqSequence>();
        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Returns(2)
            .Returns(3);

        Assert.Equal(1, mock.Object.GetNext());
        Assert.Equal(2, mock.Object.GetNext());
        Assert.Equal(3, mock.Object.GetNext());
    }
    #endregion
}
