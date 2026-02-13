namespace Prototype.Stubs.Interfaces;

/// <summary>
/// Copy of Design.Domain.Services.IStubOverrideService for prototype use.
/// </summary>
public interface IStubOverrideService
{
    string Process(string input);
    int Calculate(int a, int b);
    void Execute(string command);
    string? FindById(int id);
}
