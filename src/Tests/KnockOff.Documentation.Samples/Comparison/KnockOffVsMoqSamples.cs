/// <summary>
/// Code samples for docs/knockoff-vs-moq.md
///
/// Snippets in this file:
/// - docs:knockoff-vs-moq:basic-stub
/// - docs:knockoff-vs-moq:basic-verification
/// - docs:knockoff-vs-moq:property-stub
/// - docs:knockoff-vs-moq:async-stub
/// - docs:knockoff-vs-moq:argument-capture
/// - docs:knockoff-vs-moq:multiple-interfaces
/// - docs:knockoff-vs-moq:indexer-stub
/// - docs:knockoff-vs-moq:event-stub
/// - docs:knockoff-vs-moq:verification-patterns
/// - docs:knockoff-vs-moq:sequential-returns
/// - docs:knockoff-vs-moq:per-test-override
/// - docs:knockoff-vs-moq:basic-stub-usage
/// - docs:knockoff-vs-moq:property-mocking
/// - docs:knockoff-vs-moq:argument-capture
/// - docs:knockoff-vs-moq:async-stub-usage
/// - docs:knockoff-vs-moq:multiple-interfaces-usage
/// - docs:knockoff-vs-moq:indexer-stub-usage
/// - docs:knockoff-vs-moq:event-stub-usage
/// - docs:knockoff-vs-moq:verification-patterns-usage
/// - docs:knockoff-vs-moq:sequential-returns-usage
/// - docs:knockoff-vs-moq:per-test-override-usage
/// - docs:knockoff-vs-moq:reset-and-reuse
///
/// Corresponding tests: KnockOffVsMoqSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Comparison;

// ============================================================================
// Domain Types for Comparison
// ============================================================================

public class VsUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VsEntity
{
    public int Id { get; set; }
}

public class VsPropertyInfo
{
    public object? Value { get; set; }
}

// ============================================================================
// Basic Setup and Verification
// ============================================================================

public interface IVsUserService
{
    VsUser GetUser(int id);
    VsUser? CurrentUser { get; set; }
    VsUser? Save(VsUser user);
    void Delete(int id);
    IEnumerable<VsUser> GetAll();
    void Update(VsUser user);
}

#region knockoff-vs-moq-basic-stub
[KnockOff]
public partial class VsUserServiceKnockOff : IVsUserService
{
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Test User" };
}
#endregion

// ============================================================================
// Async Methods
// ============================================================================

public interface IVsRepository
{
    Task<VsEntity?> GetByIdAsync(int id);
    void Save(VsEntity entity);
}

#region knockoff-vs-moq-async-stub
[KnockOff]
public partial class VsRepositoryKnockOff : IVsRepository
{
    protected Task<VsEntity?> GetByIdAsync(int id) =>
        Task.FromResult<VsEntity?>(new VsEntity { Id = id });
}
#endregion

// ============================================================================
// Multiple Interface Implementation
// ============================================================================

public interface IVsEmployeeRepository
{
    VsUser? GetEmployee(int id);
}

public interface IVsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
#region knockoff-vs-moq-multiple-interfaces
[KnockOff]
public partial class VsEmployeeRepoKnockOff : IVsEmployeeRepository
{
}

[KnockOff]
public partial class VsUnitOfWorkKnockOff : IVsUnitOfWork
{
    protected Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}
#endregion

// ============================================================================
// Indexer Mocking
// ============================================================================

public interface IVsPropertyStore
{
    VsPropertyInfo? this[string key] { get; set; }
}

#region knockoff-vs-moq-indexer-stub
[KnockOff]
public partial class VsPropertyStoreKnockOff : IVsPropertyStore { }
#endregion

// ============================================================================
// Event Mocking
// ============================================================================

public interface IVsEventSource
{
    event EventHandler<string>? DataReceived;
}

#region knockoff-vs-moq-event-stub
[KnockOff]
public partial class VsEventSourceKnockOff : IVsEventSource { }
#endregion

// ============================================================================
// Verification Patterns
// ============================================================================

public interface IVsVerificationRepository
{
    void Save(VsEntity entity);
    void Delete(int id);
    IEnumerable<VsEntity> GetAll();
    void Update(VsEntity entity);
}

#region knockoff-vs-moq-verification-patterns
[KnockOff]
public partial class VsVerificationRepositoryKnockOff : IVsVerificationRepository { }
#endregion

// ============================================================================
// Sequential Returns
// ============================================================================

public interface IVsSequence
{
    int GetNext();
}

#region knockoff-vs-moq-sequential-returns
[KnockOff]
public partial class VsSequenceKnockOff : IVsSequence { }
#endregion

// ============================================================================
// Per-Test Override
// ============================================================================

public interface IVsOverrideService
{
    VsUser GetUser(int id);
}

#region knockoff-vs-moq-per-test-override
[KnockOff]
public partial class VsOverrideServiceKnockOff : IVsOverrideService
{
    // Default behavior for most tests
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Default" };
}
#endregion

// ============================================================================
// Comment-based usage examples
// ============================================================================

