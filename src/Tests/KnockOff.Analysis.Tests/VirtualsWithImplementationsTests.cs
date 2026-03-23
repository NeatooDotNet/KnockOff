// ============================================================================
// VirtualsWithImplementationsTests: Edge case tests for default interface
// members (C# 8+) and virtual class members with base implementations.
// Inspired by Rocks.Analysis.IntegrationTests.VirtualsWithImplementationsTests
//
// Tests three categories:
// 1. Methods with default/virtual implementations (GetPerimeter)
// 2. Properties with default/virtual implementations (Perimeter)
// 3. Indexers with default/virtual implementations (this[numberOfSides])
//
// Key behavioral question: When a member with a default/virtual implementation
// is NOT configured, but its dependencies ARE configured, does calling the
// member invoke the default/virtual implementation using configured values?
//
// INTERFACE STUBS (Patterns 1, 5): The generator creates explicit implementations
// for ALL interface members, including those with default implementations. The
// default implementation is NOT invoked — unconfigured members return default(T).
//
// CLASS STUBS (Patterns 3, 6): Virtual methods fall through to base.Method()
// when unconfigured. Since base.GetPerimeter() calls this.SideLength (which is
// overridden to use the interceptor), the configured dependency values flow
// through the base implementation. So unconfigured GetPerimeter() returns 15
// when SideLength=3 and NumberOfSides=5.
//
// Each edge case is tested with:
// - Pattern 1 (Standalone) and Pattern 5 (Inline) for interfaces
// - Pattern 3 (Standalone Class) and Pattern 6 (Inline Class) for classes
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.VirtualsWithImplementationsTestTypes
{
	// ========================================================================
	// Edge Case 1: Interface with default method implementation
	// ========================================================================

	public interface IMethodPolygon
	{
		int NumberOfSides { get; }
		int SideLength { get; }

		double GetPerimeter() => this.SideLength * this.NumberOfSides;
	}

	// ========================================================================
	// Edge Case 2: Interface with default property implementation
	// ========================================================================

	public interface IPropertyPolygon
	{
		int NumberOfSides { get; }
		int SideLength { get; }

		double Perimeter => this.SideLength * this.NumberOfSides;
	}

	// ========================================================================
	// Edge Case 3: Interface with default indexer implementation
	// ========================================================================

	public interface IIndexerPolygon
	{
		int SideLength { get; }

		double this[int numberOfSides] => this.SideLength * numberOfSides;
	}

	// ========================================================================
	// Class equivalents with virtual implementations
	// ========================================================================

	public class MethodPolygon
	{
		public virtual int NumberOfSides { get; }
		public virtual int SideLength { get; }

		public virtual double GetPerimeter() => this.SideLength * this.NumberOfSides;
	}

	public class PropertyPolygon
	{
		public virtual int NumberOfSides { get; }
		public virtual int SideLength { get; }

		public virtual double Perimeter => this.SideLength * this.NumberOfSides;
	}

	public class IndexerPolygon
	{
		public virtual int SideLength { get; }

		public virtual double this[int numberOfSides] => this.SideLength * numberOfSides;
	}

	// ========================================================================
	// Pattern 1: Standalone stubs (interfaces)
	// ========================================================================

	[KnockOff]
	public partial class MethodPolygonStandaloneKnockOff : IMethodPolygon
	{
	}

	[KnockOff]
	public partial class PropertyPolygonStandaloneKnockOff : IPropertyPolygon
	{
	}

	[KnockOff]
	public partial class IndexerPolygonStandaloneKnockOff : IIndexerPolygon
	{
	}

	// ========================================================================
	// Pattern 3: Standalone class stubs
	// ========================================================================

	[KnockOffBase<MethodPolygon>]
	public partial class MethodPolygonClassStandaloneKnockOff
	{
	}

	[KnockOffBase<PropertyPolygon>]
	public partial class PropertyPolygonClassStandaloneKnockOff
	{
	}

	[KnockOffBase<IndexerPolygon>]
	public partial class IndexerPolygonClassStandaloneKnockOff
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.VirtualsWithImplementationsTestTypes;

	// Pattern 5: Inline interface stubs
	// Pattern 6: Inline class stubs
	[KnockOff<IMethodPolygon>]
	[KnockOff<IPropertyPolygon>]
	[KnockOff<IIndexerPolygon>]
	[KnockOff<MethodPolygon>]
	[KnockOff<PropertyPolygon>]
	[KnockOff<IndexerPolygon>]
	public partial class VirtualsInlineTests
	{
	}

	public class VirtualsWithImplementationsTests
	{
		// ====================================================================
		// Edge Case 1: Method with default/virtual implementation
		// ====================================================================

		// ----------------------------------------------------------------
		// INTERFACE: Default method implementations are NOT invoked.
		// The generator creates explicit implementations for ALL members,
		// so the default impl body is never reached.
		// ----------------------------------------------------------------

		#region Standalone Interface: GetPerimeter (default method)

		[Fact]
		public void DefaultMethod_Standalone_UnconfiguredReturnsDefault()
		{
			// Default interface methods are replaced by interceptor-generated
			// implementations, so an unconfigured GetPerimeter returns 0.0
			var stub = new MethodPolygonStandaloneKnockOff();
			IMethodPolygon service = stub;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			// The default implementation (this.SideLength * this.NumberOfSides)
			// is NOT invoked — the interceptor returns default(double) = 0.0
			double result = service.GetPerimeter();

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultMethod_Standalone_ConfiguredReturnsConfiguredValue()
		{
			var stub = new MethodPolygonStandaloneKnockOff();
			IMethodPolygon service = stub;

			stub.GetPerimeter.Return(42.0);

			double result = service.GetPerimeter();

			Assert.Equal(42.0, result);
		}

		[Fact]
		public void DefaultMethod_Standalone_VerifyTracksUnconfiguredCall()
		{
			var stub = new MethodPolygonStandaloneKnockOff();
			IMethodPolygon service = stub;

			service.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Once);
		}

		[Fact]
		public void DefaultMethod_Standalone_VerifyTracksConfiguredCall()
		{
			var stub = new MethodPolygonStandaloneKnockOff();
			IMethodPolygon service = stub;

			stub.GetPerimeter.Return(10.0);
			service.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Once);
		}

		#endregion

		#region Inline Interface: GetPerimeter (default method)

		[Fact]
		public void DefaultMethod_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new VirtualsInlineTests.Stubs.IMethodPolygon();
			IMethodPolygon service = stub;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			double result = service.GetPerimeter();

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultMethod_Inline_ConfiguredReturnsConfiguredValue()
		{
			var stub = new VirtualsInlineTests.Stubs.IMethodPolygon();
			IMethodPolygon service = stub;

			stub.GetPerimeter.Return(42.0);

			double result = service.GetPerimeter();

			Assert.Equal(42.0, result);
		}

		[Fact]
		public void DefaultMethod_Inline_VerifyTracksCall()
		{
			var stub = new VirtualsInlineTests.Stubs.IMethodPolygon();
			IMethodPolygon service = stub;

			service.GetPerimeter();
			service.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Exactly(2));
		}

		#endregion

		// ----------------------------------------------------------------
		// CLASS: Virtual method implementations ARE invoked when unconfigured.
		// base.GetPerimeter() calls this.SideLength (overridden by interceptor),
		// so configured dependency values flow through the base implementation.
		// ----------------------------------------------------------------

		#region Standalone Class: GetPerimeter (virtual method)

		[Fact]
		public void VirtualMethod_StandaloneClass_UnconfiguredUsesBaseImpl()
		{
			// Class stubs fall through to base.GetPerimeter() when unconfigured.
			// base.GetPerimeter() calls this.SideLength and this.NumberOfSides,
			// which ARE overridden by the interceptor, so configured values
			// flow through: 3 * 5 = 15
			var stub = new MethodPolygonClassStandaloneKnockOff();
			MethodPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			double result = obj.GetPerimeter();

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualMethod_StandaloneClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new MethodPolygonClassStandaloneKnockOff();
			MethodPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);
			stub.GetPerimeter.Return(99.0);

			double result = obj.GetPerimeter();

			// Configured value takes precedence over base implementation
			Assert.Equal(99.0, result);
		}

		[Fact]
		public void VirtualMethod_StandaloneClass_VerifyTracksUnconfiguredCall()
		{
			var stub = new MethodPolygonClassStandaloneKnockOff();
			MethodPolygon obj = stub.Object;

			obj.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Once);
		}

		[Fact]
		public void VirtualMethod_StandaloneClass_VerifyTracksConfiguredCall()
		{
			var stub = new MethodPolygonClassStandaloneKnockOff();
			MethodPolygon obj = stub.Object;

			stub.GetPerimeter.Return(10.0);
			obj.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Once);
		}

		#endregion

		#region Inline Class: GetPerimeter (virtual method)

		[Fact]
		public void VirtualMethod_InlineClass_UnconfiguredUsesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.MethodPolygon();
			MethodPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			double result = obj.GetPerimeter();

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualMethod_InlineClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.MethodPolygon();
			MethodPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);
			stub.GetPerimeter.Return(99.0);

			double result = obj.GetPerimeter();

			Assert.Equal(99.0, result);
		}

		[Fact]
		public void VirtualMethod_InlineClass_VerifyTracksCall()
		{
			var stub = new VirtualsInlineTests.Stubs.MethodPolygon();
			MethodPolygon obj = stub.Object;

			obj.GetPerimeter();
			obj.GetPerimeter();

			stub.GetPerimeter.Verify(Called.Exactly(2));
		}

		#endregion

		// ====================================================================
		// Edge Case 2: Property with default/virtual implementation
		// ====================================================================

		#region Standalone Interface: Perimeter (default property)

		[Fact]
		public void DefaultProperty_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new PropertyPolygonStandaloneKnockOff();
			IPropertyPolygon service = stub;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			// Default property implementation is NOT invoked
			double result = service.Perimeter;

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultProperty_Standalone_ConfiguredReturnsConfiguredValue()
		{
			var stub = new PropertyPolygonStandaloneKnockOff();
			IPropertyPolygon service = stub;

			stub.Perimeter.Get(42.0);

			double result = service.Perimeter;

			Assert.Equal(42.0, result);
		}

		[Fact]
		public void DefaultProperty_Standalone_VerifyTracksAccess()
		{
			var stub = new PropertyPolygonStandaloneKnockOff();
			IPropertyPolygon service = stub;

			_ = service.Perimeter;
			_ = service.Perimeter;

			stub.Perimeter.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Interface: Perimeter (default property)

		[Fact]
		public void DefaultProperty_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new VirtualsInlineTests.Stubs.IPropertyPolygon();
			IPropertyPolygon service = stub;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			double result = service.Perimeter;

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultProperty_Inline_ConfiguredReturnsConfiguredValue()
		{
			var stub = new VirtualsInlineTests.Stubs.IPropertyPolygon();
			IPropertyPolygon service = stub;

			stub.Perimeter.Get(42.0);

			double result = service.Perimeter;

			Assert.Equal(42.0, result);
		}

		[Fact]
		public void DefaultProperty_Inline_VerifyTracksAccess()
		{
			var stub = new VirtualsInlineTests.Stubs.IPropertyPolygon();
			IPropertyPolygon service = stub;

			_ = service.Perimeter;

			stub.Perimeter.VerifyGet(Called.Once);
		}

		#endregion

		#region Standalone Class: Perimeter (virtual property)

		[Fact]
		public void VirtualProperty_StandaloneClass_UnconfiguredUsesBaseImpl()
		{
			var stub = new PropertyPolygonClassStandaloneKnockOff();
			PropertyPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			// base.Perimeter calls this.SideLength * this.NumberOfSides
			double result = obj.Perimeter;

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualProperty_StandaloneClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new PropertyPolygonClassStandaloneKnockOff();
			PropertyPolygon obj = stub.Object;

			stub.Perimeter.Get(99.0);

			double result = obj.Perimeter;

			Assert.Equal(99.0, result);
		}

		[Fact]
		public void VirtualProperty_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new PropertyPolygonClassStandaloneKnockOff();
			PropertyPolygon obj = stub.Object;

			_ = obj.Perimeter;
			_ = obj.Perimeter;

			stub.Perimeter.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Perimeter (virtual property)

		[Fact]
		public void VirtualProperty_InlineClass_UnconfiguredUsesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.PropertyPolygon();
			PropertyPolygon obj = stub.Object;

			stub.SideLength.Get(3);
			stub.NumberOfSides.Get(5);

			double result = obj.Perimeter;

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualProperty_InlineClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.PropertyPolygon();
			PropertyPolygon obj = stub.Object;

			stub.Perimeter.Get(99.0);

			double result = obj.Perimeter;

			Assert.Equal(99.0, result);
		}

		[Fact]
		public void VirtualProperty_InlineClass_VerifyTracksAccess()
		{
			var stub = new VirtualsInlineTests.Stubs.PropertyPolygon();
			PropertyPolygon obj = stub.Object;

			_ = obj.Perimeter;

			stub.Perimeter.VerifyGet(Called.Once);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: Indexer with default/virtual implementation
		// ====================================================================

		#region Standalone Interface: Indexer (default indexer)

		[Fact]
		public void DefaultIndexer_Standalone_UnconfiguredReturnsDefault()
		{
			var stub = new IndexerPolygonStandaloneKnockOff();
			IIndexerPolygon service = stub;

			stub.SideLength.Get(3);

			// Default indexer implementation is NOT invoked
			double result = service[5];

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultIndexer_Standalone_ConfiguredReturnsConfiguredValue()
		{
			var stub = new IndexerPolygonStandaloneKnockOff();
			IIndexerPolygon service = stub;

			stub.Indexer.Get((int numberOfSides) => numberOfSides * 10.0);

			double result = service[5];

			Assert.Equal(50.0, result);
		}

		[Fact]
		public void DefaultIndexer_Standalone_VerifyTracksAccess()
		{
			var stub = new IndexerPolygonStandaloneKnockOff();
			IIndexerPolygon service = stub;

			_ = service[3];
			_ = service[5];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Interface: Indexer (default indexer)

		[Fact]
		public void DefaultIndexer_Inline_UnconfiguredReturnsDefault()
		{
			var stub = new VirtualsInlineTests.Stubs.IIndexerPolygon();
			IIndexerPolygon service = stub;

			stub.SideLength.Get(3);

			double result = service[5];

			Assert.Equal(0.0, result);
		}

		[Fact]
		public void DefaultIndexer_Inline_ConfiguredReturnsConfiguredValue()
		{
			var stub = new VirtualsInlineTests.Stubs.IIndexerPolygon();
			IIndexerPolygon service = stub;

			stub.Indexer.Get((int numberOfSides) => numberOfSides * 10.0);

			double result = service[5];

			Assert.Equal(50.0, result);
		}

		[Fact]
		public void DefaultIndexer_Inline_VerifyTracksAccess()
		{
			var stub = new VirtualsInlineTests.Stubs.IIndexerPolygon();
			IIndexerPolygon service = stub;

			_ = service[3];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion

		#region Standalone Class: Indexer (virtual indexer)

		[Fact]
		public void VirtualIndexer_StandaloneClass_UnconfiguredUsesBaseImpl()
		{
			var stub = new IndexerPolygonClassStandaloneKnockOff();
			IndexerPolygon obj = stub.Object;

			stub.SideLength.Get(3);

			// base[5] calls this.SideLength * numberOfSides = 3 * 5 = 15
			double result = obj[5];

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualIndexer_StandaloneClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new IndexerPolygonClassStandaloneKnockOff();
			IndexerPolygon obj = stub.Object;

			stub.Indexer.Get((int numberOfSides) => numberOfSides * 100.0);

			double result = obj[5];

			Assert.Equal(500.0, result);
		}

		[Fact]
		public void VirtualIndexer_StandaloneClass_VerifyTracksAccess()
		{
			var stub = new IndexerPolygonClassStandaloneKnockOff();
			IndexerPolygon obj = stub.Object;

			_ = obj[3];
			_ = obj[5];

			stub.Indexer.VerifyGet(Called.Exactly(2));
		}

		#endregion

		#region Inline Class: Indexer (virtual indexer)

		[Fact]
		public void VirtualIndexer_InlineClass_UnconfiguredUsesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.IndexerPolygon();
			IndexerPolygon obj = stub.Object;

			stub.SideLength.Get(3);

			double result = obj[5];

			Assert.Equal(15.0, result);
		}

		[Fact]
		public void VirtualIndexer_InlineClass_ConfiguredOverridesBaseImpl()
		{
			var stub = new VirtualsInlineTests.Stubs.IndexerPolygon();
			IndexerPolygon obj = stub.Object;

			stub.Indexer.Get((int numberOfSides) => numberOfSides * 100.0);

			double result = obj[5];

			Assert.Equal(500.0, result);
		}

		[Fact]
		public void VirtualIndexer_InlineClass_VerifyTracksAccess()
		{
			var stub = new VirtualsInlineTests.Stubs.IndexerPolygon();
			IndexerPolygon obj = stub.Object;

			_ = obj[3];

			stub.Indexer.VerifyGet(Called.Once);
		}

		#endregion
	}
}
