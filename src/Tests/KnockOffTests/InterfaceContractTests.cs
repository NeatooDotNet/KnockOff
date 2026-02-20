namespace KnockOff.Tests;

/// <summary>
/// Verifies that generated inner types (builders, sequences) implement their
/// corresponding library interfaces. These are compile-time contract tests:
/// if a generated type doesn't implement the expected interface, the assignment
/// fails with CS0029 at compile time.
///
/// This catches issues like the base class mode bug where MethodCallBuilderImpl
/// didn't implement IMethodTracking — caught by Benchmarks but not KnockOffTests
/// because existing tests used var/concrete types instead of interface types.
/// </summary>
public class InterfaceContractTests
{
	// ========================================================================
	// Method: void, 0 params → IMethodCallBuilder<Action>, IMethodTracking
	// ========================================================================

	[Fact]
	public void VoidMethod_0Param_Builder_IsIMethodCallBuilder()
	{
		var stub = new SampleKnockOff();
		IMethodCallBuilder<SampleKnockOff.DoSomethingInterceptor.DoSomethingDelegate> builder = stub.DoSomething.Call(() => { });
		Assert.NotNull(builder);
	}

	[Fact]
	public void VoidMethod_0Param_Builder_IsIMethodTracking()
	{
		var stub = new SampleKnockOff();
		IMethodTracking tracking = stub.DoSomething.Call(() => { });
		Assert.NotNull(tracking);
	}

