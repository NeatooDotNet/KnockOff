using System.Diagnostics.CodeAnalysis;

namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with various property types.
/// Used to measure property get/set overhead.
/// </summary>
[SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "Testing write-only property support")]
public interface IPropertyService
{
    string Name { get; set; }
    int ReadOnlyValue { get; }
    int WriteOnlyValue { set; }
}
