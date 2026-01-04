namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Data service interface used in README quick example.
/// </summary>
public interface IDataService
{
    string Name { get; set; }
    string? GetDescription(int id);
    int GetCount();
}