	[Fact]
	public void VoidMethod_0Param_Sequence_IsIMethodCallSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.DoSomething.Call(() => { });
		IMethodCallSequence<SampleKnockOff.DoSomethingInterceptor.DoSomethingDelegate> sequence = builder.ThenCall(() => { });
		Assert.NotNull(sequence);
	}

	[Fact]
	public void VoidMethod_0Param_Sequence_IsIMethodSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.DoSomething.Call(() => { });
		IMethodSequence sequence = builder.ThenCall(() => { });
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// Method: void, 3 params → IMethodCallBuilder<TDelegate, TArgs?>, IMethodTracking<TArgs?>
	// ========================================================================

	[Fact]
	public void VoidMethod_MultiParam_Builder_IsIMethodCallBuilderArgs()
	{
		var stub = new SampleKnockOff();
		IMethodCallBuilderArgs<SampleKnockOff.CalculateInterceptor.CalculateDelegate, (string? name, int? @value, bool? flag)> builder =
			stub.Calculate.Call((string name, int value, bool flag) => { });
		Assert.NotNull(builder);
	}

	[Fact]
	public void VoidMethod_MultiParam_Builder_IsIMethodTrackingArgs()
	{
		var stub = new SampleKnockOff();
		IMethodTrackingArgs<(string? name, int? @value, bool? flag)> tracking =
			stub.Calculate.Call((string name, int value, bool flag) => { });
		Assert.NotNull(tracking);
	}

	[Fact]
	public void VoidMethod_MultiParam_Builder_IsIMethodTracking()
	{
		var stub = new SampleKnockOff();
		IMethodTracking tracking = stub.Calculate.Call((string name, int value, bool flag) => { });
		Assert.NotNull(tracking);
	}

	[Fact]
	public void VoidMethod_MultiParam_Sequence_IsIMethodCallSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.Calculate.Call((string name, int value, bool flag) => { });
		IMethodCallSequence<SampleKnockOff.CalculateInterceptor.CalculateDelegate> sequence =
			builder.ThenCall((string name, int value, bool flag) => { });
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// Method: non-void, 0 params → IMethodReturnBuilder<TDelegate>
	// ========================================================================

	[Fact]
	public void NonVoidMethod_0Param_Builder_IsIMethodReturnBuilder()
	{
		var stub = new SampleKnockOff();
		IMethodReturnBuilder<SampleKnockOff.GetOptionalInterceptor.GetOptionalDelegate> builder =
			stub.GetOptional.Call(() => "test");
		Assert.NotNull(builder);
	}

	[Fact]
	public void NonVoidMethod_0Param_Builder_IsIMethodTracking()
	{
		var stub = new SampleKnockOff();
		IMethodTracking tracking = stub.GetOptional.Call(() => "test");
		Assert.NotNull(tracking);
	}

	[Fact]
	public void NonVoidMethod_0Param_Sequence_IsIMethodReturnSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.GetOptional.Call(() => "test");
		IMethodReturnSequence<SampleKnockOff.GetOptionalInterceptor.GetOptionalDelegate> sequence =
			builder.ThenReturn(() => "test2");
		Assert.NotNull(sequence);
	}

	[Fact]
	public void NonVoidMethod_0Param_Sequence_IsIMethodSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.GetOptional.Call(() => "test");
		IMethodSequence sequence = builder.ThenReturn(() => "test2");
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// Method: non-void, 1 param → IMethodReturnBuilder<TDelegate, TArg>
	// ========================================================================

	[Fact]
	public void NonVoidMethod_1Param_Builder_IsIMethodReturnBuilder()
	{
		var stub = new SampleKnockOff();
		IMethodReturnBuilder<SampleKnockOff.GetValueInterceptor.GetValueDelegate, int> builder =
			stub.GetValue.Call((int x) => x * 10);
		Assert.NotNull(builder);
	}

	[Fact]
	public void NonVoidMethod_1Param_Builder_IsIMethodTracking1Arg()
	{
		var stub = new SampleKnockOff();
		IMethodTracking<int> tracking = stub.GetValue.Call((int x) => x * 10);
		Assert.NotNull(tracking);
	}

	[Fact]
	public void NonVoidMethod_1Param_Sequence_IsIMethodReturnSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.GetValue.Call((int x) => x * 10);
		IMethodReturnSequence<SampleKnockOff.GetValueInterceptor.GetValueDelegate> sequence =
			builder.ThenReturn((int x) => x * 20);
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// Property: get+set → IPropertyGetBuilder, IPropertySetBuilder
	// ========================================================================

	[Fact]
	public void Property_GetBuilder_IsIPropertyGetBuilder()
	{
		var stub = new SampleKnockOff();
		IPropertyGetBuilder<string> builder = stub.Name.Get(() => "test");
		Assert.NotNull(builder);
	}

	[Fact]
	public void Property_GetBuilder_IsIPropertyGetTracking()
	{
		var stub = new SampleKnockOff();
		IPropertyGetTracking tracking = stub.Name.Get(() => "test");
		Assert.NotNull(tracking);
	}

	[Fact]
	public void Property_GetSequence_IsIPropertyGetSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.Name.Get(() => "first");
		IPropertyGetSequence<string> sequence = builder.ThenGet(() => "second");
		Assert.NotNull(sequence);
	}

	[Fact]
	public void Property_SetBuilder_IsIPropertySetBuilder()
	{
		var stub = new SampleKnockOff();
		IPropertySetBuilder<string> builder = stub.Name.Set(v => { });
		Assert.NotNull(builder);
	}

	[Fact]
	public void Property_SetBuilder_IsIPropertySetTracking()
	{
		var stub = new SampleKnockOff();
		IPropertySetTracking<string> tracking = stub.Name.Set(v => { });
		Assert.NotNull(tracking);
	}

	[Fact]
	public void Property_SetSequence_IsIPropertySetSequence()
	{
		var stub = new SampleKnockOff();
		var builder = stub.Name.Set(v => { });
		IPropertySetSequence<string> sequence = builder.ThenSet(v => { });
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// Indexer: get+set → IIndexerGetBuilder, IIndexerSetBuilder
	// ========================================================================

	[Fact]
	public void Indexer_GetBuilder_IsIIndexerGetBuilder()
	{
		var stub = new SimpleIndexerKnockOff();
		IIndexerGetBuilder<string, int> builder = stub.Indexer.Get(key => 42);
		Assert.NotNull(builder);
	}

	[Fact]
	public void Indexer_GetBuilder_IsIIndexerGetTracking()
	{
		var stub = new SimpleIndexerKnockOff();
		IIndexerGetTracking<string> tracking = stub.Indexer.Get(key => 42);
		Assert.NotNull(tracking);
	}

	[Fact]
	public void Indexer_GetSequence_IsIIndexerGetSequence()
	{
		var stub = new SimpleIndexerKnockOff();
		var builder = stub.Indexer.Get(key => 42);
		IIndexerGetSequence<string, int> sequence = builder.ThenGet(key => 99);
		Assert.NotNull(sequence);
	}

	[Fact]
	public void Indexer_SetBuilder_IsIIndexerSetBuilder()
	{
		var stub = new SimpleIndexerKnockOff();
		IIndexerSetBuilder<string, int> builder = stub.Indexer.Set((key, value) => { });
		Assert.NotNull(builder);
	}

	[Fact]
	public void Indexer_SetBuilder_IsIIndexerSetTracking()
	{
		var stub = new SimpleIndexerKnockOff();
		IIndexerSetTracking<string, int> tracking = stub.Indexer.Set((key, value) => { });
		Assert.NotNull(tracking);
	}

	[Fact]
	public void Indexer_SetSequence_IsIIndexerSetSequence()
	{
		var stub = new SimpleIndexerKnockOff();
		var builder = stub.Indexer.Set((key, value) => { });
		IIndexerSetSequence<string, int> sequence = builder.ThenSet((key, value) => { });
		Assert.NotNull(sequence);
	}

	// ========================================================================
	// ITracking (root interface) — verify full hierarchy
	// ========================================================================

	[Fact]
	public void MethodBuilder_IsITracking()
	{
		var stub = new SampleKnockOff();
		ITracking tracking = stub.DoSomething.Call(() => { });
		Assert.NotNull(tracking);
	}
}
