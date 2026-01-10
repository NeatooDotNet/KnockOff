namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Interface with overloaded methods.
/// Used to measure overload resolution overhead.
/// </summary>
public interface IOverloadedService
{
    void Process(int value);
    void Process(string value);
    void Process(int a, int b);
    int Calculate(int value);
    int Calculate(int a, int b);
}
