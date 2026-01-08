using KnockOff;

// Test that the KnockOff package works correctly from NuGet

Console.WriteLine("Testing KnockOff NuGet Package");
Console.WriteLine("==============================");

var knockOff = new CalculatorKnockOff();
ICalculator calc = knockOff;

// Test property
calc.LastResult = 42;
Console.WriteLine($"LastResult set to: {calc.LastResult}");
Console.WriteLine($"  SetCount: {knockOff.LastResult.SetCount}");
Console.WriteLine($"  GetCount: {knockOff.LastResult.GetCount}");
Console.WriteLine($"  LastSetValue: {knockOff.LastResult.LastSetValue}");

// Test method with user implementation
var result = calc.Add(10, 20);
Console.WriteLine($"Add(10, 20) = {result}");
Console.WriteLine($"  CallCount: {knockOff.Add2.CallCount}");
Console.WriteLine($"  LastCallArgs: {knockOff.Add2.LastCallArgs}");

// Test void method
calc.Clear();
Console.WriteLine($"Clear() called");
Console.WriteLine($"  WasCalled: {knockOff.Clear.WasCalled}");

Console.WriteLine();
Console.WriteLine("Package test successful!");

// Interface to knock off
public interface ICalculator
{
    int LastResult { get; set; }
    int Add(int a, int b);
    void Clear();
}

// KnockOff implementation
[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    // User method for Add
    protected int Add(int a, int b) => a + b;
}
