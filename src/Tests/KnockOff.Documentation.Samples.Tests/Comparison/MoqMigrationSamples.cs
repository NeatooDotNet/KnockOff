/// <summary>
/// Moq code samples for docs/migration-from-moq.md
///
/// These snippets show Moq patterns as the "Before" examples in the migration guide.
/// Types use "MoqMig" prefix to avoid conflicts with KnockOff samples which use "Mig" prefix.
///
/// Snippets in this file:
/// - moq-create-mock
/// - moq-mock-object
/// - moq-setup-returns
/// - moq-async-returns
/// - moq-verification
/// - moq-callback
/// - moq-property-setup
/// - moq-static-returns
/// - moq-conditional-returns
/// - moq-throwing-exceptions
/// - moq-setup-sequence
/// - moq-multiple-interfaces-as
/// - moq-argument-matching
/// - gradual-migration
/// </summary>

using KnockOff.Documentation.Samples.Comparison;
using Moq;

namespace KnockOff.Documentation.Samples.Tests.Comparison;

// ============================================================================
// Domain Types for Migration Samples (MoqMig prefix to avoid conflicts)
// ============================================================================

public class MoqMigUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MoqMigConfig
{
    public int Timeout { get; set; }
}

public interface IMoqMigUserService
{
    MoqMigUser? GetUser(int id);
    Task<MoqMigUser?> GetUserAsync(int id);
    void Save(MoqMigUser user);
    void Delete(int id);
    IEnumerable<MoqMigUser> GetAll();
    void Update(MoqMigUser user);
    string Name { get; set; }
}

public interface IMoqMigConfigService
{
    MoqMigConfig GetConfig();
}

public interface IMoqMigConnection
{
    void Connect();
}

public interface IMoqMigSequence
{
    int GetNext();
}

public interface IMoqMigRepository { }

public interface IMoqMigUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface IMoqMigLogger
{
    void Log(string message);
}

public interface IMoqMigOrderService { }

// ============================================================================
// Moq Samples for migration-from-moq.md
// ============================================================================

[Trait("Category", "Documentation")]
[Trait("Category", "MoqMigration")]
public class MoqMigrationSamples
{
    #region moq-create-mock
    public void CreateMock()
    {
        var mock = new Mock<IMoqMigUserService>();
        _ = mock;
    }
    #endregion

    #region moq-mock-object
    public void MockObject()
    {
        var mock = new Mock<IMoqMigUserService>();

        var service = mock.Object;
        DoSomething(mock.Object);

        _ = service;
    }

    private static void DoSomething(IMoqMigUserService service) { }
    #endregion

    #region moq-setup-returns
    public void SetupReturns()
    {
        var mock = new Mock<IMoqMigUserService>();

        mock.Setup(x => x.GetUser(It.IsAny<int>()))
            .Returns(new MoqMigUser { Id = 1, Name = "Test" });
    }
    #endregion

    #region moq-async-returns
    public void AsyncReturns()
    {
        var mock = new Mock<IMoqMigUserService>();

        mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
            .ReturnsAsync(new MoqMigUser { Id = 1 });
    }
    #endregion

    #region moq-verification
    public void Verification()
    {
        var mock = new Mock<IMoqMigUserService>();

        mock.Object.Save(new MoqMigUser());
        mock.Object.GetAll();
        mock.Object.Update(new MoqMigUser());
        mock.Object.Update(new MoqMigUser());
        mock.Object.Update(new MoqMigUser());

        mock.Verify(x => x.Save(It.IsAny<MoqMigUser>()), Times.Once);
        mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
        mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
        mock.Verify(x => x.Update(It.IsAny<MoqMigUser>()), Times.Exactly(3));
    }
    #endregion

    #region moq-callback
    public void Callback()
    {
        MoqMigUser? captured = null;
        var mock = new Mock<IMoqMigUserService>();

        mock.Setup(x => x.Save(It.IsAny<MoqMigUser>()))
            .Callback<MoqMigUser>(u => captured = u);

        _ = captured;
    }
    #endregion

    #region moq-property-setup
    public void PropertySetup()
    {
        var mock = new Mock<IMoqMigUserService>();

        mock.Setup(x => x.Name).Returns("Test");
        mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
    }
    #endregion

    #region moq-static-returns
    public void StaticReturns()
    {
        var mock = new Mock<IMoqMigConfigService>();

        mock.Setup(x => x.GetConfig()).Returns(new MoqMigConfig { Timeout = 30 });
    }
    #endregion

    #region moq-conditional-returns
    public void ConditionalReturns()
    {
        var mock = new Mock<IMoqMigUserService>();

        mock.Setup(x => x.GetUser(1)).Returns(new MoqMigUser { Name = "Admin" });
        mock.Setup(x => x.GetUser(2)).Returns(new MoqMigUser { Name = "Guest" });
        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((MoqMigUser?)null);
    }
    #endregion

    #region moq-throwing-exceptions
    public void ThrowingExceptions()
    {
        var mock = new Mock<IMoqMigConnection>();

        mock.Setup(x => x.Connect()).Throws(new TimeoutException());
    }
    #endregion

    #region moq-setup-sequence
    public void SetupSequence()
    {
        var mock = new Mock<IMoqMigSequence>();

        mock.SetupSequence(x => x.GetNext())
            .Returns(1)
            .Returns(2)
            .Returns(3);
    }
    #endregion

    #region moq-multiple-interfaces-as
    public void MultipleInterfacesAs()
    {
        var mock = new Mock<IMoqMigRepository>();
        mock.As<IMoqMigUnitOfWork>()
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var repo = mock.Object;
        var uow = mock.As<IMoqMigUnitOfWork>().Object;

        _ = (repo, uow);
    }
    #endregion

    #region moq-argument-matching
    public void ArgumentMatching()
    {
        var errors = new List<string>();
        var mock = new Mock<IMoqMigLogger>();

        mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
            .Callback<string>(s => errors.Add(s));
    }
    #endregion

    #region gradual-migration
    public void GradualMigration()
    {
        // New tests use KnockOff
        var userKnockOff = new MigUserServiceKnockOff();

        // Legacy tests keep Moq (until migrated)
        var orderMock = new Mock<IMoqMigOrderService>();

        _ = (userKnockOff, orderMock);
    }
    #endregion
}
