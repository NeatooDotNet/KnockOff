// ============================================================================
// GenericTests: Edge case tests inspired by Rocks.Analysis.IntegrationTests
//
// Tests three categories of generic edge cases:
// 1. Generic interface inheritance with overloaded methods from different
//    type parameters (ISourceUpdater<string, Guid>)
// 2. Class with generic virtual methods returning generic containers
//    (GenericContainer)
// 3. Interface with multiple generic method overloads (IGenericContainer)
//
// Patterns tested:
// - Pattern 1 (Standalone Interface) for interfaces
// - Pattern 2 (Generic Standalone) for ISourceUpdater
// - Pattern 3 (Standalone Class) for classes
// - Pattern 5 (Inline Interface) for interfaces
// - Pattern 6 (Inline Class) for classes
// - Pattern 8 (Open Generic Interface) for ISourceUpdater
// ============================================================================

using KnockOff;

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

namespace KnockOff.Analysis.Tests.GenericTestTypes
{
	// ========================================================================
	// Edge Case 1: Generic interface inheritance with overloaded Refresh
	// from different type parameters
	// ========================================================================

	public interface ICacheUpdater<TObject, TKey>
		where TObject : notnull
		where TKey : notnull
	{
		void Refresh(IEnumerable<TKey> keys);
	}

	public interface ISourceUpdater<TObject, TKey>
		: ICacheUpdater<TObject, TKey>
		where TObject : notnull
		where TKey : notnull
	{
		void AddOrUpdate(TObject item);
		void Refresh(IEnumerable<TObject> items);
	}

	// Pattern 1: Standalone stub for ISourceUpdater<string, Guid>
	[KnockOff]
	public partial class SourceUpdaterStandaloneKnockOff : ISourceUpdater<string, Guid>
	{
	}

	// Pattern 2: Generic standalone stub for ISourceUpdater<TObject, TKey>
	[KnockOff]
	public partial class SourceUpdaterGenericStub<TObject, TKey> : ISourceUpdater<TObject, TKey>
		where TObject : notnull
		where TKey : notnull
	{
	}

	// ========================================================================
	// Edge Case 2: Class with generic virtual methods returning generic
	// containers and constrained generic returns
	// ========================================================================

	public class ReferenceTypeOne { }
	public class ReferenceTypeTwo { }

	public class ReferencedContainer<T> { }

	public class GenericContainer
	{
		public virtual ReferencedContainer<T> SetThings<T>() where T : class => new();
		public virtual TReturn Run<TReturn>() where TReturn : new() => new();
	}

	// Pattern 3: Standalone class stub for GenericContainer
	[KnockOffBase<GenericContainer>]
	public partial class GenericContainerStandaloneKnockOff
	{
	}

	// ========================================================================
	// Edge Case 3: Interface with multiple generic method overloads
	// ========================================================================

	public interface IGenericContainer
	{
		void Sprint<TReturn>();
		TReturn Run<TReturn>() where TReturn : new();
		TReturn Run<TInput, TReturn>(TInput input) where TReturn : new();
	}

	// Pattern 1: Standalone stub for IGenericContainer
	[KnockOff]
	public partial class GenericContainerInterfaceStandaloneKnockOff : IGenericContainer
	{
	}
}

// ============================================================================
// INLINE PATTERN DECLARATIONS + TESTS
// ============================================================================

namespace KnockOff.Analysis.Tests
{
	using KnockOff.Analysis.Tests.GenericTestTypes;

	// Inline stubs (Pattern 5 for interfaces, Pattern 6 for class)
	[KnockOff<ISourceUpdater<string, Guid>>]
	[KnockOff<IGenericContainer>]
	[KnockOff<GenericContainer>]
	public partial class GenericInlineTests
	{
	}

	// Pattern 8: Open Generic Interface for ISourceUpdater<,>
	[KnockOff(typeof(ISourceUpdater<,>))]
	public partial class GenericOpenInlineTests
	{
	}

	public class GenericTests
	{
		// ====================================================================
		// Edge Case 1: Generic interface inheritance — overloaded Refresh
		// ISourceUpdater<string, Guid> has:
		//   - Refresh(IEnumerable<Guid>) from ICacheUpdater
		//   - Refresh(IEnumerable<string>) from ISourceUpdater
		// ====================================================================

