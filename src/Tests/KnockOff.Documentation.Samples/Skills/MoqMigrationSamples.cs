/// <summary>
/// Code samples for ~/.claude/skills/knockoff/moq-migration.md
///
/// Snippets in this file:
/// - skill:moq-migration:step1-create
/// - skill:moq-migration:step2-object
/// - skill:moq-migration:step3-setup
/// - skill:moq-migration:step4-async
/// - skill:moq-migration:step5-verify
/// - skill:moq-migration:step6-callback
/// - skill:moq-migration:static-returns
/// - skill:moq-migration:conditional-returns
/// - skill:moq-migration:throwing-exceptions
/// - skill:moq-migration:sequential-returns
/// - skill:moq-migration:property-setup
/// - skill:moq-migration:multiple-interfaces
/// - skill:moq-migration:argument-matching
/// - skill:moq-migration:method-overloads
/// - skill:moq-migration:out-params
/// - skill:moq-migration:ref-params
///
/// Corresponding tests: MoqMmrationSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Mmration Samples
// ============================================================================

public class MmUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MmConfig
{
    public int Timeout { get; set; }
}

// ============================================================================
// Step 1: Create KnockOff Class
// ============================================================================

public interface IMmUserService
{
    MmUser? GetUser(int id);
    Task<MmUser?> GetUserAsync(int id);
    void Save(MmUser user);
    void Delete(int id);
    IEnumerable<MmUser> GetAll();
    void Update(MmUser user);
}

#region skill:moq-migration:step1-create
// Before: Moq
// var mock = new Mock<IMmUserService>();

// After: Create class once
[KnockOff]
public partial class MmUserServiceKnockOff : IMmUserService { }

// In test
// var knockOff = new MmUserServiceKnockOff();
#endregion

// ============================================================================
// Step 2: Replace mock.Object
// ============================================================================

#region skill:moq-migration:step2-object
// Before
// var service = mock.Object;
// DoWork(mock.Object);

// After
// IMmUserService service = knockOff;
// DoWork(knockOff.AsMmUserService());
#endregion

// ============================================================================
// Step 3: Convert Setup/Returns
// ============================================================================

#region skill:moq-migration:step3-setup
// Before
// mock.Setup(x => x.GetUser(It.IsAny<int>()))
//     .Returns(new MmUser { Id = 1 });

// After
// knockOff.IMmUserService.GetUser.OnCall = (ko, id) =>
//     new MmUser { Id = id };
#endregion

// ============================================================================
// Step 4: Convert ReturnsAsync
// ============================================================================

#region skill:moq-migration:step4-async
// Before
// mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
//     .ReturnsAsync(new MmUser { Id = 1 });

// After
// knockOff.IMmUserService.GetUserAsync.OnCall = (ko, id) =>
//     Task.FromResult<MmUser?>(new MmUser { Id = id });
#endregion

// ============================================================================
// Step 5: Convert Verify
// ============================================================================

#region skill:moq-migration:step5-verify
// Before
// mock.Verify(x => x.Save(It.IsAny<MmUser>()), Times.Once);
// mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
// mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
// mock.Verify(x => x.Update(It.IsAny<MmUser>()), Times.Exactly(3));

// After
// Assert.Equal(1, knockOff.IMmUserService.Save.CallCount);
// Assert.Equal(0, knockOff.IMmUserService.Delete.CallCount);
// Assert.True(knockOff.IMmUserService.GetAll.WasCalled);
// Assert.Equal(3, knockOff.IMmUserService.Update.CallCount);
#endregion

// ============================================================================
// Step 6: Convert Callback
// ============================================================================

#region skill:moq-migration:step6-callback
// Before
// MmUser? captured = null;
// mock.Setup(x => x.Save(It.IsAny<MmUser>()))
//     .Callback<MmUser>(u => captured = u);

// After (automatic tracking)
// service.Save(user);
// var captured = knockOff.IMmUserService.Save.LastCallArg;

