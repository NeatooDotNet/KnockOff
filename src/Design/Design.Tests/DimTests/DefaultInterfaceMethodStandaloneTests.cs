// -----------------------------------------------------------------------------
// Design.Tests - Default Interface Method (DIM) Standalone Tests
// -----------------------------------------------------------------------------
// These tests verify that unconfigured DIM members execute the DIM on
// standalone (flat) stubs, not just inline stubs.
//
// BUG: All tests currently FAIL because KnockOff returns default(T) for
// unconfigured DIM members. See Gap #12.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.DefaultMethods;

namespace Design.Tests.DimTests;

public class DefaultInterfaceMethodStandaloneTests
{
    // =========================================================================
    // Method DIM - Standalone
    // =========================================================================

    [Fact]
    public void Standalone_UnconfiguredMethodDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultMethodPolygonStub();
        stub.SideLength.Get(3);
        stub.NumberOfSides.Get(5);

        IDefaultMethodPolygon polygon = stub;

        // DIM: double GetPerimeter() => this.SideLength * this.NumberOfSides;
        // Expected: 15.0 (3 * 5)
        Assert.Equal(15.0, polygon.GetPerimeter());
    }

    [Fact]
    public void Standalone_ConfiguredMethodDim_ShouldUseConfiguredValue()
    {
        var stub = new DefaultMethodPolygonStub();
        stub.SideLength.Get(3);
        stub.NumberOfSides.Get(5);
        stub.GetPerimeter.Return(() => 42.0);

        IDefaultMethodPolygon polygon = stub;

        // Configured value should take priority over DIM
        Assert.Equal(42.0, polygon.GetPerimeter());
    }

    // =========================================================================
    // Property DIM - Standalone
    // =========================================================================

    [Fact]
    public void Standalone_UnconfiguredPropertyDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultPropertyPolygonStub();
        stub.SideLength.Get(3);
        stub.NumberOfSides.Get(5);

        IDefaultPropertyPolygon polygon = stub;

        // DIM: double Perimeter => this.SideLength * this.NumberOfSides;
        // Expected: 15.0 (3 * 5)
        Assert.Equal(15.0, polygon.Perimeter);
    }

    // =========================================================================
    // Indexer DIM - Standalone
    // =========================================================================

    [Fact]
    public void Standalone_UnconfiguredIndexerDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultIndexerPolygonStub();
        stub.SideLength.Get(3);

        IDefaultIndexerPolygon polygon = stub;

        // DIM: double this[int numberOfSides] => this.SideLength * numberOfSides;
        // Expected: 15.0 (3 * 5)
        Assert.Equal(15.0, polygon[5]);
    }
}
