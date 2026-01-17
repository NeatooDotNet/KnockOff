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
/// - docs:methods:priority-order-usage
/// - docs:methods:simulating-failures
/// - docs:methods:simulating-failures-usage
/// - docs:methods:conditional-returns
/// - docs:methods:capturing-arguments
/// - docs:methods:verifying-call-order
/// - docs:methods:verifying-call-order-usage
/// - docs:methods:sequential-returns
/// - docs:methods:sequential-returns-usage
/// - docs:methods:accessing-handler-state
/// - docs:methods:accessing-handler-state-usage
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

#region methods-void-no-params
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

#region methods-void-with-params
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

#region methods-return-value
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

[KnockOff]
public partial class MethodSingleParamKnockOff : IMethodSingleParam { }

// ============================================================================
// Multiple Parameters
// ============================================================================

public interface IMethodMultiParam
{
    void Process(string name, int value, bool flag);
}

[KnockOff]
public partial class MethodMultiParamKnockOff : IMethodMultiParam { }

// ============================================================================
// User-Defined Methods
// ============================================================================

public interface IMethodUserDefined
{
    MethodUser? GetById(int id);
    int Count();
}

#region methods-user-defined
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

#region methods-priority-order
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

#region methods-simulating-failures
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

#region methods-verifying-call-order
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

#region methods-sequential-returns
[KnockOff]
public partial class MethodSequentialKnockOff : IMethodSequential { }
#endregion

// ============================================================================
// Accessing Handler State
// ============================================================================

public interface IMethodHandlerState
{
    void Initialize();
    void Process();
}

#region methods-accessing-handler-state
[KnockOff]
public partial class MethodHandlerStateKnockOff : IMethodHandlerState { }
#endregion

// ============================================================================
// Usage Examples (Compilable methods with snippet regions)
// ============================================================================

/// <summary>
/// Usage examples demonstrating method patterns.
/// Each method is a compilable example; snippets extract the key portions.
/// </summary>
public static class MethodsUsageExamples
{
    public static void SingleParamTracking()
    {
        var knockOff = new MethodSingleParamKnockOff();
        IMethodSingleParam service = knockOff;

        #region methods-single-param
        // Set up callback to get tracking
        var tracking = knockOff.GetUser.OnCall((ko, id) => new MethodUser { Id = id });

        service.GetUser(42);

        // Tracking - single parameter uses raw type (not a tuple)
        int? lastId = tracking.LastArg;  // 42, not (42,)
        #endregion

        _ = lastId; // Use variable
    }

    public static void MultipleParamsTracking()
    {
        var knockOff = new MethodMultiParamKnockOff();
        IMethodMultiParam service = knockOff;

        #region methods-multiple-params
        // Set up callback to get tracking
        var tracking = knockOff.Process.OnCall((ko, name, value, flag) => { });

        service.Process("test", 42, true);

        // Tracking - named tuple with original parameter names
        var args = tracking.LastArgs;
        var name2 = args.name;   // "test"
        var value2 = args.value; // 42
        var flag2 = args.flag;   // true
        #endregion

        _ = (name2, value2, flag2); // Use variables
    }

    public static void VoidCallbacks()
    {
        var serviceKnockOff = new MethodServiceKnockOff();
        var loggerKnockOff = new MethodLoggerKnockOff();

        #region methods-void-callbacks
        // No parameters
        serviceKnockOff.Initialize.OnCall((ko) =>
        {
            // Custom initialization logic
        });

        // Single parameter
        loggerKnockOff.Log.OnCall((ko, message) =>
        {
            Console.WriteLine($"Logged: {message}");
        });

        // Multiple parameters
        loggerKnockOff.LogError.OnCall((ko, message, ex) =>
        {
            Console.WriteLine($"Error: {message} - {ex.Message}");
        });
        #endregion
    }