// Or with callback
// knockOff.IMmUserService.Save.OnCall = (ko, user) =>
// {
//     customList.Add(user);
// };
#endregion

// ============================================================================
// Static Returns
// ============================================================================

public interface IMmConfigService
{
    MmConfig GetConfig();
}

#region skill:moq-migration:static-returns
// Moq
// mock.Setup(x => x.GetConfig()).Returns(new MmConfig { Timeout = 30 });

// KnockOff Option 1: User method
[KnockOff]
public partial class MmConfigServiceKnockOff : IMmConfigService
{
    protected MmConfig GetConfig() => new MmConfig { Timeout = 30 };
}

// KnockOff Option 2: Callback
// knockOff.IMmConfigService.GetConfig.OnCall = (ko) => new MmConfig { Timeout = 30 };
#endregion

// ============================================================================
// Conditional Returns
// ============================================================================

#region skill:moq-migration:conditional-returns
// Moq
// mock.Setup(x => x.GetUser(1)).Returns(new MmUser { Name = "Admin" });
// mock.Setup(x => x.GetUser(2)).Returns(new MmUser { Name = "Guest" });
// mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((MmUser?)null);

// KnockOff
// knockOff.IMmUserService.GetUser.OnCall = (ko, id) => id switch
// {
//     1 => new MmUser { Name = "Admin" },
//     2 => new MmUser { Name = "Guest" },
//     _ => null
// };
#endregion

// ============================================================================
// Throwing Exceptions
// ============================================================================

public interface IMmConnectionService
{
    void Connect();
}

#region skill:moq-migration:throwing-exceptions
[KnockOff]
public partial class MmConnectionKnockOff : IMmConnectionService { }

// Moq
// mock.Setup(x => x.Connect()).Throws(new TimeoutException());

// KnockOff
// knockOff.IMmConnectionService.Connect.OnCall = (ko) =>
//     throw new TimeoutException();
#endregion

// ============================================================================
// Sequential Returns
// ============================================================================

public interface IMmSequenceService
{
    int GetNext();
}

#region skill:moq-migration:sequential-returns
[KnockOff]
public partial class MmSequenceKnockOff : IMmSequenceService { }

// Moq
// mock.SetupSequence(x => x.GetNext())
//     .Returns(1)
//     .Returns(2)
//     .Returns(3);

// KnockOff
// var results = new Queue<int>([1, 2, 3]);
// knockOff.IMmSequenceService.GetNext.OnCall = (ko) => results.Dequeue();
#endregion

// ============================================================================
// Property Setup
// ============================================================================

public interface IMmPropService
{
    string Name { get; set; }
}

#region skill:moq-migration:property-setup
[KnockOff]
public partial class MmPropServiceKnockOff : IMmPropService { }

// Moq
// mock.Setup(x => x.Name).Returns("Test");
// mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();

// KnockOff
// knockOff.IMmPropService.Name.OnGet = (ko) => "Test";
// Setter tracking is automatic
// service.Name = "Value";
// Assert.Equal("Value", knockOff.IMmPropService.Name.LastSetValue);
#endregion

// ============================================================================
// Multiple Interfaces
// ============================================================================

public interface IMmRepository
{
    void Save(object entity);
}

public interface IMmUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

#region skill:moq-migration:multiple-interfaces
// Moq
// var mock = new Mock<IMmRepository>();
// mock.As<IMmUnitOfWork>()
//     .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
//     .ReturnsAsync(1);

// KnockOff
[KnockOff]
public partial class MmDataContextKnockOff : IMmRepository, IMmUnitOfWork { }

// knockOff.IMmUnitOfWork.SaveChangesAsync.OnCall = (ko, ct) =>
//     Task.FromResult(1);
// IMmRepository repo = knockOff.AsMmRepository();
// IMmUnitOfWork uow = knockOff.AsMmUnitOfWork();
#endregion

