namespace KnockOff.Tests;

/// <summary>
/// Tests for protected member interception in standalone class stubs.
/// Protected virtual/abstract members are intercepted just like public ones,
/// with interceptors exposed as public properties on the stub wrapper.
/// Since protected members can't be called directly from tests, public
/// non-virtual bridge methods exercise the protected interceptors indirectly.
/// </summary>
public class ProtectedMemberTests
{
	#region Protected Abstract Method Tests

	[Fact]
	public void ProtectedAbstractMethod_Unconfigured_ReturnsDefault()
	{
		var stub = new ProtectedMemberStub();

		// GetDescription() calls GetInternalId() (protected abstract, unconfigured → null)
		// and FormatLabel() (protected virtual, base: $"[{Label}]" → "[]")
		var description = stub.Object.GetDescription();

		Assert.Equal(": []", description);
	}

	[Fact]
	public void ProtectedAbstractMethod_Configured_ReturnsCallbackValue()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "ABC-123");

		var description = stub.Object.GetDescription();

		Assert.Equal("ABC-123: []", description);
	}

	[Fact]
	public void ProtectedAbstractMethod_TracksCallCount()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");

		stub.Object.GetDescription();
		stub.Object.GetDescription();

		stub.GetInternalId.Verify(Called.Exactly(2));
	}

	[Fact]
	public void ProtectedAbstractMethod_ReturnValue_Works()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return("STATIC-ID");

		var description = stub.Object.GetDescription();

		Assert.Equal("STATIC-ID: []", description);
	}

	#endregion

	#region Protected Virtual Method Tests

	[Fact]
	public void ProtectedVirtualMethod_Unconfigured_DelegatesToBase()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");

		// FormatLabel() base implementation: $"[{Label}]" where Label defaults to ""
		var description = stub.Object.GetDescription();

		Assert.Equal("id: []", description);
	}

	[Fact]
	public void ProtectedVirtualMethod_Configured_OverridesBase()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "X");
		stub.FormatLabel.Return(() => "custom-format");

		var description = stub.Object.GetDescription();

		Assert.Equal("X: custom-format", description);
	}

	[Fact]
	public void ProtectedVirtualMethod_TracksCallCount()
	{
		var stub = new ProtectedMemberStub();

		stub.Object.GetDescription(); // calls FormatLabel once

		stub.FormatLabel.Verify(Called.Once);
	}

	#endregion

	#region Protected Property Tests

	[Fact]
	public void ProtectedProperty_Unconfigured_UsesBaseDefault()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");

		// Label base default is "", FormatLabel uses Label: $"[{Label}]"
		var description = stub.Object.GetDescription();

		Assert.Equal("id: []", description);
	}

	[Fact]
	public void ProtectedProperty_ConfiguredGetter_AffectsVirtualMethodThatReadsIt()
	{
		var stub = new ProtectedMemberStub();
		stub.Label.Get(() => "MyLabel");
		stub.GetInternalId.Return(() => "id");

		// FormatLabel (unconfigured → base: $"[{Label}]") reads Label (configured → "MyLabel")
		var description = stub.Object.GetDescription();

		Assert.Equal("id: [MyLabel]", description);
	}

	[Fact]
	public void ProtectedProperty_TracksGetterCalls()
	{
		var stub = new ProtectedMemberStub();

		// GetDescription calls FormatLabel (base), which reads Label
		stub.Object.GetDescription();

		stub.Label.VerifyGet(Called.Once);
	}

	#endregion

	#region Protected Indexer Tests

	[Fact]
	public void ProtectedIndexer_Unconfigured_DelegatesToBase()
	{
		var stub = new ProtectedMemberStub();

		// Base indexer getter returns ""
		var result = stub.Object.GetItemAt(0);

		Assert.Equal("", result);
	}

	[Fact]
	public void ProtectedIndexer_ConfiguredGetter_ReturnsCallbackValue()
	{
		var stub = new ProtectedMemberStub();
		stub.Indexer.Get((index) => $"item-{index}");

		var result = stub.Object.GetItemAt(3);

		Assert.Equal("item-3", result);
	}

	[Fact]
	public void ProtectedIndexer_SetterTracksCall()
	{
		var stub = new ProtectedMemberStub();

		stub.Object.SetItemAt(0, "value");

		stub.Indexer.VerifySet(Called.Once);
	}

	[Fact]
	public void ProtectedIndexer_BackedByDictionary()
	{
		var stub = new ProtectedMemberStub();
		var data = new Dictionary<int, string>();
		stub.Indexer.Get((index) => data.TryGetValue(index, out var v) ? v : "");
		stub.Indexer.Set((index, value) => data[index] = value);

		stub.Object.SetItemAt(5, "hello");
		var result = stub.Object.GetItemAt(5);

		Assert.Equal("hello", result);
	}

	[Fact]
	public void ProtectedIndexer_GetterTracksCallCount()
	{
		var stub = new ProtectedMemberStub();

		stub.Object.GetItemAt(0);
		stub.Object.GetItemAt(1);
		stub.Object.GetItemAt(2);

		stub.Indexer.VerifyGet(Called.Exactly(3));
	}

	#endregion

	#region Protected Event Tests

	[Fact]
	public void ProtectedEvent_HasSubscribers_FalseByDefault()
	{
		var stub = new ProtectedMemberStub();

		Assert.False(stub.InternalStateChanged.HasSubscribers);
	}

	[Fact]
	public void ProtectedEvent_Raise_DoesNotThrowWhenNoSubscribers()
	{
		var stub = new ProtectedMemberStub();

		// Should not throw even with no subscribers
		stub.InternalStateChanged.Raise(stub.Object, EventArgs.Empty);
	}

	#endregion

	#region Verification Tests

	[Fact]
	public void Protected_Verify_TracksMultipleCalls()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");

		stub.Object.GetDescription();
		stub.Object.GetDescription();
		stub.Object.GetDescription();

		stub.GetInternalId.Verify(Called.Exactly(3));
		stub.FormatLabel.Verify(Called.Exactly(3));
	}

	[Fact]
	public void Protected_Verify_ThrowsWhenNotSatisfied()
	{
		var stub = new ProtectedMemberStub();

		stub.Object.GetDescription(); // calls GetInternalId once

		Assert.Throws<VerificationException>(() =>
			stub.GetInternalId.Verify(Called.Exactly(2)));
	}

	[Fact]
	public void Protected_Verifiable_PassesWhenCalled()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id").Verifiable();

		stub.Object.GetDescription();

		stub.Verify(); // Should not throw
	}

	[Fact]
	public void Protected_Verifiable_ThrowsWhenNotCalled()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id").Verifiable();
		// Don't call GetDescription

		Assert.Throws<VerificationException>(() => stub.Verify());
	}

	#endregion

	#region Sequence Tests

	[Fact]
	public void ProtectedAbstractMethod_Sequence_ReturnsValuesInOrder()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId
			.Return(() => "first")
			.ThenReturn(() => "second");

		var d1 = stub.Object.GetDescription();
		var d2 = stub.Object.GetDescription();
		var d3 = stub.Object.GetDescription(); // repeats last

		Assert.Equal("first: []", d1);
		Assert.Equal("second: []", d2);
		Assert.Equal("second: []", d3);
	}

	[Fact]
	public void ProtectedVirtualMethod_Sequence_ReturnsValuesInOrder()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "X");
		stub.FormatLabel
			.Return(() => "label-1")
			.ThenReturn(() => "label-2");

		var d1 = stub.Object.GetDescription();
		var d2 = stub.Object.GetDescription();

		Assert.Equal("X: label-1", d1);
		Assert.Equal("X: label-2", d2);
	}

	[Fact]
	public void ProtectedVirtualMethod_Sequence_FallsToBaseAfterExhausted()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "X");
		stub.FormatLabel
			.Return(() => "label-1")
			.ThenReturn(() => "label-2");

		stub.Object.GetDescription(); // label-1
		stub.Object.GetDescription(); // label-2
		var d3 = stub.Object.GetDescription(); // sequence exhausted → base: "[]"

		Assert.Equal("X: []", d3);
	}

	#endregion

	#region Reset Tests

	[Fact]
	public void Protected_Reset_ClearsTrackingState()
	{
		var stub = new ProtectedMemberStub();
		var tracking = stub.GetInternalId.Return(() => "id");

		stub.Object.GetDescription();
		tracking.Verify(Called.Once);

		stub.GetInternalId.Reset();

		tracking.Verify(Called.Never);
	}

	[Fact]
	public void Protected_ResetInterceptors_ClearsAllState()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");

		stub.Object.GetDescription();
		stub.Object.GetItemAt(0);
		stub.Object.SetItemAt(0, "v");

		stub.ResetInterceptors();

		stub.GetInternalId.Verify(Called.Never);
		stub.FormatLabel.Verify(Called.Never);
		stub.Indexer.VerifyGet(Called.Never);
		stub.Indexer.VerifySet(Called.Never);
	}

	#endregion

	#region Strict Mode Tests

	[Fact]
	public void ProtectedAbstractMethod_StrictMode_ThrowsWhenUnconfigured()
	{
		var stub = new ProtectedMemberStub();
		stub.Strict = true;

		// GetDescription calls GetInternalId (abstract, unconfigured) → should throw
		Assert.Throws<StubException>(() => stub.Object.GetDescription());
	}

	[Fact]
	public void ProtectedAbstractMethod_StrictMode_ConfiguredDoesNotThrow()
	{
		var stub = new ProtectedMemberStub();
		stub.Strict = true;
		stub.GetInternalId.Return(() => "id");
		stub.FormatLabel.Return(() => "label");

		// Both configured → strict mode doesn't throw
		var description = stub.Object.GetDescription();

		Assert.Equal("id: label", description);
	}

	[Fact]
	public void ProtectedVirtualMethod_StrictMode_ThrowsWhenUnconfigured()
	{
		var stub = new ProtectedMemberStub();
		stub.Strict = true;
		stub.GetInternalId.Return(() => "id");

		// FormatLabel is virtual but unconfigured → strict mode throws
		Assert.Throws<StubException>(() => stub.Object.GetDescription());
	}

	#endregion

	#region Combined Member Tests

	[Fact]
	public void ProtectedMembers_AllConfigured_WorkTogether()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "SVC-001");
		stub.Label.Get(() => "Production");
		// FormatLabel unconfigured → base reads Label → "Production" → "[Production]"

		var description = stub.Object.GetDescription();

		Assert.Equal("SVC-001: [Production]", description);
	}

	[Fact]
	public void ProtectedMembers_MethodAndIndexer_IndependentTracking()
	{
		var stub = new ProtectedMemberStub();
		stub.GetInternalId.Return(() => "id");
		stub.Indexer.Get((i) => $"val-{i}");

		stub.Object.GetDescription();
		stub.Object.GetItemAt(0);
		stub.Object.GetItemAt(1);

		stub.GetInternalId.Verify(Called.Once);
		stub.FormatLabel.Verify(Called.Once);
		stub.Indexer.VerifyGet(Called.Exactly(2));
	}

	#endregion
}

