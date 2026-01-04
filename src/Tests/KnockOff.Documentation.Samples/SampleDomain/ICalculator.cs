namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Calculator interface for method examples.
/// </summary>
public interface ICalculator
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    double Divide(double a, double b);
    Task<int> AddAsync(int a, int b);
    void Reset();
}

/// <summary>
/// Service interface with void methods.
/// </summary>
public interface IService
{
    void Initialize();
    void Process(string name, int value, bool flag);
    User GetUser(int id);
    int Count();
}
