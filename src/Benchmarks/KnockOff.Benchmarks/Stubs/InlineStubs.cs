using System.Diagnostics.CodeAnalysis;
using KnockOff.Benchmarks.Interfaces;

namespace KnockOff.Benchmarks.Stubs;

/// <summary>
/// Container for inline stubs using the [KnockOff&lt;T&gt;] pattern.
/// </summary>
[KnockOff<ISimpleService>]
[KnockOff<ICalculator>]
[KnockOff<IPropertyService>]
[SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "Partial class generates nested types")]
public partial class InlineStubs
{
}
