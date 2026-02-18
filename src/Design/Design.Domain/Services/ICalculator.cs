// -----------------------------------------------------------------------------
// Design.Domain - Interfaces and classes for KnockOff design documentation
// -----------------------------------------------------------------------------
// This file is part of the Design Source of Truth. It defines interfaces
// that are used to demonstrate KnockOff's stub generation capabilities.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// A simple calculator interface used to demonstrate basic method stubbing.
///
/// This interface is intentionally simple - more complex scenarios are
/// demonstrated in other interfaces (IDataService for async, IEventSource for events, etc.)
/// </summary>
public interface ICalculator
{
    /// <summary>
    /// Adds two integers and returns the result.
    /// Used to demonstrate: Return(), Call(), When()
    /// </summary>
    int Add(int a, int b);

    /// <summary>
    /// Subtracts b from a and returns the result.
    /// Used to demonstrate method sequences: Call().ThenCall()
    /// </summary>
    int Subtract(int a, int b);

    /// <summary>
    /// Divides a by b. May throw DivideByZeroException.
    /// Used to demonstrate: Call() with exception throwing
    /// </summary>
    int Divide(int a, int b);

    /// <summary>
    /// A void method - resets the calculator state.
    /// Used to demonstrate: void method handling, Verify()
    /// </summary>
    void Reset();
}