#region knockoff-vs-moq-basic-stub-usage
// // Use in test
// var knockOff = new VsUserServiceKnockOff();
// IVsUserService service = knockOff;
//
// var user = service.GetUser(42);
//
// Assert.Equal(1, knockOff.GetUser.CallCount);
// Assert.Equal(42, knockOff.GetUser.LastCallArg);
#endregion

#region knockoff-vs-moq-property-mocking
// // Define stub - properties use auto-generated backing fields
// [KnockOff]
// public partial class UserServiceKnockOff : IUserService { }
//
// // Use in test
// var knockOff = new UserServiceKnockOff();
// IUserService service = knockOff;
//
// // Optional: customize getter behavior
// knockOff.CurrentUser.OnGet = (ko) => new User { Name = "Test" };
//
// var user = service.CurrentUser;
// service.CurrentUser = new User { Name = "New" };
//
// Assert.Equal(1, knockOff.CurrentUser.GetCount);
// Assert.Equal(1, knockOff.CurrentUser.SetCount);
// Assert.Equal("New", knockOff.CurrentUser.LastSetValue?.Name);
#endregion

#region knockoff-vs-moq-argument-capture
// // Define stub - arguments are captured automatically
// [KnockOff]
// public partial class RepositoryKnockOff : IRepository
// {
//     protected void Save(Entity entity) { /* optional logic */ }
// }
//
// // Use in test - no callback setup needed
// var knockOff = new RepositoryKnockOff();
// IRepository repo = knockOff;
//
// repo.Save(new Entity { Id = 1 });
//
// var captured = knockOff.Save.LastCallArg;
// Assert.Equal(1, captured?.Id);
#endregion

#region knockoff-vs-moq-async-stub-usage
// // Use in test
// var knockOff = new VsRepositoryKnockOff();
// var entity = await knockOff.AsIVsRepository().GetByIdAsync(42);
#endregion

#region knockoff-vs-moq-multiple-interfaces-usage
// // Use in tests - separate stubs for each interface
// var repoKnockOff = new VsEmployeeRepoKnockOff();
// IVsEmployeeRepository repo = repoKnockOff.AsIVsEmployeeRepository();
//
// var uowKnockOff = new VsUnitOfWorkKnockOff();
// IVsUnitOfWork unitOfWork = uowKnockOff.AsIVsUnitOfWork();
#endregion

#region knockoff-vs-moq-indexer-stub-usage
// // Use in test - pre-populate backing dictionary
// var knockOff = new VsPropertyStoreKnockOff();
// knockOff.Indexer.Backing["Name"] = new VsPropertyInfo { Value = "Test" };
// knockOff.Indexer.Backing["Age"] = new VsPropertyInfo { Value = "25" };
//
// IVsPropertyStore store = knockOff;
// var name = store["Name"];
//
// Assert.Equal("Name", knockOff.Indexer.LastGetKey);
#endregion

#region knockoff-vs-moq-event-stub-usage
// // Use in test
// var knockOff = new VsEventSourceKnockOff();
// IVsEventSource source = knockOff;
//
// string? receivedData = null;
//
// // Subscribe (tracked automatically)
// source.DataReceived += (sender, data) => receivedData = data;
//
// Assert.True(knockOff.DataReceived.HasSubscribers);
// Assert.Equal(1, knockOff.DataReceived.AddCount);
//
// // Raise event (requires sender parameter for EventHandler<T>)
// knockOff.DataReceived.Raise(null, "test data");
//
// Assert.Equal("test data", receivedData);
#endregion

#region knockoff-vs-moq-verification-patterns-usage
// // Verify in test
// Assert.Equal(1, knockOff.Save.CallCount);      // Times.Once
// Assert.Equal(0, knockOff.Delete.CallCount);    // Times.Never
// Assert.True(knockOff.GetAll.WasCalled);        // Times.AtLeastOnce
// Assert.Equal(3, knockOff.Update.CallCount);    // Times.Exactly(3)
#endregion

#region knockoff-vs-moq-sequential-returns-usage
// var knockOff = new VsSequenceKnockOff();
// var returnValues = new Queue<int>([1, 2, 3]);
// knockOff.GetNext.OnCall = (ko) => returnValues.Dequeue();
#endregion

#region knockoff-vs-moq-per-test-override-usage
// [Fact]
// public void Test_WithSpecialCase()
// {
//     var knockOff = new VsOverrideServiceKnockOff();
//
//     // Override just for this test
//     knockOff.GetUser.OnCall = (ko, id) => new VsUser { Id = id, Name = "Special" };
//
//     var user = knockOff.AsIVsOverrideService().GetUser(42);
//     Assert.Equal("Special", user.Name);
// }
#endregion

#region knockoff-vs-moq-reset-and-reuse
// var knockOff = new UserServiceKnockOff();
// IUserService service = knockOff;
//
// knockOff.GetUser.OnCall = (ko, id) => new User { Name = "First" };
// var user1 = service.GetUser(1);
//
// knockOff.GetUser.Reset(); // Clears callback and tracking
//
// // Now falls back to user method or default
// var user2 = service.GetUser(2);
// Assert.Equal(0, knockOff.GetUser.CallCount); // Reset cleared count
#endregion
