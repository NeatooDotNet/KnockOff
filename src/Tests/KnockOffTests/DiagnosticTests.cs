using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KnockOff.Tests;

/// <summary>
/// Tests for KnockOff diagnostics emitted by the source generator.
/// Uses CSharpGeneratorDriver to run the generator and verify diagnostics.
/// </summary>
public class DiagnosticTests
{
	/// <summary>
	/// Loads the KnockOff generator assembly and creates a generator instance.
	/// The generator is referenced as an Analyzer (not a runtime reference) in the test project,
	/// so we load it dynamically for programmatic access via CSharpGeneratorDriver.
	/// </summary>
	private static IIncrementalGenerator CreateGenerator()
	{
		// The Generator builds to src/Generator/bin/{config}/netstandard2.0/KnockOff.Generator.dll
		// Test output dir: src/Tests/KnockOffTests/bin/{config}/{tfm}/
		var testAssemblyDir = Path.GetDirectoryName(typeof(DiagnosticTests).Assembly.Location)!;

		// testAssemblyDir = .../src/Tests/KnockOffTests/bin/{config}/{tfm}
		// Navigate up: {tfm} -> {config} -> bin -> KnockOffTests -> Tests -> src
		var tfmDir = testAssemblyDir;
		var configDir = Path.GetDirectoryName(tfmDir)!;        // .../bin/{config}
		var config = Path.GetFileName(configDir);               // "Debug" or "Release"
		var srcDir = Path.GetFullPath(Path.Combine(configDir, "..", "..", "..", "..")); // .../src

		var generatorPath = Path.Combine(srcDir, "Generator", "bin", config, "netstandard2.0", "KnockOff.Generator.dll");

		Assert.True(File.Exists(generatorPath), $"Generator DLL not found at: {generatorPath}");

		var generatorAssembly = Assembly.LoadFrom(generatorPath);
		var generatorType = generatorAssembly.GetType("KnockOff.KnockOffGenerator")!;
		return (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
	}

	/// <summary>
	/// Runs the KnockOff generator on the given source code and returns the diagnostics.
	/// </summary>
	private static ImmutableArray<Diagnostic> GetGeneratorDiagnostics(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);

		// Include references for basic compilation + KnockOff attributes
		var references = new List<MetadataReference>
		{
			MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
			MetadataReference.CreateFromFile(typeof(KnockOffAttribute).Assembly.Location),
		};

		// Add runtime assembly references needed for compilation
		var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
		references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			new[] { syntaxTree },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = CreateGenerator();
		var driver = CSharpGeneratorDriver.Create(generator);
		driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

		return diagnostics;
	}

	[Fact]
	public void KO0201_ClassWithBaseType_NoInterfaces_EmitsDiagnostic()
	{
		var source = @"
using KnockOff;

public class MyBaseClass
{
    public virtual void DoWork() { }
}

[KnockOff]
public partial class MyStub : MyBaseClass
{
}
";

		var diagnostics = GetGeneratorDiagnostics(source);
		var ko0201 = diagnostics.FirstOrDefault(d => d.Id == "KO0201");

		Assert.NotNull(ko0201);
		Assert.Equal(DiagnosticSeverity.Error, ko0201.Severity);
		Assert.Contains("MyStub", ko0201.GetMessage());
		Assert.Contains("MyBaseClass", ko0201.GetMessage());
		Assert.Contains("KnockOffBase", ko0201.GetMessage());
	}

	[Fact]
	public void KO0201_ClassWithBaseType_WithInterfaces_DoesNotEmitKO0201()
	{
		// When a class has both a base type and interfaces, it should NOT get KO0201
		// because it has interfaces to generate stubs for (it may get KO0200 instead)
		var source = @"
using KnockOff;

public interface IService
{
    void DoWork();
}

public class MyBaseClass { }

[KnockOff]
public partial class MyStub : MyBaseClass, IService
{
}
";

		var diagnostics = GetGeneratorDiagnostics(source);
		var ko0201 = diagnostics.FirstOrDefault(d => d.Id == "KO0201");

		Assert.Null(ko0201);
	}

	[Fact]
	public void KO0201_ClassWithNoBaseType_NoInterfaces_DoesNotEmitKO0201()
	{
		// A class with no base type and no interfaces should NOT get KO0201
		// (it just silently generates nothing, as before)
		var source = @"
using KnockOff;

[KnockOff]
public partial class MyStub
{
}
";

		var diagnostics = GetGeneratorDiagnostics(source);
		var ko0201 = diagnostics.FirstOrDefault(d => d.Id == "KO0201");

		Assert.Null(ko0201);
	}
}