		#region Standalone: ISourceUpdater<string, Guid>

		[Fact]
		public void SourceUpdater_Standalone_AddOrUpdate_CanCall()
		{
			var knockOff = new SourceUpdaterStandaloneKnockOff();
			ISourceUpdater<string, Guid> service = knockOff;

			string? captured = null;
			knockOff.AddOrUpdate.Call((item) => captured = item);

			service.AddOrUpdate("test-item");

			Assert.Equal("test-item", captured);
		}

		[Fact]
		public void SourceUpdater_Standalone_BothRefreshOverloads_WorkIndependently()
		{
			var knockOff = new SourceUpdaterStandaloneKnockOff();
			ISourceUpdater<string, Guid> service = knockOff;

			// Configure both overloads
			List<string>? capturedStrings = null;
			List<Guid>? capturedGuids = null;

			knockOff.Refresh.Call((IEnumerable<string> items) =>
			{
				capturedStrings = items.ToList();
			});
			knockOff.Refresh.Call((IEnumerable<Guid> keys) =>
			{
				capturedGuids = keys.ToList();
			});

			// Call both overloads
			var testGuid = Guid.NewGuid();
			service.Refresh(new[] { "a", "b" });
			service.Refresh(new[] { testGuid });

			// Both should have been captured
			Assert.NotNull(capturedStrings);
			Assert.Equal(2, capturedStrings!.Count);
			Assert.Equal("a", capturedStrings[0]);

			Assert.NotNull(capturedGuids);
			Assert.Single(capturedGuids!);
			Assert.Equal(testGuid, capturedGuids[0]);
		}

		[Fact]
		public void SourceUpdater_Standalone_Refresh_VerifyPerOverload()
		{
			var knockOff = new SourceUpdaterStandaloneKnockOff();
			ISourceUpdater<string, Guid> service = knockOff;

			var stringTracking = knockOff.Refresh.Call((IEnumerable<string> items) => { });
			var guidTracking = knockOff.Refresh.Call((IEnumerable<Guid> keys) => { });

			service.Refresh(new[] { "a" });
			service.Refresh(new[] { Guid.NewGuid() });
			service.Refresh(new[] { Guid.NewGuid() });

			stringTracking.Verify(Called.Once);
			guidTracking.Verify(Called.Exactly(2));

			// Total across all overloads
			knockOff.Refresh.Verify(Called.Exactly(3));
		}

		#endregion

		#region Inline: ISourceUpdater<string, Guid>

		[Fact]
		public void SourceUpdater_Inline_BothRefreshOverloads_WorkIndependently()
		{
			var stub = new GenericInlineTests.Stubs.ISourceUpdater();
			ISourceUpdater<string, Guid> service = stub;

			List<string>? capturedStrings = null;
			List<Guid>? capturedGuids = null;

			stub.Refresh.Call((IEnumerable<string> items) =>
			{
				capturedStrings = items.ToList();
			});
			stub.Refresh.Call((IEnumerable<Guid> keys) =>
			{
				capturedGuids = keys.ToList();
			});

			var testGuid = Guid.NewGuid();
			service.Refresh(new[] { "x", "y" });
			service.Refresh(new[] { testGuid });

			Assert.NotNull(capturedStrings);
			Assert.Equal(2, capturedStrings!.Count);
			Assert.NotNull(capturedGuids);
			Assert.Single(capturedGuids!);
			Assert.Equal(testGuid, capturedGuids[0]);
		}

		#endregion

		// ====================================================================
		// Pattern 2: Generic Standalone — ISourceUpdater<TObject, TKey>
		// ====================================================================

		#region Generic Standalone: ISourceUpdater<string, Guid>

		[Fact]
		public void SourceUpdater_GenericStandalone_AddOrUpdate_CanCall()
		{
			var knockOff = new SourceUpdaterGenericStub<string, Guid>();
			ISourceUpdater<string, Guid> service = knockOff;

			string? captured = null;
			knockOff.AddOrUpdate.Call((item) => captured = item);

			service.AddOrUpdate("test-item");

			Assert.Equal("test-item", captured);
		}

