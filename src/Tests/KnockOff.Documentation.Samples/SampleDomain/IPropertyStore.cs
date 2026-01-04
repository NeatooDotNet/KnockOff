namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Property store interface used in indexer examples.
/// </summary>
public interface IPropertyStore
{
    PropertyInfo? this[string key] { get; set; }
    int Count { get; }
}

/// <summary>
/// Read-only property store for get-only indexer examples.
/// </summary>
public interface IReadOnlyPropertyStore
{
    PropertyInfo? this[string key] { get; }
}