#region Base Class and Stub

/// <summary>
/// Abstract base class with protected members for testing protected member interception.
/// Public non-virtual bridge methods call through to protected virtual/abstract members,
/// enabling tests to exercise protected interceptors indirectly.
/// </summary>
public abstract class ProtectedMemberBase
{
	/// <summary>Protected abstract method — must be implemented.</summary>
	protected abstract string GetInternalId();

	/// <summary>Protected virtual method with default implementation.</summary>
	protected virtual string FormatLabel() => $"[{Label}]";

	/// <summary>Protected virtual property with default value.</summary>
	protected virtual string Label { get; set; } = "";

	/// <summary>Protected virtual indexer with default implementation.</summary>
	protected virtual string this[int index]
	{
		get => "";
		set { }
	}

	/// <summary>Protected virtual event.</summary>
	protected virtual event EventHandler? InternalStateChanged;

	/// <summary>Raises InternalStateChanged (for subclasses).</summary>
	protected void OnInternalStateChanged()
	{
		InternalStateChanged?.Invoke(this, EventArgs.Empty);
	}

	// =========================================================================
	// Public NON-VIRTUAL bridge methods (not intercepted — they call through
	// to protected members which ARE intercepted by the generated Impl class)
	// =========================================================================

	/// <summary>
	/// Calls <see cref="GetInternalId"/> and <see cref="FormatLabel"/>
	/// to produce a description string.
	/// </summary>
	public string GetDescription() => $"{GetInternalId()}: {FormatLabel()}";

	/// <summary>Reads the protected indexer at the given position.</summary>
	public string GetItemAt(int index) => this[index];

	/// <summary>Writes the protected indexer at the given position.</summary>
	public void SetItemAt(int index, string value) => this[index] = value;
}

/// <summary>
/// Standalone class stub for ProtectedMemberBase.
/// Interceptors for protected members are exposed as public properties.
/// </summary>
[KnockOffBase<ProtectedMemberBase>]
public partial class ProtectedMemberStub
{
}

#endregion
