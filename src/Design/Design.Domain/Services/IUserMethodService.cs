// -----------------------------------------------------------------------------
// Design.Domain - Interfaces for demonstrating user-defined method patterns
// -----------------------------------------------------------------------------
// These interfaces are designed to explore and document user method behavior:
// - Basic user methods (protected methods matching interface signatures)
// - User method overloads (multiple signatures, same name)
// - Async user methods
// - Generic user methods
// - Mixed scenarios (some methods with user impl, some without)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Basic interface for demonstrating user-defined methods.
/// User methods are protected methods in the stub that match interface signatures.
/// </summary>
public interface IUserMethodService
{
    /// <summary>
    /// Simple method with single parameter.
    /// Demonstrates: basic user method pattern
    /// </summary>
    string Process(string input);

    /// <summary>
    /// Method with multiple parameters.
    /// Demonstrates: multi-parameter user methods
    /// </summary>
    int Calculate(int a, int b);

    /// <summary>
    /// Void method.
    /// Demonstrates: void user methods
    /// </summary>
    void Execute(string command);

    /// <summary>
    /// Method that returns nullable.
    /// Demonstrates: nullable return types in user methods
    /// </summary>
    string? FindById(int id);
}

/// <summary>
/// Interface with overloaded methods for testing user method overloads.
/// </summary>
public interface IOverloadedUserMethodService
{
    /// <summary>Format with single parameter.</summary>
    string Format(string input);

    /// <summary>Format with options.</summary>
    string Format(string input, bool uppercase);

    /// <summary>Format with options and max length.</summary>
    string Format(string input, bool uppercase, int maxLength);

    /// <summary>Log with message only.</summary>
    void Log(string message);

    /// <summary>Log with level.</summary>
    void Log(string message, int level);
}

/// <summary>
/// Interface for testing mixed scenarios - some methods have user implementations,
/// others don't.
/// </summary>
public interface IMixedUserMethodService
{
    /// <summary>Will have user method implementation.</summary>
    string WithUserMethod(string input);

    /// <summary>Will NOT have user method - uses OnCall instead.</summary>
    string WithoutUserMethod(string input);

    /// <summary>Will have user method implementation.</summary>
    int ComputeWithUserMethod(int value);

    /// <summary>Will NOT have user method - uses Returns instead.</summary>
    int ComputeWithoutUserMethod(int value);
}

/// <summary>
/// Interface with async methods for testing async user methods.
/// </summary>
public interface IAsyncUserMethodService
{
    /// <summary>Async method returning Task of T.</summary>
    Task<string> ProcessAsync(string input);

    /// <summary>Async void method returning Task.</summary>
    Task ExecuteAsync(string command);

    /// <summary>ValueTask version.</summary>
    ValueTask<int> ComputeAsync(int value);
}

/// <summary>
/// Interface with generic methods for testing generic user methods.
/// </summary>
public interface IGenericUserMethodService
{
    /// <summary>Generic method with type parameter.</summary>
    T Create<T>() where T : new();

    /// <summary>Generic method with input and output.</summary>
    TOut Transform<TIn, TOut>(TIn input) where TOut : new();

    /// <summary>Generic method with constraint.</summary>
    T Clone<T>(T source) where T : ICloneable;
}

/// <summary>
/// Interface with properties and methods for testing coexistence.
/// </summary>
public interface IUserMethodWithPropertiesService
{
    /// <summary>Property - uses OnGet/OnSet interceptor.</summary>
    string Title { get; set; }

    /// <summary>Read-only property - uses OnGet interceptor.</summary>
    int Count { get; }

    /// <summary>Method that happens to interact with properties.</summary>
    void IncrementCount();

    /// <summary>Method that returns computed value.</summary>
    string GetFormattedTitle();
}
