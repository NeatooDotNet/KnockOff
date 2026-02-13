// -----------------------------------------------------------------------------
// Design.Tests - Default Interface Method (DIM) Tests
// -----------------------------------------------------------------------------
// These tests verify that unconfigured interface members with default
// implementations (DIMs) execute the DIM rather than returning default(T).
//
// BUG: All tests currently FAIL because KnockOff returns default(T) for
// unconfigured DIM members. See Gap #12.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.DefaultMethods;

namespace Design.Tests.DimTests;

public class DefaultInterfaceMethodTests
{
    // =========================================================================
    // Method DIM
    // =========================================================================

    [Fact]
    public void UnconfiguredMethodDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultMethodDemo.Stubs.IDefaultMethodPolygon();
        stub.SideLength.Get(3);
        stub.NumberOfSides.Get(5);

        IDefaultMethodPolygon polygon = stub;

        // DIM: double GetPerimeter() => this.SideLength * this.NumberOfSides;
        // Expected: 15.0 (3 * 5)
        // Actual (bug): 0.0 (default(double))
        Assert.Equal(15.0, polygon.GetPerimeter());
    }

    // =========================================================================
    // Property DIM
    // =========================================================================

    [Fact]
    public void UnconfiguredPropertyDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultMethodDemo.Stubs.IDefaultPropertyPolygon();
        stub.SideLength.Get(3);
        stub.NumberOfSides.Get(5);

        IDefaultPropertyPolygon polygon = stub;

        // DIM: double Perimeter => this.SideLength * this.NumberOfSides;
        // Expected: 15.0 (3 * 5)
        // Actual (bug): 0.0 (default(double))
        Assert.Equal(15.0, polygon.Perimeter);
    }

    // =========================================================================
    // Indexer DIM
    // =========================================================================

    [Fact]
    public void UnconfiguredIndexerDim_ShouldExecuteDefaultImplementation()
    {
        var stub = new DefaultMethodDemo.Stubs.IDefaultIndexerPolygon();
        stub.SideLength.Get(3);

        IDefaultIndexerPolygon polygon = stub;

        // DIM: double this[int numberOfSides] => this.SideLength * numberOfSides;
        // Expected: 15.0 (3 * 5)
        // Actual (bug): 0.0 (default(double))
        Assert.Equal(15.0, polygon[5]);
    }
}
