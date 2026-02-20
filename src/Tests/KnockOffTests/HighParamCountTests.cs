namespace KnockOff.Tests;

/// <summary>
/// Tests for methods with >8 parameters, which trigger the generated
/// interceptor fallback path instead of pre-compiled TTuple types.
/// </summary>
public class HighParamCountTests
{
	[Fact]
	public void HighParamCount_CompilesAndImplementsInterface()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		Assert.NotNull(knockOff.Compute);
		Assert.NotNull(knockOff.ProcessAll);
	}

	[Fact]
	public void HighParamCount_9Params_ReturnCallback()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		knockOff.Compute.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i) =>
			a + b + c + d + e + f + g + h + i);

		var result = service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9);

		Assert.Equal(45, result);
	}

	[Fact]
	public void HighParamCount_9Params_LastArgs()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		var tracking = knockOff.Compute.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i) => 0);

		service.Compute(10, 20, 30, 40, 50, 60, 70, 80, 90);

		var args = tracking.LastArgs;
		Assert.Equal(10, args.a);
		Assert.Equal(20, args.b);
		Assert.Equal(30, args.c);
		Assert.Equal(40, args.d);
		Assert.Equal(50, args.e);
		Assert.Equal(60, args.f);
		Assert.Equal(70, args.g);
		Assert.Equal(80, args.h);
		Assert.Equal(90, args.i);
	}

	[Fact]
	public void HighParamCount_9Params_Verify()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		knockOff.Compute.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i) => 0);

		service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9);
		service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9);

		knockOff.Compute.Verify(Called.Exactly(2));
	}

	[Fact]
	public void HighParamCount_10Params_VoidCallCallback()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		int sum = 0;
		knockOff.ProcessAll.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) =>
		{
			sum = a + b + c + d + e + f + g + h + i + j;
		});

		service.ProcessAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

		Assert.Equal(55, sum);
	}

	[Fact]
	public void HighParamCount_10Params_Verify()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		var tracking = knockOff.ProcessAll.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) => { });

		service.ProcessAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
		service.ProcessAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
		service.ProcessAll(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

		tracking.Verify(Called.Exactly(3));
	}

	[Fact]
	public void HighParamCount_9Params_Sequence_ThenReturn()
	{
		var knockOff = new HighParamCountServiceKnockOff();
		IHighParamCountService service = knockOff;

		knockOff.Compute
			.Call((int a, int b, int c, int d, int e, int f, int g, int h, int i) => 100)
			.ThenReturn((int a, int b, int c, int d, int e, int f, int g, int h, int i) => 200)
			.ThenReturn((int a, int b, int c, int d, int e, int f, int g, int h, int i) => 300);

		Assert.Equal(100, service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9));
		Assert.Equal(200, service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9));
		Assert.Equal(300, service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9));
		Assert.Equal(300, service.Compute(1, 2, 3, 4, 5, 6, 7, 8, 9)); // repeats last
	}
}
