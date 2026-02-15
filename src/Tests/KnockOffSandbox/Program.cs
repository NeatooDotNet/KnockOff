using KnockOff;
using KnockOff.Sandbox;

// Sandbox for manually testing the KnockOff source generator.
// Check the Generated/ folder to see the generated code.

Console.WriteLine("KnockOff Sandbox - Strongly Typed Edition");
Console.WriteLine("=========================================");
Console.WriteLine();

var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

// Test property set/get with typed tracking
// Properties use Verify API for validation
Console.WriteLine("Property Tracking:");
service.Name = "Test User";
Console.WriteLine($"  Set Name to: {service.Name}");
knockOff.Name.VerifySet(Called.Once);
knockOff.Name.VerifyGet(Called.Once);
Console.WriteLine($"  Verified: VerifySet(Called.Once) and VerifyGet(Called.Once) passed");
// Strongly typed - no cast!
string? lastSetValue = knockOff.Name.LastSetValue;
Console.WriteLine($"  LastSetValue (typed): {lastSetValue}");
Console.WriteLine();

// Test void method with no params - uses Call API
Console.WriteLine("Void Method (no params):");
var doWorkTracking = knockOff.DoWork.Call(() => { });
service.DoWork();
doWorkTracking.Verify(); // Verify method was called
Console.WriteLine($"  DoWork verified!");
// Verify method was called (throws if not)
doWorkTracking.Verify(Called.Once);
Console.WriteLine();

// Test method with single param - user-defined method with tracking interceptor
Console.WriteLine("Method with single param (typed access):");
var greeting = service.GetGreeting("World");
Console.WriteLine($"  Result: {greeting}");
knockOff.GetGreeting.Verify(Called.Once);
Console.WriteLine($"  Verified: Verify(Called.Once) passed");

// User-defined method tracking has LastArg directly on the interceptor
string lastArg = knockOff.GetGreeting.LastArg!;
Console.WriteLine($"  LastArg: {lastArg}");
Console.WriteLine();

// Test method with multiple params - uses Call API
Console.WriteLine("Method with multiple params:");
var processTracking = knockOff.Process.Call((id, count, urgent) => { });
service.Process("item1", 100, true);
service.Process("item2", 200, false);

var processArgs = processTracking.LastArgs;
// Verify method was called twice (throws if not)
processTracking.Verify(Called.Exactly(2));
Console.WriteLine($"  Last call: ({processArgs.Item1}, {processArgs.Item2}, {processArgs.Item3})");
Console.WriteLine();

// Test interface access via implicit cast
Console.WriteLine("Interface access via cast:");
IUserService svc = knockOff;
svc.Name = "Via cast";
Console.WriteLine($"  Name set via cast: {knockOff.Name.LastSetValue}");
Console.WriteLine();

// Test Reset
Console.WriteLine("Reset tracking:");
Console.WriteLine("  Before reset - testing VerifySet(Called.AtLeast(2))...");
knockOff.Name.VerifySet(Called.AtLeast(2));  // Name was set twice (once in Property Tracking, once via cast)
Console.WriteLine("  Verified!");
knockOff.Name.Reset();
Console.WriteLine("  After reset - testing VerifySet(Called.Never)...");
knockOff.Name.VerifySet(Called.Never);
Console.WriteLine("  Verified! Reset cleared tracking.");

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
		protected override string GetGreeting_(string name) => $"Hello, {name}!";
	}
}
