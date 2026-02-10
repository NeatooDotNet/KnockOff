namespace KnockOff.Tests;

/// <summary>
/// Tests for the indexer API redesign features.
/// Covers per-key callbacks, per-key sequences, per-key with fallback,
/// multi-param indexers, and all-keys sequences.
/// </summary>
public class IndexerRedesignTests
{
	// =========================================================================
	// AC-2: Per-key Get callback
	// =========================================================================

	[Fact]
	public void PerKey_GetCallback_ReturnConfiguredValue()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["foo"].Get(() => 42);

		var result = store["foo"];

		Assert.Equal(42, result);
	}

	[Fact]
	public void PerKey_GetCallback_DynamicValue()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		var counter = 0;
		knockOff.Indexer["foo"].Get(() => ++counter);

		var r1 = store["foo"];
		var r2 = store["foo"];

		Assert.Equal(1, r1);
		Assert.Equal(2, r2);
	}

	// =========================================================================
	// AC-3: Per-key Set callback
	// =========================================================================

	[Fact]
	public void PerKey_SetCallback_CapturesValue()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		int captured = 0;
		knockOff.Indexer["foo"].Set(v => captured = v);

		store["foo"] = 42;

		Assert.Equal(42, captured);
	}

	[Fact]
	public void PerKey_SetCallback_OnlyInvokedForConfiguredKey()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		int captured = 0;
		knockOff.Indexer["foo"].Set(v => captured = v);

		store["bar"] = 99; // different key, should NOT trigger per-key callback
		store["foo"] = 42;

		Assert.Equal(42, captured);
	}

	// =========================================================================
	// AC-4: Per-key sequences
	// =========================================================================

	[Fact]
	public void PerKey_Sequence_ReturnsValuesInOrderThenRepeatsLast()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["foo"].Returns(1).ThenReturns(2).ThenReturns(3);

		var r1 = store["foo"];
		var r2 = store["foo"];
		var r3 = store["foo"];
		var r4 = store["foo"]; // repeats last

		Assert.Equal(1, r1);
		Assert.Equal(2, r2);
		Assert.Equal(3, r3);
		Assert.Equal(3, r4);
	}

	[Fact]
	public void PerKey_Sequence_IndependentPerKey()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["a"].Returns(10).ThenReturns(20);
		knockOff.Indexer["b"].Returns(100).ThenReturns(200);

		Assert.Equal(10, store["a"]);
		Assert.Equal(100, store["b"]);
		Assert.Equal(20, store["a"]);
		Assert.Equal(200, store["b"]);
	}

	// =========================================================================
	// AC-6: Per-key with all-keys fallback
	// =========================================================================

	[Fact]
	public void PerKey_WinsOverAllKeysCallback()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["foo"].Returns(42);
		knockOff.Indexer.Get((string key) => key.Length);

		var val1 = store["foo"];   // per-key wins
		var val2 = store["hello"]; // all-keys fallback

		Assert.Equal(42, val1);
		Assert.Equal(5, val2);
	}

	// =========================================================================
	// AC-7: Multi-param flattened indexer syntax
	// =========================================================================

	[Fact]
	public void MultiParam_FlattenedIndexer_ReturnsConfiguredValue()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		knockOff.Indexer[1, 2].Returns(3.14);

		var result = matrix[1, 2];

		Assert.Equal(3.14, result);
	}

	[Fact]
	public void MultiParam_FlattenedIndexer_DifferentKeysIndependent()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		knockOff.Indexer[0, 0].Returns(1.0);
		knockOff.Indexer[1, 1].Returns(2.0);
		knockOff.Indexer[2, 2].Returns(3.0);

		Assert.Equal(1.0, matrix[0, 0]);
		Assert.Equal(2.0, matrix[1, 1]);
		Assert.Equal(3.0, matrix[2, 2]);
	}

	// =========================================================================
	// AC-8: Multi-param callbacks with tuple key
	// =========================================================================

	[Fact]
	public void MultiParam_GetCallbackUsesTupleKey()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		knockOff.Indexer.Get(((int row, int col) key) => key.row * 10.0 + key.col);

		var result = matrix[2, 3];

		Assert.Equal(23.0, result);
	}

	[Fact]
	public void MultiParam_SetCallbackUsesTupleKey()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		(int row, int col)? capturedKey = null;
		double capturedValue = 0;
		knockOff.Indexer.Set(((int row, int col) key, double value) =>
		{
			capturedKey = key;
			capturedValue = value;
		});

		matrix[5, 7] = 99.9;

		Assert.NotNull(capturedKey);
		Assert.Equal(5, capturedKey.Value.row);
		Assert.Equal(7, capturedKey.Value.col);
		Assert.Equal(99.9, capturedValue);
	}

	// =========================================================================
	// Per-key VerifyGet / VerifySet
	// =========================================================================

	[Fact]
	public void PerKey_VerifyGet_Passes_WhenCalled()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["key"].Returns(42);
		_ = store["key"];
		_ = store["key"];

		knockOff.Indexer["key"].VerifyGet();
		knockOff.Indexer["key"].VerifyGet(Called.Exactly(2));
	}

	[Fact]
	public void PerKey_VerifyGet_Throws_WhenNotCalled()
	{
		var knockOff = new SimpleIndexerKnockOff();

		knockOff.Indexer["key"].Returns(42);

		Assert.Throws<VerificationException>(() =>
			knockOff.Indexer["key"].VerifyGet());
	}

	[Fact]
	public void PerKey_VerifyGet_Throws_WhenCountMismatch()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["key"].Returns(42);
		_ = store["key"];

		Assert.Throws<VerificationException>(() =>
			knockOff.Indexer["key"].VerifyGet(Called.Exactly(3)));
	}

	[Fact]
	public void PerKey_VerifySet_Passes_WhenCalled()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["key"].Set(v => { });
		store["key"] = 42;

		knockOff.Indexer["key"].VerifySet();
		knockOff.Indexer["key"].VerifySet(Called.Once);
	}

	[Fact]
	public void PerKey_VerifySet_Throws_WhenNotCalled()
	{
		var knockOff = new SimpleIndexerKnockOff();

		knockOff.Indexer["key"].Set(v => { });

		Assert.Throws<VerificationException>(() =>
			knockOff.Indexer["key"].VerifySet());
	}

	[Fact]
	public void PerKey_VerifySet_Throws_WhenCountMismatch()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer["key"].Set(v => { });
		store["key"] = 42;

		Assert.Throws<VerificationException>(() =>
			knockOff.Indexer["key"].VerifySet(Called.Exactly(5)));
	}

	[Fact]
	public void MultiParam_PerKey_VerifyGet()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		knockOff.Indexer[1, 2].Returns(3.14);
		_ = matrix[1, 2];

		knockOff.Indexer[1, 2].VerifyGet(Called.Once);
	}

	// =========================================================================
	// When(predicate) for indexers
	// =========================================================================

	[Fact]
	public void When_GetterMatch_ReturnsConfiguredValue()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer.When(key => key.Length > 3).Returns(99);

		var result = store["hello"]; // length 5, matches predicate

		Assert.Equal(99, result);
	}

	[Fact]
	public void When_NoMatch_FallsThrough()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer.When(key => key.Length > 10).Returns(99);
		knockOff.Indexer.Get((string key) => -1); // fallback

		var result = store["hi"]; // length 2, does NOT match When predicate

		Assert.Equal(-1, result); // falls through to all-keys callback
	}

	[Fact]
	public void When_ThenWhenChain_AdvancesAfterMatch()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer
			.When(key => key.StartsWith("a", System.StringComparison.Ordinal)).Returns(1)
			.ThenWhen(key => key.StartsWith("b", System.StringComparison.Ordinal)).Returns(2);

		var r1 = store["alpha"]; // matches first When
		var r2 = store["beta"];  // matches second When

		Assert.Equal(1, r1);
		Assert.Equal(2, r2);
	}

	[Fact]
	public void When_SetCallback_CapturesKeyAndValue()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		(string key, int value)? captured = null;
		knockOff.Indexer.When(key => key.StartsWith("temp_", System.StringComparison.Ordinal)).Set((key, value) =>
		{
			captured = (key, value);
		});

		store["temp_data"] = 42;

		Assert.NotNull(captured);
		Assert.Equal("temp_data", captured.Value.key);
		Assert.Equal(42, captured.Value.value);
	}

	[Fact]
	public void When_GetSetChains_AreIndependent()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		// Configure getter When chain
		knockOff.Indexer.When(key => key.StartsWith("a", System.StringComparison.Ordinal)).Returns(1);

		// Configure setter When chain (separate from getter)
		knockOff.Indexer.When(key => key.StartsWith("a", System.StringComparison.Ordinal)).Set((key, value) => { });

		// Set operation should NOT consume getter When matcher
		store["alpha"] = 99;

		// Get operation should still return 1 (getter chain unaffected by set)
		var val = store["alpha"];
		Assert.Equal(1, val);
	}

	[Fact]
	public void When_MultiParam_TupleKeyPredicate()
	{
		var knockOff = new MatrixIndexerKnockOff();
		IMatrixIndexer matrix = knockOff;

		knockOff.Indexer.When(key => key.row > 0 && key.col > 0).Returns(1.0);

		var result = matrix[1, 2];

		Assert.Equal(1.0, result);
	}

	[Fact]
	public void When_PriorityBelowPerKey()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		// Per-key exact match takes priority
		knockOff.Indexer["exact"].Returns(100);

		// When predicate matches "exact" too, but per-key wins
		knockOff.Indexer.When(key => key.Length > 3).Returns(42);

		var val1 = store["exact"];  // per-key wins
		var val2 = store["hello"];  // When matches

		Assert.Equal(100, val1);
		Assert.Equal(42, val2);
	}

	[Fact]
	public void When_Standalone_WhenPredicate()
	{
		// Standalone stubs share the same renderer, but this test exercises
		// the pattern to ensure no regression
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer.When(key => key.Length > 3).Returns(42);

		var val = store["hello"]; // length 5, matches
		Assert.Equal(42, val);
	}

	// =========================================================================
	// AC-13: All-keys sequences (Get + ThenGet + ThenGet)
	// =========================================================================

	[Fact]
	public void AllKeys_Sequence_ReturnsInOrder()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer.Get((k) => k.Length)
			.ThenGet((k) => 100)
			.ThenGet((k) => 999);

		var r1 = store["hello"]; // 5
		var r2 = store["world"]; // 100
		var r3 = store["foo"];   // 999

		Assert.Equal(5, r1);
		Assert.Equal(100, r2);
		Assert.Equal(999, r3);
	}

	[Fact]
	public void AllKeys_Sequence_RepeatsLastAfterExhaustion()
	{
		var knockOff = new SimpleIndexerKnockOff();
		ISimpleIndexer store = knockOff;

		knockOff.Indexer.Get((k) => 1)
			.ThenGet((k) => 2);

		var r1 = store["a"]; // 1
		var r2 = store["b"]; // 2
		var r3 = store["c"]; // 2 (repeats last)
		var r4 = store["d"]; // 2 (repeats last)

		Assert.Equal(1, r1);
		Assert.Equal(2, r2);
		Assert.Equal(2, r3);
		Assert.Equal(2, r4);
	}
}
