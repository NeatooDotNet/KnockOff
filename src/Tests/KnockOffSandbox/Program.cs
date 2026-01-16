using KnockOff.Sandbox;

// Sandbox for manually testing the KnockOff source generator.
// Check the Generated/ folder to see the generated code.

Console.WriteLine("KnockOff Sandbox - Strongly Typed Edition");
Console.WriteLine("=========================================");
Console.WriteLine();

var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

// Test property set/get with typed tracking
// Properties still use direct property access for tracking
Console.WriteLine("Property Tracking:");
service.Name = "Test User";
Console.WriteLine($"  Set Name to: {service.Name}");
Console.WriteLine($"  SetCount: {knockOff.Name.SetCount}");
Console.WriteLine($"  GetCount: {knockOff.Name.GetCount}");
// Strongly typed - no cast!
string? lastSetValue = knockOff.Name.LastSetValue;
Console.WriteLine($"  LastSetValue (typed): {lastSetValue}");
Console.WriteLine();

// Test void method with no params - uses OnCall API
Console.WriteLine("Void Method (no params):");
var doWorkTracking = knockOff.DoWork.OnCall((ko) => { });
service.DoWork();
Console.WriteLine($"  DoWork.WasCalled: {doWorkTracking.WasCalled}");
Console.WriteLine($"  DoWork.CallCount: {doWorkTracking.CallCount}");
Console.WriteLine();

// Test method with single param - user-defined method with tracking interceptor
Console.WriteLine("Method with single param (typed access):");
var greeting = service.GetGreeting("World");
Console.WriteLine($"  Result: {greeting}");
Console.WriteLine($"  CallCount: {knockOff.GetGreeting2.CallCount}");

// User-defined method tracking has LastArg directly on the interceptor
string lastArg = knockOff.GetGreeting2.LastArg;
Console.WriteLine($"  LastArg: {lastArg}");
Console.WriteLine();

// Test method with multiple params - uses OnCall API
Console.WriteLine("Method with multiple params:");
var processTracking = knockOff.Process.OnCall((ko, id, count, urgent) => { });
service.Process("item1", 100, true);
service.Process("item2", 200, false);

var processArgs = processTracking.LastArgs;
Console.WriteLine($"  Call count: {processTracking.CallCount}");
Console.WriteLine($"  Last call: ({processArgs.id}, {processArgs.count}, {processArgs.urgent})");
Console.WriteLine();

// Test interface access via implicit cast
Console.WriteLine("Interface access via cast:");
IUserService svc = knockOff;
svc.Name = "Via cast";
Console.WriteLine($"  Name set via cast: {knockOff.Name.LastSetValue}");
Console.WriteLine();

// Test Reset
Console.WriteLine("Reset tracking:");
Console.WriteLine($"  Before reset - Name.SetCount: {knockOff.Name.SetCount}");
knockOff.Name.Reset();
Console.WriteLine($"  After reset - Name.SetCount: {knockOff.Name.SetCount}");

Console.WriteLine();
Console.WriteLine("Done!");

namespace KnockOff.Sandbox
{
	// Sample interface for testing
	public interface IUserService
	{
		string Name { get; set; }
		int Count { get; }
		void DoWork();
		string GetGreeting(string name);
		void Process(string id, int count, bool urgent);
	}

	// KnockOff class - generator creates explicit interface implementations
	[KnockOff]
	public partial class UserServiceKnockOff : IUserService
	{
		// User-defined protected method - generator will call this for GetGreeting
		protected string GetGreeting(string name) => $"Hello, {name}!";
	}
}
