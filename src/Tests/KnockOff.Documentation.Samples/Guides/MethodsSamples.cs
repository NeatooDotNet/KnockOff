/// <summary>
/// Code samples for docs/guides/methods.md
///
/// Snippets in this file:
/// - docs:methods:void-no-params
/// - docs:methods:void-with-params
/// - docs:methods:return-value
/// - docs:methods:single-param
/// - docs:methods:multiple-params
/// - docs:methods:user-defined
/// - docs:methods:void-callbacks
/// - docs:methods:return-callbacks
/// - docs:methods:priority-order
/// - docs:methods:simulating-failures
/// - docs:methods:conditional-returns
/// - docs:methods:capturing-arguments
/// - docs:methods:verifying-call-order
/// - docs:methods:sequential-returns
/// - docs:methods:accessing-spy-state
///
/// Corresponding tests: MethodsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class MethodUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MethodEntity
{
    public int Id { get; set; }
}

// ============================================================================
// Void Methods (No Parameters)
// ============================================================================

#region docs:methods:void-no-params
public interface IMethodService
{
    void Initialize();
}

[KnockOff]
public partial class MethodServiceKnockOff : IMethodService { }
#endregion

// ============================================================================
// Void Methods (With Parameters)
// ============================================================================

#region docs:methods:void-with-params
public interface IMethodLogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}

[KnockOff]
public partial class MethodLoggerKnockOff : IMethodLogger { }
#endregion

// ============================================================================
// Methods with Return Values
// ============================================================================

#region docs:methods:return-value
public interface IMethodRepository
{
    MethodUser? GetById(int id);
    int Count();
}

[KnockOff]
public partial class MethodRepositoryKnockOff : IMethodRepository { }
#endregion

// ============================================================================
// Single Parameter
// ============================================================================

public interface IMethodSingleParam
{
    MethodUser GetUser(int id);
}

#region docs:methods:single-param
[KnockOff]
public partial class MethodSingleParamKnockOff : IMethodSingleParam { }
#endregion

// ============================================================================
// Multiple Parameters
// ============================================================================

public interface IMethodMultiParam
{
    void Process(string name, int value, bool flag);
}

#region docs:methods:multiple-params
[KnockOff]
public partial class MethodMultiParamKnockOff : IMethodMultiParam { }
#endregion

// ============================================================================
// User-Defined Methods
// ============================================================================

public interface IMethodUserDefined
{
    MethodUser? GetById(int id);
    int Count();
}

#region docs:methods:user-defined
[KnockOff]
public partial class MethodUserDefinedKnockOff : IMethodUserDefined
{
    protected MethodUser? GetById(int id) => new MethodUser { Id = id, Name = "Default" };

    protected int Count() => 100;
}
#endregion

// ============================================================================
// Priority Order
// ============================================================================

public interface IMethodPriority
{
    int Calculate(int x);
}

#region docs:methods:priority-order
[KnockOff]
public partial class MethodPriorityKnockOff : IMethodPriority
{
    protected int Calculate(int x) => x * 2;  // User method
}
#endregion

// ============================================================================
// Simulating Failures
// ============================================================================

public interface IMethodFailure
{
    void Save(MethodEntity entity);
}

#region docs:methods:simulating-failures
[KnockOff]
public partial class MethodFailureKnockOff : IMethodFailure { }
#endregion

// ============================================================================
// Verifying Call Order
// ============================================================================

public interface IMethodCallOrder
{
    void Initialize();
    void Process();
    void Cleanup();
}

#region docs:methods:verifying-call-order
[KnockOff]
public partial class MethodCallOrderKnockOff : IMethodCallOrder { }
#endregion

// ============================================================================
// Sequential Returns
// ============================================================================

public interface IMethodSequential
{
    int GetNext();
}

#region docs:methods:sequential-returns
[KnockOff]
public partial class MethodSequentialKnockOff : IMethodSequential { }
#endregion

// ============================================================================
// Accessing Spy State
// ============================================================================

public interface IMethodSpyState
{
    void Initialize();
    void Process();
}

#region docs:methods:accessing-spy-state
[KnockOff]
public partial class MethodSpyStateKnockOff : IMethodSpyState { }
#endregion
