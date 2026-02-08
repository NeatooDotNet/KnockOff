namespace KnockOff.Tests;

/// <summary>
/// Tests for ClassRenderer indexer verification API.
/// Verifies that inline class stubs ([KnockOff&lt;SomeClass&gt;]) with virtual indexers
/// have the full verification API (Verifiable, VerifyGet, VerifySet, etc.).
/// </summary>
public class ClassIndexerVerificationTests
{
	#region Verifiable() Tests

	[Fact]
	public void Verifiable_MarksIndexerForStubVerify()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();
		// Don't access the indexer

		// Act & Assert - Stub.Verify() should fail because verifiable indexer wasn't accessed
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void Verifiable_SucceedsWhenIndexerAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();

		// Act
		_ = stub.Object["key"];

		// Assert - Should not throw
		stub.Verify();
	}

	[Fact]
	public void Verifiable_WithTimes_VerifiesWithConstraint()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable(Called.Exactly(2));

		// Act
		_ = stub.Object["key1"];
		// Only accessed once but expected twice

		// Assert
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void Verifiable_WithTimes_SucceedsWhenConstraintMet()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable(Called.Exactly(2));

		// Act
		_ = stub.Object["key1"];
		_ = stub.Object["key2"];

		// Assert - Should not throw
		stub.Verify();
	}

	[Fact]
	public void NonVerifiable_NotCheckedByStubVerify()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// No .Verifiable() call
		// Don't access the indexer

		// Assert - Stub.Verify() should pass because nothing is marked verifiable
		stub.Verify(); // Should not throw
	}

	#endregion

	#region VerifyGet() Tests

	[Fact]
	public void VerifyGet_SucceedsWhenGetterAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		_ = stub.Object["key"];

		// Assert - Should not throw
		stub.Indexer.VerifyGet();
	}

	[Fact]
	public void VerifyGet_ThrowsWhenGetterNotAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// Don't access the indexer

		// Act & Assert
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifyGet());
	}

	[Fact]
	public void VerifyGet_WithTimes_VerifiesWithConstraint()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		_ = stub.Object["key1"];
		_ = stub.Object["key2"];
		_ = stub.Object["key3"];

		// Assert - Should not throw (called 3 times, expected exactly 3)
		stub.Indexer.VerifyGet(Called.Exactly(3));
	}

	[Fact]
	public void VerifyGet_WithTimes_ThrowsWhenConstraintNotMet()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		_ = stub.Object["key1"];

		// Assert - Called 1 time, expected exactly 3
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifyGet(Called.Exactly(3)));
	}

	[Fact]
	public void VerifyGet_DoesNotCountSetterAccess()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - Only set, never get
		stub.Object["key"] = "value";

		// Assert - VerifyGet should throw because getter was never accessed
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifyGet());
	}

	#endregion

	#region VerifySet() Tests

	[Fact]
	public void VerifySet_SucceedsWhenSetterAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		stub.Object["key"] = "value";

		// Assert - Should not throw
		stub.Indexer.VerifySet();
	}

	[Fact]
	public void VerifySet_ThrowsWhenSetterNotAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// Don't access the indexer

		// Act & Assert
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifySet());
	}

	[Fact]
	public void VerifySet_WithTimes_VerifiesWithConstraint()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		stub.Object["key1"] = "value1";
		stub.Object["key2"] = "value2";

		// Assert - Should not throw (set 2 times, expected exactly 2)
		stub.Indexer.VerifySet(Called.Exactly(2));
	}

	[Fact]
	public void VerifySet_WithTimes_ThrowsWhenConstraintNotMet()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		stub.Object["key1"] = "value1";

		// Assert - Set 1 time, expected exactly 3
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifySet(Called.Exactly(3)));
	}

	[Fact]
	public void VerifySet_DoesNotCountGetterAccess()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - Only get, never set
		_ = stub.Object["key"];

		// Assert - VerifySet should throw because setter was never accessed
		Assert.Throws<VerificationException>(() => stub.Indexer.VerifySet());
	}

	#endregion

	#region Verify() (Total Access) Tests

	[Fact]
	public void Verify_CountsTotalAccess_GetAndSet()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - 2 gets + 1 set = 3 total
		_ = stub.Object["key1"];
		_ = stub.Object["key2"];
		stub.Object["key3"] = "value";

		// Assert - Should not throw (3 total accesses, expected exactly 3)
		stub.Indexer.Verify(Called.Exactly(3));
	}

	[Fact]
	public void Verify_ThrowsWhenTotalAccessDoesNotMeetConstraint()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - 1 get only
		_ = stub.Object["key"];

		// Assert - Only 1 access, expected at least 5
		Assert.Throws<VerificationException>(() => stub.Indexer.Verify(Called.AtLeast(5)));
	}

	[Fact]
	public void Verify_WithNoArgs_DefaultsToAtLeastOnce()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// Don't access the indexer

		// Act & Assert - Default is AtLeastOnce, so should throw
		Assert.Throws<VerificationException>(() => stub.Indexer.Verify());
	}

	[Fact]
	public void Verify_SucceedsWithTimesNever_WhenNotAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// Don't access the indexer

		// Assert - Should not throw (expected never, never accessed)
		stub.Indexer.Verify(Called.Never);
	}

	#endregion

	#region Stub.Verify() Integration Tests

	[Fact]
	public void StubVerify_IncludesVerifiableIndexers()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();
		stub.Count.Get(() => 10); // Also mark a property as configured
		// Don't access the indexer

		// Act & Assert - Should fail because verifiable indexer wasn't accessed
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void StubVerify_SucceedsWhenAllVerifiableIndexersAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();

		// Act
		_ = stub.Object["key"];

		// Assert - Should not throw
		stub.Verify();
	}

	#endregion

	#region Stub.VerifyAll() Integration Tests

	[Fact]
	public void StubVerifyAll_IncludesConfiguredIndexers()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Get((key) => $"value_{key}");
		// Don't access the indexer

		// Act & Assert - Should fail because configured indexer wasn't accessed
		Assert.Throws<VerificationException>(() => stub.VerifyAll());
	}

	[Fact]
	public void StubVerifyAll_SucceedsWhenAllConfiguredIndexersAccessed()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Get((key) => $"value_{key}");

		// Act
		_ = stub.Object["key"];

		// Assert - Should not throw
		stub.VerifyAll();
	}

	[Fact]
	public void StubVerifyAll_IgnoresUnconfiguredIndexers()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		// Don't configure the indexer
		// Don't access the indexer

		// Assert - Should not throw (nothing configured)
		stub.VerifyAll();
	}

	[Fact]
	public void StubVerifyAll_OnSetConfigurationCountsAsConfigured()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Set((key, value) => { });
		// Don't access the indexer

		// Act & Assert - Should fail because Set makes it configured
		Assert.Throws<VerificationException>(() => stub.VerifyAll());
	}

	#endregion

	#region Reset() Tests

	[Fact]
	public void Reset_PreservesVerifiableMarking()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();

		// Act
		_ = stub.Object["key"];
		stub.Verify(); // Should pass
		stub.Indexer.Reset();

		// Assert - After reset, verifiable marking is preserved but access count is cleared
		// So Verify should fail because no accesses since reset
		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	[Fact]
	public void Reset_ClearsAccessCounts()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		_ = stub.Object["key1"];
		_ = stub.Object["key2"];
		stub.Object["key3"] = "value";

		stub.Indexer.VerifyGet(Called.Exactly(2));
		stub.Indexer.VerifySet(Called.Once);

		stub.Indexer.Reset();

		// Assert
		stub.Indexer.VerifyGet(Called.Never);
		stub.Indexer.VerifySet(Called.Never);
	}

	[Fact]
	public void Reset_PreservesConfiguration()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Get((key) => "configured_value");
		var setWasCalled = false;
		stub.Indexer.Set((key, value) => { setWasCalled = true; });

		// Act
		stub.Indexer.Reset();

		// Assert - Configuration is preserved by verifying callbacks still work
		Assert.Equal("configured_value", stub.Object["test_key"]);
		stub.Object["key"] = "value";
		Assert.True(setWasCalled);
	}

	[Fact]
	public void Reset_AndReaccess_WorksWithVerifiable()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();
		stub.Indexer.Verifiable();

		// Act
		_ = stub.Object["key"];
		stub.Verify(); // Pass
		stub.Indexer.Reset();
		_ = stub.Object["another_key"]; // Access again after reset

		// Assert - Should pass because accessed again after reset
		stub.Verify();
	}

	#endregion

	#region Get-Only Indexer Tests

	[Fact]
	public void GetOnlyIndexer_Verifiable_Works()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.ReadOnlyIndexedService();
		stub.Indexer.Verifiable();

		// Act
		_ = stub.Object[0];

		// Assert - Should not throw
		stub.Verify();
	}

	[Fact]
	public void GetOnlyIndexer_VerifyGet_Works()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.ReadOnlyIndexedService();

		// Act
		_ = stub.Object[0];
		_ = stub.Object[1];

		// Assert
		stub.Indexer.VerifyGet(Called.Exactly(2));
	}

	[Fact]
	public void GetOnlyIndexer_Verify_CountsOnlyGets()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.ReadOnlyIndexedService();

		// Act
		_ = stub.Object[0];

		// Assert - Verify counts total access (which is only gets for this indexer)
		stub.Indexer.Verify(Called.Exactly(1));
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void Verifiable_FluentChaining_Works()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - Fluent chaining returns the interceptor
		var result = stub.Indexer.Verifiable();

		// Assert
		Assert.Same(stub.Indexer, result);
	}

	[Fact]
	public void Verifiable_WithTimes_FluentChaining_Works()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act - Fluent chaining returns the interceptor
		var result = stub.Indexer.Verifiable(Called.AtLeastOnce);

		// Assert
		Assert.Same(stub.Indexer, result);
	}

	[Fact]
	public void VerificationException_ContainsIndexerName()
	{
		// Arrange
		var stub = new ClassIndexerTestClass.Stubs.IndexedCacheService();

		// Act
		var ex = Assert.Throws<VerificationException>(() => stub.Indexer.Verify());

		// Assert
		Assert.Contains("Indexer", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	#endregion
}

#region Test Types

/// <summary>
/// Class with virtual indexer (get/set) for testing inline class stub indexer verification.
/// </summary>
public class IndexedCacheService
{
	private readonly Dictionary<string, string> _cache = new();

	public IndexedCacheService() { }

	/// <summary>Gets or sets the cached value for the specified key.</summary>
	public virtual string this[string key]
	{
		get => _cache.TryGetValue(key, out var value) ? value : string.Empty;
		set => _cache[key] = value;
	}

	/// <summary>Gets the number of items in the cache.</summary>
	public virtual int Count => _cache.Count;
}

/// <summary>
/// Class with virtual get-only indexer for testing.
/// </summary>
public class ReadOnlyIndexedService
{
	private readonly int[] _data = [10, 20, 30, 40, 50];

	public ReadOnlyIndexedService() { }

	/// <summary>Gets the value at the specified index.</summary>
	public virtual int this[int index] => index >= 0 && index < _data.Length ? _data[index] : 0;
}

/// <summary>
/// Test class with [KnockOff&lt;T&gt;] for class stub indexer verification testing.
/// </summary>
[KnockOff<IndexedCacheService>]
[KnockOff<ReadOnlyIndexedService>]
public partial class ClassIndexerTestClass
{
}

#endregion
