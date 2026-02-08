// -----------------------------------------------------------------------------
// Design.Domain - Interfaces for exploring ref/out parameter behavior
// -----------------------------------------------------------------------------
// These interfaces explore how KnockOff handles ref and out parameters.
//
// KEY CONSTRAINT: Func<> and Action<> delegates cannot have ref/out parameters.
// This means lambda-based APIs (Return(callback), Call(callback)) cannot work
// with ref/out parameters. Only constant Return(value), standalone user methods,
// and possibly Verify() are expected to work.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Interface with out parameters to explore KnockOff's out parameter support.
/// </summary>
public interface IOutParameterService
{
    /// <summary>
    /// Classic TryGet pattern: returns bool, provides value via out parameter.
    /// </summary>
    bool TryGetValue(string key, out string value);

    /// <summary>
    /// Void method with out parameter.
    /// </summary>
    void GetDefaults(out int width, out int height);

    /// <summary>
    /// Out parameter with return value (non-bool).
    /// </summary>
    int Parse(string input, out string error);
}

/// <summary>
/// Interface with ref parameters to explore KnockOff's ref parameter support.
/// </summary>
public interface IRefParameterService
{
    /// <summary>
    /// Ref parameter that may be modified.
    /// </summary>
    void Increment(ref int value);

    /// <summary>
    /// Mix of ref and regular parameters.
    /// </summary>
    bool TryUpdate(string key, ref string value);

    /// <summary>
    /// Multiple ref parameters.
    /// </summary>
    void Swap(ref int a, ref int b);
}

/// <summary>
/// Interface mixing ref, out, and normal parameters.
/// </summary>
public interface IMixedRefOutService
{
    /// <summary>
    /// Normal, out, and ref parameters in the same method.
    /// </summary>
    bool Process(string input, out string output, ref int counter);

    /// <summary>
    /// Out parameter with async-like pattern (non-async, just complex signature).
    /// </summary>
    int Transform(string input, out string transformed, out bool wasModified);
}
