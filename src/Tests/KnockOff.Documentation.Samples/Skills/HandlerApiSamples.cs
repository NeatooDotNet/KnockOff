/// <summary>
/// Code samples for ~/.claude/skills/knockoff/interceptor-api.md
///
/// Snippets in this file:
/// - skill:interceptor-api:method-interceptor-example
/// - skill:interceptor-api:property-interceptor-example
/// - skill:interceptor-api:indexer-interceptor-example
/// - skill:interceptor-api:event-interceptor-example
/// - skill:interceptor-api:overload-interceptor-example
/// - skill:interceptor-api:out-param-callback
/// - skill:interceptor-api:ref-param-callback
/// - skill:interceptor-api:ref-param-tracking
/// - skill:interceptor-api:async-interceptor-example
/// - skill:interceptor-api:generic-interceptor-example
/// - skill:interceptor-api:smart-defaults-example
///
/// Corresponding tests: HandlerApiSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Handler API Samples
// ============================================================================

public class HaUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class HaEntity
{
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Method Handler Example
// ============================================================================

public interface IHaService
{
    void Initialize();
    HaUser GetById(int id);
    HaEntity Create(string name, int value);
}

#region skill-interceptor-api-method-interceptor-example
[KnockOff]
public partial class HaServiceKnockOff : IHaService { }
#endregion

// ============================================================================
// Property Handler Example
// ============================================================================

public interface IHaPropertyService
{
    string Name { get; set; }
}

#region skill-interceptor-api-property-interceptor-example
[KnockOff]
public partial class HaPropertyServiceKnockOff : IHaPropertyService { }
#endregion

// ============================================================================
// Indexer Handler Example
// ============================================================================

public interface IHaPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill-interceptor-api-indexer-interceptor-example
[KnockOff]
public partial class HaPropertyStoreKnockOff : IHaPropertyStore { }
#endregion

// ============================================================================
// Event Handler Example
// ============================================================================

public interface IHaEventSource
{
    event EventHandler<string> DataReceived;
    event EventHandler Completed;
    event Action<int> ProgressChanged;
    event Action<string, int> DataUpdated;
}

#region skill-interceptor-api-event-interceptor-example
[KnockOff]
public partial class HaEventSourceKnockOff : IHaEventSource { }
#endregion

// ============================================================================
// Overload Handler Example
// ============================================================================

public interface IHaOverloadService
{
    void Process(string data);
    void Process(string data, int priority);
    int Calculate(int value);
    int Calculate(int a, int b);
}

#region skill-interceptor-api-overload-interceptor-example
[KnockOff]
public partial class HaOverloadServiceKnockOff : IHaOverloadService { }
#endregion

// ============================================================================
// Out Parameter Callback
// ============================================================================

public interface IHaParser
{
    bool TryParse(string input, out int result);
    void GetData(out string name, out int count);
}

#region skill-interceptor-api-out-param-callback
[KnockOff]
public partial class HaParserKnockOff : IHaParser { }
#endregion

// ============================================================================
// Ref Parameter Callback
// ============================================================================

public interface IHaProcessor
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
}

#region skill-interceptor-api-ref-param-callback
[KnockOff]
public partial class HaProcessorKnockOff : IHaProcessor { }
#endregion

// skill:interceptor-api:ref-param-tracking - kept inline in docs (illustrative only)

// ============================================================================
// Async Handler Example
// ============================================================================

public interface IHaAsyncRepository
{
    Task<HaUser?> GetByIdAsync(int id);
    Task<int> SaveAsync(object entity);
}

#region skill-interceptor-api-async-interceptor-example
[KnockOff]
public partial class HaAsyncRepositoryKnockOff : IHaAsyncRepository { }
#endregion

// ============================================================================
// Generic Handler Example
// ============================================================================

public interface IHaSerializer
{
    T Deserialize<T>(string json);
    TOut Convert<TIn, TOut>(TIn input);
}

#region skill-interceptor-api-generic-interceptor-example
[KnockOff]
public partial class HaSerializerKnockOff : IHaSerializer { }
#endregion

// ============================================================================
// Smart Defaults Example
// ============================================================================

public interface IHaDefaultsService
{
    int GetCount();
    List<string> GetItems();
    IList<string> GetIList();
    string? GetOptional();
    IDisposable GetDisposable();
}

#region skill-interceptor-api-smart-defaults-example
[KnockOff]
public partial class HaDefaultsServiceKnockOff : IHaDefaultsService { }
#endregion