    public static void ReturnCallbacks()
    {
        var knockOff = new MethodRepositoryKnockOff();

        #region methods-return-callbacks
        // No parameters
        knockOff.Count.OnCall((ko) => 42);

        // Single parameter
        knockOff.GetById.OnCall((ko, id) => new MethodUser { Id = id });
        #endregion
    }

    public static void PriorityOrder()
    {
        var knockOff = new MethodPriorityKnockOff();
        IMethodPriority service = knockOff;

        #region methods-priority-order-usage
        // User method provides implementation
        var result1 = service.Calculate(5);  // 10 (5 * 2)
        var result2 = service.Calculate(10); // 20 (10 * 2)

        // Interceptor with "2" suffix tracks calls (no OnCall - user method is implementation)
        Assert.Equal(2, knockOff.Calculate2.CallCount);
        Assert.True(knockOff.Calculate2.WasCalled);
        Assert.Equal(10, knockOff.Calculate2.LastArg);

        // Reset clears tracking
        knockOff.Calculate2.Reset();
        Assert.Equal(0, knockOff.Calculate2.CallCount);
        #endregion

        _ = (result1, result2); // Use variables
    }

    public static void SimulatingFailures()
    {
        var knockOff = new MethodFailureKnockOff();

        #region methods-simulating-failures-usage
        knockOff.Save.OnCall((ko, entity) =>
        {
            throw new InvalidOperationException("Connection failed");
        });
        #endregion
    }

    public static void ConditionalReturns()
    {
        var knockOff = new MethodRepositoryKnockOff();

        #region methods-conditional-returns
        knockOff.GetById.OnCall((ko, id) => id switch
        {
            1 => new MethodUser { Id = 1, Name = "Admin" },
            2 => new MethodUser { Id = 2, Name = "Guest" },
            _ => null
        });
        #endregion
    }

    public static void CapturingArguments()
    {
        var knockOff = new MethodRepositoryKnockOff();

        #region methods-capturing-arguments
        var capturedIds = new List<int>();
        knockOff.GetById.OnCall((ko, id) =>
        {
            capturedIds.Add(id);
            return new MethodUser { Id = id };
        });
        #endregion
    }

    public static void VerifyingCallOrder()
    {
        var knockOff = new MethodCallOrderKnockOff();
        IMethodCallOrder service = knockOff;

        #region methods-verifying-call-order-usage
        var callOrder = new List<string>();

        knockOff.Initialize.OnCall((ko) => callOrder.Add("Initialize"));
        knockOff.Process.OnCall((ko) => callOrder.Add("Process"));
        knockOff.Cleanup.OnCall((ko) => callOrder.Add("Cleanup"));

        service.Initialize();
        service.Process();
        service.Cleanup();

        // callOrder is ["Initialize", "Process", "Cleanup"]
        knockOff.VerifyAll();  // Verifies all configured callbacks were called
        #endregion

        _ = callOrder; // Use variable
    }

    public static void SequentialReturns()
    {
        var knockOff = new MethodSequentialKnockOff();
        IMethodSequential service = knockOff;

        #region methods-sequential-returns-usage
        var results = new Queue<int>([1, 2, 3]);
        knockOff.GetNext.OnCall((ko) => results.Dequeue());

        var first = service.GetNext();   // 1
        var second = service.GetNext();  // 2
        var third = service.GetNext();   // 3
        #endregion

        _ = (first, second, third); // Use variables
    }

    public static void AccessingHandlerState()
    {
        var knockOff = new MethodHandlerStateKnockOff();

        #region methods-accessing-handler-state-usage
        // Set up Initialize to get its tracking
        var initTracking = knockOff.Initialize.OnCall((ko) => { });

        knockOff.Process.OnCall((ko) =>
        {
            // Use tracked state from another method
            if (!initTracking.WasCalled)
                throw new InvalidOperationException("Not initialized");
        });
        #endregion
    }
}

// Minimal Assert class for compilation (tests use xUnit)
file static class Assert
{
    public static void True(bool condition) { }
    public static void Equal<T>(T expected, T actual) { }
}
