// -----------------------------------------------------------------------------
// Design.Domain - Interfaces for demonstrating stub override patterns
// -----------------------------------------------------------------------------
// These interfaces are designed to explore and document stub override behavior:
// - Basic stub overrides (protected override methods with underscore suffix)
// - Stub override overloads (multiple signatures, same name)
// - Async stub overrides
// - Generic stub overrides
// - Mixed scenarios (some methods with stub override impl, some without)
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Basic interface for demonstrating stub overrides.
/// Stub overrides are protected override methods with underscore suffix (e.g., Process_).
/// </summary>
public interface IStubOverrideService
{
    /// <summary>
    /// Simple method with single parameter.
    /// Demonstrates: basic stub override pattern
    /// </summary>
    string Process(string input);

    /// <summary>
    /// Method with multiple parameters.
    /// Demonstrates: multi-parameter stub overrides
    /// </summary>
    int Calculate(int a, int b);

    /// <summary>
    /// Void method.
    /// Demonstrates: void stub overrides
    /// </summary>
    void Execute(string command);

    /// <summary>
    /// Method that returns nullable.
    /// Demonstrates: nullable return types in stub overrides
    /// </summary>
    string? FindById(int id);
}

/// <summary>
/// Interface with overloaded methods for testing stub override overloads.
/// </summary>
public interface IOverloadedStubOverrideService
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
/// Interface for testing mixed scenarios - some methods have stub override implementations,
/// others don't.
/// </summary>
public interface IMixedStubOverrideService
{
    /// <summary>Will have stub override implementation.</summary>
    string WithStubOverride(string input);

    /// <summary>Will NOT have stub override - uses Call instead.</summary>
    string WithoutStubOverride(string input);

    /// <summary>Will have stub override implementation.</summary>
    int ComputeWithStubOverride(int value);

    /// <summary>Will NOT have stub override - uses Returns instead.</summary>
    int ComputeWithoutStubOverride(int value);
}

/// <summary>
/// Interface with async methods for testing async stub overrides.
/// </summary>
public interface IAsyncStubOverrideService
{
    /// <summary>Async method returning Task of T.</summary>
    Task<string> ProcessAsync(string input);

    /// <summary>Async void method returning Task.</summary>
    Task ExecuteAsync(string command);

    /// <summary>ValueTask version.</summary>
    ValueTask<int> ComputeAsync(int value);
}

/// <summary>
/// Interface with generic methods for testing generic stub overrides.
/// </summary>
public interface IGenericStubOverrideService
{
    /// <summary>Generic method with type parameter.</summary>
    T Create<T>() where T : new();

    /// <summary>Generic method with input and output.</summary>
    TOut Transform<TIn, TOut>(TIn input) where TOut : new();

    /// <summary>Generic method with constraint.</summary>
    T Clone<T>(T source) where T : ICloneable;
}

/// <summary>
/// Interface with overloaded generic methods for testing generic stub override overloads.
/// </summary>
public interface IOverloadedGenericStubOverrideService
{
    /// <summary>Single-parameter generic overload.</summary>
    T Process<T>(T input);

    /// <summary>Two-parameter generic overload.</summary>
    T Process<T>(T input, string options);

    /// <summary>Multi-type-parameter version.</summary>
    TOut Process<TIn, TOut>(TIn input) where TOut : new();
}

/// <summary>
/// Interface with properties and methods for testing coexistence.
/// </summary>
public interface IStubOverrideWithPropertiesService
{
    /// <summary>Property - uses Get/Set interceptor.</summary>
    string Title { get; set; }

    /// <summary>Read-only property - uses Get interceptor.</summary>
    int Count { get; }

    /// <summary>Method that happens to interact with properties.</summary>
    void IncrementCount();

    /// <summary>Method that returns computed value.</summary>
    string GetFormattedTitle();
}