// ============================================================================
// Argument Matching
// ============================================================================

public interface IMmLogger
{
    void Log(string message);
}

#region skill:moq-migration:argument-matching
[KnockOff]
public partial class MmLoggerKnockOff : IMmLogger { }

// Moq
// mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
//     .Callback<string>(s => errors.Add(s));

// KnockOff
// knockOff.IMmLogger.Log.OnCall = (ko, message) =>
// {
//     if (message.Contains("error"))
//         errors.Add(message);
// };
#endregion

// ============================================================================
// Method Overloads
// ============================================================================

public interface IMmProcessorService
{
    void Process(string data);
    void Process(string data, int priority);
    int Calculate(int value);
    int Calculate(int a, int b);
}

#region skill:moq-migration:method-overloads
[KnockOff]
public partial class MmProcessorKnockOff : IMmProcessorService { }

// Moq - can setup specific overloads
// mock.Setup(x => x.Process("specific")).Returns(...);
// mock.Setup(x => x.Process(It.IsAny<string>(), It.IsAny<int>())).Returns(...);

// KnockOff - each overload has its own handler (1-based suffix)
// knockOff.IMmProcessorService.Process1.OnCall = (ko, data) => { /* 1-param overload */ };
// knockOff.IMmProcessorService.Process2.OnCall = (ko, data, priority) => { /* 2-param overload */ };

// For return values
// knockOff.IMmProcessorService.Calculate1.OnCall = (ko, value) => value * 2;
// knockOff.IMmProcessorService.Calculate2.OnCall = (ko, a, b) => a + b;
#endregion

// ============================================================================
// Out Parameters
// ============================================================================

public interface IMmParser
{
    bool TryParse(string input, out int result);
}

#region skill:moq-migration:out-params
[KnockOff]
public partial class MmParserKnockOff : IMmParser { }

// Moq
// mock.Setup(x => x.TryParse(It.IsAny<string>(), out It.Ref<int>.IsAny))
//     .Returns(new TryParseDelegate((string input, out int result) =>
//     {
//         return int.TryParse(input, out result);
//     }));

// KnockOff - explicit delegate type required
// knockOff.IMmParser.TryParse.OnCall =
//     (IMmParser_TryParseHandler.TryParseDelegate)((ko, string input, out int result) =>
//     {
//         return int.TryParse(input, out result);
//     });

// Tracking: only input params (out excluded)
// Assert.Equal("42", knockOff.IMmParser.TryParse.LastCallArg);
#endregion

// ============================================================================
// Ref Parameters
// ============================================================================

public interface IMmRefProcessor
{
    void Increment(ref int value);
}

#region skill:moq-migration:ref-params
[KnockOff]
public partial class MmRefProcessorKnockOff : IMmRefProcessor { }

// Moq
// mock.Setup(x => x.Increment(ref It.Ref<int>.IsAny))
//     .Callback(new IncrementDelegate((ref int value) => value++));

// KnockOff - explicit delegate type required
// knockOff.IMmRefProcessor.Increment.OnCall =
//     (IMmRefProcessor_IncrementHandler.IncrementDelegate)((ko, ref int value) =>
//     {
//         value++;
//     });

// Tracking captures INPUT value (before modification)
// int x = 5;
// processor.Increment(ref x);
// Assert.Equal(6, x);  // Modified
// Assert.Equal(5, knockOff.IMmRefProcessor.Increment.LastCallArg);  // Original
#endregion

// ============================================================================
// Gradual Mmration
// ============================================================================

public interface IMmOrderService
{
    void PlaceOrder(int orderId);
}

[KnockOff]
public partial class MmSharedRepositoryKnockOff : IMmRepository { }

// Use both in same project (gradual migration example)
// var userKnockOff = new MmUserServiceKnockOff();          // New tests: KnockOff
// var orderMock = new Mock<IMmOrderService>();              // Legacy tests: Keep Moq
