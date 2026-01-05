namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with methods that have return values.
/// Used to measure return value handling overhead.
/// </summary>
public interface ICalculator
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Multiply(int a, int b);
    double Divide(double a, double b);
    int Square(int x);
}
