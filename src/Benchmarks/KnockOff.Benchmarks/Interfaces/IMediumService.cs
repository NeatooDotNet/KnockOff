namespace KnockOff.Benchmarks.Interfaces;

/// <summary>
/// Medium-sized interface with 10 methods.
/// Represents a typical real-world service interface size.
/// </summary>
public interface IMediumService
{
    void Method1();
    void Method2(int param);
    void Method3(string param);
    void Method4(int a, string b);
    void Method5(int a, int b, int c);

    int Method6();
    string Method7();
    int Method8(int param);
    string Method9(string param);
    bool Method10(int a, string b);
}
