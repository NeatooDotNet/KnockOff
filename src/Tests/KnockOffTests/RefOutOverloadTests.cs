namespace KnockOff.Tests;

/// <summary>
/// Tests for overload groups where one overload has ref or out parameters.
/// When any overload in a group has ref/out, the entire group falls back to
/// generated interceptor classes instead of pre-compiled TTuple types.
/// </summary>
public class RefOutOverloadTests
{
	[Fact]
	public void RefOutOverload_CompilesAndHasInterceptors()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		Assert.NotNull(knockOff.Process);
		Assert.NotNull(knockOff.TryGet);
	}

	[Fact]
	public void RefOutOverload_NonRefProcess_Works()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		var tracking = knockOff.Process.Return((input) => input.Length);

		var result = service.Process("hello");

		Assert.Equal(5, result);
		tracking.Verify(Called.Once);
		Assert.Equal("hello", tracking.LastArg);
	}

	[Fact]
	public void RefOutOverload_RefProcess_Works()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		var tracking = knockOff.Process.Return((string input, ref int counter) =>
		{
			counter += input.Length;
			return counter;
		});

		int counter = 10;
		var result = service.Process("hello", ref counter);

		Assert.Equal(15, result);
		Assert.Equal(15, counter);
		tracking.Verify(Called.Once);
		var args = tracking.LastArgs;
		Assert.Equal("hello", args.input);
		Assert.Equal(10, args.counter); // original value at call time
	}

	[Fact]
	public void RefOutOverload_BothProcessOverloads_TrackIndependently()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		var tracking1 = knockOff.Process.Return((input) => 1);
		var tracking2 = knockOff.Process.Return((string input, ref int counter) =>
		{
			counter++;
			return 2;
		});

		service.Process("a");
		service.Process("b");
		int c = 0;
		service.Process("c", ref c);

		tracking1.Verify(Called.Exactly(2));
		tracking2.Verify(Called.Once);
	}

	[Fact]
	public void RefOutOverload_NonOutTryGet_Works()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		knockOff.TryGet.Return((key) => key == "exists");

		Assert.True(service.TryGet("exists"));
		Assert.False(service.TryGet("missing"));
	}

	[Fact]
	public void RefOutOverload_OutTryGet_Works()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		knockOff.TryGet.Return((string key, out string? value) =>
		{
			if (key == "exists")
			{
				value = "found";
				return true;
			}
			value = null;
			return false;
		});

		var found = service.TryGet("exists", out var value);

		Assert.True(found);
		Assert.Equal("found", value);
	}

	[Fact]
	public void RefOutOverload_RefProcess_Sequence_ThenReturn()
	{
		var knockOff = new RefOutOverloadServiceKnockOff();
		IRefOutOverloadService service = knockOff;

		knockOff.Process
			.Return((string input, ref int counter) =>
			{
				counter += 1;
				return 100;
			})
			.ThenReturn((string input, ref int counter) =>
			{
				counter += 10;
				return 200;
			});

		int c = 0;
		Assert.Equal(100, service.Process("a", ref c));
		Assert.Equal(200, service.Process("b", ref c));
		Assert.Equal(200, service.Process("c", ref c)); // repeats last

		Assert.Equal(21, c); // 1 + 10 + 10
	}
}
