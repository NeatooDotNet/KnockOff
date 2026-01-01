using KnockOff.Sandbox;

// Sandbox for manually testing the KnockOff source generator.
// Check the Generated/ folder to see the generated code.

Console.WriteLine("KnockOff Sandbox - Strongly Typed Edition");
Console.WriteLine("=========================================");
Console.WriteLine();

var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

// Test property set/get with typed tracking
Console.WriteLine("Property Tracking:");
service.Name = "Test User";
Console.WriteLine($"  Set Name to: {service.Name}");
Console.WriteLine($"  SetCount: {knockOff.Spy.Name.SetCount}");
Console.WriteLine($"  GetCount: {knockOff.Spy.Name.GetCount}");
// Strongly typed - no cast!
string? lastSetValue = knockOff.Spy.Name.LastSetValue;
Console.WriteLine($"  LastSetValue (typed): {lastSetValue}");
Console.WriteLine();

// Test void method with no params
Console.WriteLine("Void Method (no params):");
service.DoWork();
Console.WriteLine($"  DoWork.WasCalled: {knockOff.Spy.DoWork.WasCalled}");
Console.WriteLine($"  DoWork.CallCount: {knockOff.Spy.DoWork.CallCount}");
Console.WriteLine();

// Test method with single param
Console.WriteLine("Method with single param (typed access):");
var greeting = service.GetGreeting("World");
Console.WriteLine($"  Result: {greeting}");
Console.WriteLine($"  CallCount: {knockOff.Spy.GetGreeting.CallCount}");

// Single param uses LastCallArg (not tuple)
string? lastArg = knockOff.Spy.GetGreeting.LastCallArg;
Console.WriteLine($"  LastCallArg: {lastArg}");
Console.WriteLine();

// Test method with multiple params
Console.WriteLine("Method with multiple params:");
service.Process("item1", 100, true);
service.Process("item2", 200, false);

var processArgs = knockOff.Spy.Process.LastCallArgs;
Console.WriteLine($"  Last call: ({processArgs?.id}, {processArgs?.count}, {processArgs?.urgent})");
Console.WriteLine($"  All calls:");
foreach (var call in knockOff.Spy.Process.AllCalls)
{
	Console.WriteLine($"    ({call.id}, {call.count}, {call.urgent})");
}
Console.WriteLine();

// Test AsInterface() method
Console.WriteLine("AsInterface() accessor:");
var svc = knockOff.AsUserService();
svc.Name = "Via AsUserService()";
Console.WriteLine($"  Name set via AsUserService(): {knockOff.Spy.Name.LastSetValue}");
Console.WriteLine();

// Test Reset
Console.WriteLine("Reset tracking:");
Console.WriteLine($"  Before reset - Name.SetCount: {knockOff.Spy.Name.SetCount}");
knockOff.Spy.Name.Reset();
Console.WriteLine($"  After reset - Name.SetCount: {knockOff.Spy.Name.SetCount}");

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
