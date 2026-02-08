namespace KnockOff.Tests;

/// <summary>
/// Tests for mixed ref/out parameter support in KnockOff.
/// Verifies methods that combine normal, out, and ref parameters.
/// </summary>
public class MixedRefOutTests
{
	[Fact]
	public void MixedRefOut_CompilesAndImplementsInterface()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		Assert.NotNull(knockOff.Process);
		Assert.NotNull(knockOff.Transform);
	}

	[Fact]
	public void MixedRefOut_Process_AllParameterKinds()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Process.Return((string input, out string output, ref int counter) =>
		{
			output = input.ToUpperInvariant();
			counter++;
			return true;
		});

		int counter = 10;
		var result = service.Process("hello", out string output, ref counter);

		Assert.True(result);
		Assert.Equal("HELLO", output);
		Assert.Equal(11, counter);
	}

	[Fact]
	public void MixedRefOut_Process_LastArgs_CapturesNormalAndRefOnly()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Process.Return((string input, out string output, ref int counter) =>
		{
			output = input;
			counter += 100;
			return true;
		});

		int counter = 5;
		service.Process("test-input", out _, ref counter);

		knockOff.Process.Verify(Called.Once);
		// LastArgs captures normal + ref params (at call time), excludes out params
		var args = knockOff.Process.LastArgs;
		Assert.Equal("test-input", args!.Value.input);
		Assert.Equal(5, args!.Value.counter); // value AT CALL TIME, not after modification
	}

	[Fact]
	public void MixedRefOut_Transform_MultipleOuts()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Transform.Return((string input, out string transformed, out bool wasModified) =>
		{
			transformed = input.Trim();
			wasModified = transformed != input;
			return transformed.Length;
		});

		var len = service.Transform("  hi  ", out string transformed, out bool wasModified);

		Assert.Equal(2, len);
		Assert.Equal("hi", transformed);
		Assert.True(wasModified);
	}

	[Fact]
	public void MixedRefOut_Transform_NoModification()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Transform.Return((string input, out string transformed, out bool wasModified) =>
		{
			transformed = input.Trim();
			wasModified = transformed != input;
			return transformed.Length;
		});

		var len = service.Transform("clean", out string transformed, out bool wasModified);

		Assert.Equal(5, len);
		Assert.Equal("clean", transformed);
		Assert.False(wasModified);
	}

	[Fact]
	public void MixedRefOut_Unconfigured_ReturnsDefaults()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		int counter = 42;
		var result = service.Process("input", out string output, ref counter);

		Assert.False(result);     // default(bool)
		Assert.Null(output);      // default(string)
		Assert.Equal(42, counter); // ref unchanged
	}

	[Fact]
	public void MixedRefOut_Verify_TracksCallCount()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Process.Return((string input, out string output, ref int counter) =>
		{
			output = input;
			return true;
		});

		int c = 0;
		service.Process("a", out _, ref c);
		service.Process("b", out _, ref c);

		knockOff.Process.Verify(Called.Exactly(2));
	}

	[Fact]
	public void MixedRefOut_Verify_ThrowsWhenNotSatisfied()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Transform.Return((string input, out string transformed, out bool wasModified) =>
		{
			transformed = input;
			wasModified = false;
			return input.Length;
		});

		service.Transform("once", out _, out _);

		Assert.Throws<VerificationException>(() =>
			knockOff.Transform.Verify(Called.Exactly(2)));
	}

	[Fact]
	public void MixedRefOut_Sequence_ThenReturn()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		knockOff.Process
			.Return((string input, out string output, ref int counter) =>
			{
				output = "first";
				counter += 1;
				return true;
			})
			.ThenReturn((string input, out string output, ref int counter) =>
			{
				output = "second";
				counter += 10;
				return false;
			});

		int c = 0;
		var r1 = service.Process("a", out string o1, ref c);
		var r2 = service.Process("b", out string o2, ref c);
		var r3 = service.Process("c", out string o3, ref c); // repeats last

		Assert.True(r1);
		Assert.Equal("first", o1);

		Assert.False(r2);
		Assert.Equal("second", o2);

		Assert.False(r3);
		Assert.Equal("second", o3);

		Assert.Equal(21, c); // 1 + 10 + 10
	}

	[Fact]
	public void MixedRefOut_Reset_ClearsState()
	{
		var knockOff = new MixedRefOutServiceKnockOff();
		IMixedRefOutService service = knockOff;

		var tracking = knockOff.Process.Return((string input, out string output, ref int counter) =>
		{
			output = input;
			return true;
		});

		int c = 0;
		service.Process("test", out _, ref c);
		tracking.Verify(Called.Once);

		knockOff.Process.Reset();
		tracking.Verify(Called.Never);
	}
}