		[Fact]
		public void SourceUpdater_GenericStandalone_BothRefreshOverloads_WorkIndependently()
		{
			var knockOff = new SourceUpdaterGenericStub<string, Guid>();
			ISourceUpdater<string, Guid> service = knockOff;

			List<string>? capturedStrings = null;
			List<Guid>? capturedGuids = null;

			knockOff.Refresh.Call((IEnumerable<string> items) =>
			{
				capturedStrings = items.ToList();
			});
			knockOff.Refresh.Call((IEnumerable<Guid> keys) =>
			{
				capturedGuids = keys.ToList();
			});

			var testGuid = Guid.NewGuid();
			service.Refresh(new[] { "a", "b" });
			service.Refresh(new[] { testGuid });

			Assert.NotNull(capturedStrings);
			Assert.Equal(2, capturedStrings!.Count);
			Assert.Equal("a", capturedStrings[0]);

			Assert.NotNull(capturedGuids);
			Assert.Single(capturedGuids!);
			Assert.Equal(testGuid, capturedGuids[0]);
		}

		[Fact]
		public void SourceUpdater_GenericStandalone_Refresh_VerifyPerOverload()
		{
			var knockOff = new SourceUpdaterGenericStub<string, Guid>();
			ISourceUpdater<string, Guid> service = knockOff;

			var stringTracking = knockOff.Refresh.Call((IEnumerable<string> items) => { });
			var guidTracking = knockOff.Refresh.Call((IEnumerable<Guid> keys) => { });

			service.Refresh(new[] { "a" });
			service.Refresh(new[] { Guid.NewGuid() });
			service.Refresh(new[] { Guid.NewGuid() });

			stringTracking.Verify(Called.Once);
			guidTracking.Verify(Called.Exactly(2));

			// Total across all overloads
			knockOff.Refresh.Verify(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Pattern 8: Open Generic Interface — ISourceUpdater<,>
		// ====================================================================

		#region Open Generic Interface: ISourceUpdater<string, Guid>

		[Fact]
		public void SourceUpdater_OpenGeneric_AddOrUpdate_CanCall()
		{
			var stub = new GenericOpenInlineTests.Stubs.ISourceUpdater<string, Guid>();
			ISourceUpdater<string, Guid> service = stub;

			string? captured = null;
			stub.AddOrUpdate.Call((item) => captured = item);

			service.AddOrUpdate("test-item");

			Assert.Equal("test-item", captured);
		}

		[Fact]
		public void SourceUpdater_OpenGeneric_BothRefreshOverloads_WorkIndependently()
		{
			var stub = new GenericOpenInlineTests.Stubs.ISourceUpdater<string, Guid>();
			ISourceUpdater<string, Guid> service = stub;

			List<string>? capturedStrings = null;
			List<Guid>? capturedGuids = null;

			stub.Refresh.Call((IEnumerable<string> items) =>
			{
				capturedStrings = items.ToList();
			});
			stub.Refresh.Call((IEnumerable<Guid> keys) =>
			{
				capturedGuids = keys.ToList();
			});

			var testGuid = Guid.NewGuid();
			service.Refresh(new[] { "x", "y" });
			service.Refresh(new[] { testGuid });

			Assert.NotNull(capturedStrings);
			Assert.Equal(2, capturedStrings!.Count);
			Assert.NotNull(capturedGuids);
			Assert.Single(capturedGuids!);
			Assert.Equal(testGuid, capturedGuids[0]);
		}

		[Fact]
		public void SourceUpdater_OpenGeneric_Refresh_VerifyPerOverload()
		{
			var stub = new GenericOpenInlineTests.Stubs.ISourceUpdater<string, Guid>();
			ISourceUpdater<string, Guid> service = stub;

			var stringTracking = stub.Refresh.Call((IEnumerable<string> items) => { });
			var guidTracking = stub.Refresh.Call((IEnumerable<Guid> keys) => { });

			service.Refresh(new[] { "a" });
			service.Refresh(new[] { Guid.NewGuid() });
			service.Refresh(new[] { Guid.NewGuid() });

			stringTracking.Verify(Called.Once);
			guidTracking.Verify(Called.Exactly(2));

			stub.Refresh.Verify(Called.Exactly(3));
		}

		#endregion

		// ====================================================================
		// Edge Case 2: GenericContainer — generic virtual methods
		// SetThings<T>() where T : class => ReferencedContainer<T>
		// Run<TReturn>() where TReturn : new() => TReturn
		// ====================================================================

		#region Standalone Class: GenericContainer

		[Fact]
		public void GenericContainer_Standalone_SetThings_DifferentTypeArgs()
		{
			var stub = new GenericContainerStandaloneKnockOff();
			var containerOne = new ReferencedContainer<ReferenceTypeOne>();
			var containerTwo = new ReferencedContainer<ReferenceTypeTwo>();

			stub.SetThings.Of<ReferenceTypeOne>().Call(() => containerOne);
			stub.SetThings.Of<ReferenceTypeTwo>().Call(() => containerTwo);

			var resultOne = stub.Object.SetThings<ReferenceTypeOne>();
			var resultTwo = stub.Object.SetThings<ReferenceTypeTwo>();

			Assert.Same(containerOne, resultOne);
			Assert.Same(containerTwo, resultTwo);
		}

		[Fact]
		public void GenericContainer_Standalone_Run_DifferentTypeArgs()
		{
			var stub = new GenericContainerStandaloneKnockOff();
			var guidReturn = Guid.NewGuid();

			stub.Run.Of<int>().Call(() => 42);
			stub.Run.Of<Guid>().Call(() => guidReturn);

			Assert.Equal(42, stub.Object.Run<int>());
			Assert.Equal(guidReturn, stub.Object.Run<Guid>());
		}

		[Fact]
		public void GenericContainer_Standalone_Verify_TracksPerTypeArg()
		{
			var stub = new GenericContainerStandaloneKnockOff();

			stub.Object.SetThings<ReferenceTypeOne>();
			stub.Object.SetThings<ReferenceTypeTwo>();
			stub.Object.SetThings<ReferenceTypeOne>();

			stub.SetThings.Of<ReferenceTypeOne>().Verify(Called.Exactly(2));
			stub.SetThings.Of<ReferenceTypeTwo>().Verify(Called.Once);
			stub.SetThings.Verify(Called.Exactly(3));
		}

		[Fact]
		public void GenericContainer_Standalone_VirtualFallback_WhenUnconfigured()
		{
			var stub = new GenericContainerStandaloneKnockOff();

			// Virtual method should fall back to base implementation
			var result = stub.Object.Run<List<int>>();

			// base.Run<List<int>>() returns new(), which is an empty list
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		#endregion

		#region Inline Class: GenericContainer

		[Fact]
		public void GenericContainer_Inline_SetThings_DifferentTypeArgs()
		{
			var stub = new GenericInlineTests.Stubs.GenericContainer();
			var containerOne = new ReferencedContainer<ReferenceTypeOne>();
			var containerTwo = new ReferencedContainer<ReferenceTypeTwo>();

			stub.SetThings.Of<ReferenceTypeOne>().Call(() => containerOne);
			stub.SetThings.Of<ReferenceTypeTwo>().Call(() => containerTwo);

			Assert.Same(containerOne, stub.Object.SetThings<ReferenceTypeOne>());
			Assert.Same(containerTwo, stub.Object.SetThings<ReferenceTypeTwo>());
		}

		[Fact]
		public void GenericContainer_Inline_Run_DifferentTypeArgs()
		{
			var stub = new GenericInlineTests.Stubs.GenericContainer();
			var guidReturn = Guid.NewGuid();

			stub.Run.Of<int>().Call(() => 42);
			stub.Run.Of<Guid>().Call(() => guidReturn);

			Assert.Equal(42, stub.Object.Run<int>());
			Assert.Equal(guidReturn, stub.Object.Run<Guid>());
		}

		[Fact]
		public void GenericContainer_Inline_VirtualFallback_WhenUnconfigured()
		{
			var stub = new GenericInlineTests.Stubs.GenericContainer();

			var result = stub.Object.Run<List<int>>();

			Assert.NotNull(result);
			Assert.Empty(result);
		}

		#endregion

		// ====================================================================
		// Edge Case 3: IGenericContainer — multiple generic method overloads
		// void Sprint<TReturn>()
		// TReturn Run<TReturn>() where TReturn : new()
		// TReturn Run<TInput, TReturn>(TInput input) where TReturn : new()
		// ====================================================================

		#region Standalone: IGenericContainer

		[Fact]
		public void GenericContainerInterface_Standalone_Sprint_VoidGenericMethod()
		{
			var knockOff = new GenericContainerInterfaceStandaloneKnockOff();
			IGenericContainer service = knockOff;

			var called = false;
			knockOff.Sprint.Of<int>().Call(() => called = true);

			service.Sprint<int>();

			Assert.True(called);
			knockOff.Sprint.Of<int>().Verify(Called.Once);
		}

		[Fact]
		public void GenericContainerInterface_Standalone_Run_DifferentReturnTypes()
		{
			var knockOff = new GenericContainerInterfaceStandaloneKnockOff();
			IGenericContainer service = knockOff;

			var guidReturn = Guid.NewGuid();

			knockOff.Run.Of<int>().Call(() => 4);
			knockOff.Run.Of<Guid>().Call(() => guidReturn);

			Assert.Equal(4, service.Run<int>());
			Assert.Equal(guidReturn, service.Run<Guid>());
		}

		[Fact]
		public void GenericContainerInterface_Standalone_RunTwoTypeParams()
		{
			var knockOff = new GenericContainerInterfaceStandaloneKnockOff();
			IGenericContainer service = knockOff;

			var guidReturn = Guid.NewGuid();
			var guidArgument = Guid.NewGuid();

			knockOff.Run.Of<int, Guid>().Call((input) => guidReturn);
			knockOff.Run.Of<Guid, int>().Call((input) => 5);

			Assert.Equal(guidReturn, service.Run<int, Guid>(4));
			Assert.Equal(5, service.Run<Guid, int>(guidArgument));
		}

		[Fact]
		public void GenericContainerInterface_Standalone_Verify_TracksPerTypeArg()
		{
			var knockOff = new GenericContainerInterfaceStandaloneKnockOff();
			IGenericContainer service = knockOff;

			service.Run<int>();
			service.Run<Guid>();
			service.Run<int>();

			knockOff.Run.Of<int>().Verify(Called.Exactly(2));
			knockOff.Run.Of<Guid>().Verify(Called.Once);
		}

		#endregion

		#region Inline: IGenericContainer

		[Fact]
		public void GenericContainerInterface_Inline_Sprint_VoidGenericMethod()
		{
			var stub = new GenericInlineTests.Stubs.IGenericContainer();
			IGenericContainer service = stub;

			var called = false;
			stub.Sprint.Of<string>().Call(() => called = true);

			service.Sprint<string>();

			Assert.True(called);
		}

		[Fact]
		public void GenericContainerInterface_Inline_Run_DifferentReturnTypes()
		{
			var stub = new GenericInlineTests.Stubs.IGenericContainer();
			IGenericContainer service = stub;

			var guidReturn = Guid.NewGuid();

			stub.Run.Of<int>().Call(() => 4);
			stub.Run.Of<Guid>().Call(() => guidReturn);

			Assert.Equal(4, service.Run<int>());
			Assert.Equal(guidReturn, service.Run<Guid>());
		}

		[Fact]
		public void GenericContainerInterface_Inline_RunTwoTypeParams()
		{
			var stub = new GenericInlineTests.Stubs.IGenericContainer();
			IGenericContainer service = stub;

			var guidReturn = Guid.NewGuid();
			var guidArgument = Guid.NewGuid();

			stub.Run.Of<int, Guid>().Call((input) => guidReturn);
			stub.Run.Of<Guid, int>().Call((input) => 5);

			Assert.Equal(guidReturn, service.Run<int, Guid>(4));
			Assert.Equal(5, service.Run<Guid, int>(guidArgument));
		}

		#endregion
	}
}
