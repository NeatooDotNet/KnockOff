namespace KnockOff.Tests;

/// <summary>
/// Tests for Default Interface Method (DIM) support.
/// When an interface member has a default implementation and the stub member
/// is unconfigured, the DIM should execute rather than returning default(T).
/// </summary>
[KnockOff<IDefaultMethodPolygon>]
[KnockOff<IDefaultPropertyPolygon>]
[KnockOff<IDefaultIndexerPolygon>]
public partial class DefaultInterfaceMethodTests
{
	// =========================================================================
	// Inline Tests
	// =========================================================================

	[Fact]
	public void Inline_UnconfiguredMethodDim_ExecutesDefaultImplementation()
	{
		var stub = new Stubs.IDefaultMethodPolygon();
		stub.SideLength.Get(3);
		stub.NumberOfSides.Get(5);

		IDefaultMethodPolygon polygon = stub;

		// DIM: double GetPerimeter() => this.SideLength * this.NumberOfSides;
		Assert.Equal(15.0, polygon.GetPerimeter());
	}

	[Fact]
	public void Inline_UnconfiguredPropertyDim_ExecutesDefaultImplementation()
	{
		var stub = new Stubs.IDefaultPropertyPolygon();
		stub.SideLength.Get(3);
		stub.NumberOfSides.Get(5);

		IDefaultPropertyPolygon polygon = stub;

		// DIM: double Perimeter => this.SideLength * this.NumberOfSides;
		Assert.Equal(15.0, polygon.Perimeter);
	}

	[Fact]
	public void Inline_UnconfiguredIndexerDim_ExecutesDefaultImplementation()
	{
		var stub = new Stubs.IDefaultIndexerPolygon();
		stub.SideLength.Get(3);

		IDefaultIndexerPolygon polygon = stub;

		// DIM: double this[int numberOfSides] => this.SideLength * numberOfSides;
		Assert.Equal(15.0, polygon[5]);
	}

	// =========================================================================
	// Standalone Tests
	// =========================================================================

	[Fact]
	public void Standalone_UnconfiguredMethodDim_ExecutesDefaultImplementation()
	{
		var stub = new DefaultMethodPolygonKnockOff();
		stub.SideLength.Get(3);
		stub.NumberOfSides.Get(5);

		IDefaultMethodPolygon polygon = stub;

		Assert.Equal(15.0, polygon.GetPerimeter());
	}

	[Fact]
	public void Standalone_ConfiguredMethodDim_UsesConfiguredValue()
	{
		var stub = new DefaultMethodPolygonKnockOff();
		stub.SideLength.Get(3);
		stub.NumberOfSides.Get(5);
		stub.GetPerimeter.Return(() => 42.0);

		IDefaultMethodPolygon polygon = stub;

		// Configured value should take priority over DIM
		Assert.Equal(42.0, polygon.GetPerimeter());
	}

	[Fact]
	public void Standalone_UnconfiguredPropertyDim_ExecutesDefaultImplementation()
	{
		var stub = new DefaultPropertyPolygonKnockOff();
		stub.SideLength.Get(3);
		stub.NumberOfSides.Get(5);

		IDefaultPropertyPolygon polygon = stub;

		Assert.Equal(15.0, polygon.Perimeter);
	}

	[Fact]
	public void Standalone_UnconfiguredIndexerDim_ExecutesDefaultImplementation()
	{
		var stub = new DefaultIndexerPolygonKnockOff();
		stub.SideLength.Get(3);

		IDefaultIndexerPolygon polygon = stub;

		Assert.Equal(15.0, polygon[5]);
	}
}
