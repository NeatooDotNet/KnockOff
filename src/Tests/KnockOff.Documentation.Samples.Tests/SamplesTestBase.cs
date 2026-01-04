namespace KnockOff.Documentation.Samples.Tests;

/// <summary>
/// Base class for documentation sample tests.
/// Provides test categorization for filtering.
/// </summary>
[Trait("Category", "Documentation")]
public abstract class SamplesTestBase
{
    // No DI infrastructure needed for KnockOff - stubs are instantiated directly.
    // This base class provides consistent test categorization.
}
