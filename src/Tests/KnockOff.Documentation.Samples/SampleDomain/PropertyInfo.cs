namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Property info used in indexer examples.
/// </summary>
public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;
    public object? Value { get; set; }
    public Type? Type { get; set; }
}
