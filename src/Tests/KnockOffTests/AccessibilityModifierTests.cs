using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KnockOff.Tests;

/// <summary>
/// Tests that the generator emits the correct accessibility modifier on generated
/// stub classes and Base helper classes. Covers public (regression) and internal
/// (bug fix) types across all applicable pipelines: standalone (flat), standalone
/// class, inline interface, inline class, and inline delegate.
/// </summary>
public class AccessibilityModifierTests
{
	#region Generator Helpers

	/// <summary>
	/// Loads the KnockOff generator assembly and creates a generator instance.
	/// Same approach as DiagnosticTests.
	/// </summary>
	private static IIncrementalGenerator CreateGenerator()
	{
		var testAssemblyDir = Path.GetDirectoryName(typeof(AccessibilityModifierTests).Assembly.Location)!;
		var tfmDir = testAssemblyDir;
		var configDir = Path.GetDirectoryName(tfmDir)!;
		var config = Path.GetFileName(configDir);
		var srcDir = Path.GetFullPath(Path.Combine(configDir, "..", "..", "..", ".."));
		var generatorPath = Path.Combine(srcDir, "Generator", "bin", config, "netstandard2.0", "KnockOff.Generator.dll");

		Assert.True(File.Exists(generatorPath), $"Generator DLL not found at: {generatorPath}");

		var generatorAssembly = Assembly.LoadFrom(generatorPath);
		var generatorType = generatorAssembly.GetType("KnockOff.KnockOffGenerator")!;
		return (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
	}

	/// <summary>
	/// Runs the KnockOff generator on the given source code and returns the generated
	/// source texts and compilation diagnostics.
	/// </summary>
	private static (ImmutableArray<GeneratedSourceResult> Sources, ImmutableArray<Diagnostic> Diagnostics)
		RunGenerator(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);

		var references = new List<MetadataReference>
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(KnockOffAttribute).Assembly.Location),
		};

		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")));

		// Add netstandard reference for broader API surface
		var netstandardPath = Path.Combine(runtimeDir, "netstandard.dll");
		if (File.Exists(netstandardPath))
			references.Add(MetadataReference.CreateFromFile(netstandardPath));

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = CreateGenerator();
		var driver = CSharpGeneratorDriver.Create(generator);
		driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
			compilation, out var outputCompilation, out var diagnostics);

		var runResult = driver.GetRunResult();
		var generatorResult = runResult.Results.Single();

		return (generatorResult.GeneratedSources, diagnostics);
	}

	/// <summary>
	/// Finds a generated source whose hint name contains the specified substring.
	/// </summary>
	private static string GetGeneratedSource(
		ImmutableArray<GeneratedSourceResult> sources, string hintNameContains)
	{
		var match = sources.FirstOrDefault(s =>
			s.HintName.Contains(hintNameContains, StringComparison.OrdinalIgnoreCase));

		Assert.True(match.SyntaxTree != null,
			$"No generated source found with hint name containing '{hintNameContains}'. " +
			$"Available: {string.Join(", ", sources.Select(s => s.HintName))}");

		return match.SourceText.ToString();
	}

	#endregion

	#region Scenario 1: Public standalone interface stub (regression)

	[Fact]
	public void PublicStandaloneInterface_GeneratesPublicBaseClass()
	{
		var source = @"
using KnockOff;

public interface IPublicService
{
    string GetName();
    int Count { get; }
}

[KnockOff]
public partial class PublicServiceStub : IPublicService
{
}
";
		var (sources, _) = RunGenerator(source);
		var baseSource = GetGeneratedSource(sources, "PublicServiceStub.Base");

		// The generated Base class must be public
		Assert.Contains("public class PublicServiceStubBase", baseSource);
		Assert.DoesNotContain("internal class PublicServiceStubBase", baseSource);
	}

	#endregion

	#region Scenario 2: Internal standalone interface stub (bug fix)

	[Fact]
	public void InternalStandaloneInterface_GeneratesInternalBaseClass()
	{
		var source = @"
using KnockOff;

internal interface IInternalService
{
    string GetName();
    int Count { get; }
}

[KnockOff]
internal partial class InternalServiceStub : IInternalService
{
}
";
		var (sources, _) = RunGenerator(source);
		var baseSource = GetGeneratedSource(sources, "InternalServiceStub.Base");

		// The generated Base class must be internal (matching the user's stub class)
		Assert.Contains("internal class InternalServiceStubBase", baseSource);
		Assert.DoesNotContain("public class InternalServiceStubBase", baseSource);
	}

	#endregion

	#region Scenario 3: Public standalone class stub (regression)

	[Fact]
	public void PublicStandaloneClass_GeneratesPublicBaseClass()
	{
		var source = @"
using KnockOff;

public abstract class PublicServiceBase
{
    public virtual string Name { get; set; } = """";
    public abstract int Execute(string command);
}

[KnockOffBase<PublicServiceBase>]
public partial class PublicServiceStub
{
}
";
		var (sources, _) = RunGenerator(source);
		var baseSource = GetGeneratedSource(sources, "PublicServiceStub.Base");

		// The generated Base class must be public
		Assert.Contains("public class PublicServiceStubBase", baseSource);
		Assert.DoesNotContain("internal class PublicServiceStubBase", baseSource);
	}

	#endregion

	#region Scenario 4: Internal standalone class stub (bug fix)

	[Fact]
	public void InternalStandaloneClass_GeneratesInternalBaseClass()
	{
		var source = @"
using KnockOff;

internal abstract class InternalClassBase
{
    public virtual string Name { get; set; } = """";
    public abstract string Process(string input);
}

[KnockOffBase<InternalClassBase>]
internal partial class InternalClassStub
{
}
";
		var (sources, _) = RunGenerator(source);
		var baseSource = GetGeneratedSource(sources, "InternalClassStub.Base");

		// The generated Base class must be internal (matching the user's stub class)
		Assert.Contains("internal class InternalClassStubBase", baseSource);
		Assert.DoesNotContain("public class InternalClassStubBase", baseSource);
	}

	#endregion

	#region Scenario 5: Public inline interface stub (regression)

	[Fact]
	public void PublicInlineInterface_GeneratesPublicStubClass()
	{
		var source = @"
using KnockOff;

public interface IPublicService
{
    string GetName();
    int Count { get; }
}

[KnockOff<IPublicService>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated stub class must be public
		Assert.Contains("public class IPublicService", stubSource);
		Assert.DoesNotContain("internal class IPublicService", stubSource);
	}

	#endregion

	#region Scenario 6: Internal inline interface stub (bug fix)

	[Fact]
	public void InternalInlineInterface_GeneratesInternalStubClass()
	{
		var source = @"
using KnockOff;

internal interface IInternalService
{
    string GetName();
    int Count { get; }
}

[KnockOff<IInternalService>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated stub class must be internal (matching the target interface)
		Assert.Contains("internal class IInternalService", stubSource);
		Assert.DoesNotContain("public class IInternalService", stubSource);
	}

	#endregion

	#region Scenario 7: Public inline class stub (regression)

	[Fact]
	public void PublicInlineClass_GeneratesPublicStubClass()
	{
		var source = @"
using KnockOff;

public abstract class PublicServiceBase
{
    public virtual string Name { get; set; } = """";
    public abstract int Execute(string command);
}

[KnockOff<PublicServiceBase>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated wrapper stub class must be public
		Assert.Contains("public class PublicServiceBase", stubSource);
		Assert.DoesNotContain("internal class PublicServiceBase", stubSource);
	}

	#endregion

	#region Scenario 8: Internal inline class stub (bug fix)

	[Fact]
	public void InternalInlineClass_GeneratesInternalStubClass()
	{
		var source = @"
using KnockOff;

internal abstract class InternalClassBase
{
    public virtual string Name { get; set; } = """";
    public abstract string Process(string input);
}

[KnockOff<InternalClassBase>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated wrapper stub class must be internal (matching the target class)
		Assert.Contains("internal class InternalClassBase", stubSource);
		Assert.DoesNotContain("public class InternalClassBase", stubSource);
	}

	#endregion

	#region Scenario 9: Public inline delegate stub (regression)

	[Fact]
	public void PublicInlineDelegate_GeneratesPublicSealedStubClass()
	{
		var source = @"
using KnockOff;

public delegate int PublicOperation(int a, int b);

[KnockOff<PublicOperation>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated delegate stub class must be public sealed.
		// Use " :" suffix to match the class declaration (avoids matching PublicOperationInterceptor).
		Assert.Contains("public sealed class PublicOperation :", stubSource);
		Assert.DoesNotContain("internal sealed class PublicOperation :", stubSource);
	}

	#endregion

	#region Scenario 10: Internal inline delegate stub (bug fix)

	[Fact]
	public void InternalInlineDelegate_GeneratesInternalSealedStubClass()
	{
		var source = @"
using KnockOff;

internal delegate int InternalOperation(int a, int b);

[KnockOff<InternalOperation>]
public partial class TestClass
{
}
";
		var (sources, _) = RunGenerator(source);
		var stubSource = GetGeneratedSource(sources, "TestClass.Stubs.g.cs");

		// The generated delegate stub class must be internal sealed.
		// Use " :" suffix to match the class declaration (avoids matching InternalOperationInterceptor).
		Assert.Contains("internal sealed class InternalOperation :", stubSource);
		Assert.DoesNotContain("public sealed class InternalOperation :", stubSource);
	}

	#endregion

	#region Scenario 11: Regression — existing public stubs unchanged

	[Fact]
	public void PublicStandaloneInterface_NoInternalLeakage()
	{
		// Verify that a public stub does not accidentally get emitted as internal
		var source = @"
using KnockOff;

public interface IService
{
    void DoWork();
    string Name { get; set; }
}

[KnockOff]
public partial class ServiceStub : IService
{
}
";
		var (sources, _) = RunGenerator(source);

		// Check the main stub file
		var mainSource = GetGeneratedSource(sources, "ServiceStub.g.cs");
		Assert.DoesNotContain("internal class", mainSource);

		// Check the base file
		var baseSource = GetGeneratedSource(sources, "ServiceStub.Base");
		Assert.Contains("public class ServiceStubBase", baseSource);
		Assert.DoesNotContain("internal class ServiceStubBase", baseSource);
	}

	#endregion

	#region Additional edge case: implicit internal (no access modifier)

	[Fact]
	public void ImplicitInternalStandaloneInterface_GeneratesInternalBaseClass()
	{
		// In C#, a top-level class with no explicit modifier defaults to internal.
		// However, the Roslyn semantic model will report DeclaredAccessibility.Internal
		// for such a class. The generator must handle this correctly.
		var source = @"
using KnockOff;

internal interface IImplicitInternalService
{
    string GetName();
}

// No explicit modifier — defaults to internal in C#
[KnockOff]
partial class ImplicitInternalStub : IImplicitInternalService
{
}
";
		var (sources, _) = RunGenerator(source);
		var baseSource = GetGeneratedSource(sources, "ImplicitInternalStub.Base");

		// Should be internal (default for top-level class without modifier)
		Assert.Contains("internal class ImplicitInternalStubBase", baseSource);
		Assert.DoesNotContain("public class ImplicitInternalStubBase", baseSource);
	}

	#endregion

	#region Additional: compilation succeeds for internal types

	[Fact]
	public void InternalStandaloneInterface_CompilationSucceeds()
	{
		// The whole point of the fix: an internal interface stub should compile
		// without CS0060 errors.
		var source = @"
using KnockOff;

internal interface IInternalService
{
    string GetName();
    int Count { get; }
}

[KnockOff]
internal partial class InternalServiceStub : IInternalService
{
}
";
		var syntaxTree = CSharpSyntaxTree.ParseText(source);

		var references = new List<MetadataReference>
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(KnockOffAttribute).Assembly.Location),
		};

		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")));
		var netstandardPath = Path.Combine(runtimeDir, "netstandard.dll");
		if (File.Exists(netstandardPath))
			references.Add(MetadataReference.CreateFromFile(netstandardPath));

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = CreateGenerator();
		var driver = CSharpGeneratorDriver.Create(generator);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

		// Should not have CS0060 (inconsistent accessibility) errors
		var errors = outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToList();

		var cs0060Errors = errors.Where(d => d.Id == "CS0060").ToList();
		Assert.Empty(cs0060Errors);
	}

	[Fact]
	public void InternalInlineInterface_CompilationSucceeds()
	{
		var source = @"
using KnockOff;

internal interface IInternalService
{
    string GetName();
    int Count { get; }
}

[KnockOff<IInternalService>]
public partial class TestClass
{
}
";
		var syntaxTree = CSharpSyntaxTree.ParseText(source);

		var references = new List<MetadataReference>
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(KnockOffAttribute).Assembly.Location),
		};

		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")));
		var netstandardPath = Path.Combine(runtimeDir, "netstandard.dll");
		if (File.Exists(netstandardPath))
			references.Add(MetadataReference.CreateFromFile(netstandardPath));

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = CreateGenerator();
		var driver = CSharpGeneratorDriver.Create(generator);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

		var cs0060Errors = outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "CS0060")
			.ToList();

		Assert.Empty(cs0060Errors);
	}

	#endregion
}
